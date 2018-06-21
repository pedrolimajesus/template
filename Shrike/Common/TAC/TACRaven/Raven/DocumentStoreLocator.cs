// // 
// //  Copyright 2012 David Gressett
// // 
// //    Licensed under the Apache License, Version 2.0 (the "License");
// //    you may not use this file except in compliance with the License.
// //    You may obtain a copy of the License at
// // 
// //        http://www.apache.org/licenses/LICENSE-2.0
// // 
// //    Unless required by applicable law or agreed to in writing, software
// //    distributed under the License is distributed on an "AS IS" BASIS,
// //    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //    See the License for the specific language governing permissions and
// //    limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AppComponents.ControlFlow;
using AppComponents.Extensions.EnumerableEx;
using Raven.Abstractions.Commands;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;
using Raven.Client.Listeners;
using Raven.Client.Shard;

namespace AppComponents.Raven
{
    using System.Xml.Serialization;
    using global::Raven.Client.Extensions;
    using global::Raven.Imports.Newtonsoft.Json;

    internal class DocumentStoreRoute
    {
        private const string _defaultPassword = "diioeupohdiDH98237aaneth@#$*antoeh234895";

        private static readonly string _passPhrase = _defaultPassword;

        static DocumentStoreRoute()
        {
            try
            {
                var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Safe);
                _passPhrase = cf.Get(CommonConfiguration.DatabaseSecret, _defaultPassword);
            }
            catch
            {
            }
        }

        public string Id
        {
            get { return Path.ToString().ToLower(); }

            set { Path = new Uri(value.ToLower()); }
        }

        public Uri Path { get; set; }

        public string Server { get; set; }

        public string Database { get; set; }

        public string CredUser { get; set; }

        public string RegisteredShardStrategy { get; set; }

        public string IndexCatalogKey { get; set; }

        public string _credPassword { get; private set; }

        //ignore serialization of cred password for security reasons on ravendb
        [JsonIgnore]
        [XmlIgnore]
        public string CredPassword
        {
            get { return _credPassword; }
            set { _credPassword = PWCrypto.Encrypt(value, _passPhrase); }
        }

        public string DecodePassword()
        {
            return PWCrypto.Decrypt(_credPassword, _passPhrase);
        }
    }

    public sealed class DocumentStoreNotFoundException : ApplicationException
    {
        public DocumentStoreNotFoundException()
        {
        }

        public DocumentStoreNotFoundException(string msg)
            : base(msg)
        {
        }
    }

    public abstract class AbstractIndexCreationGroupAssemblyTag
    {
    }

    public static class DocumentStoreLocator
    {
        public const string SchemeRavenRoute = "raven://";

        public const string RootLocation = "raven://applicationroot/";


        public static IDocumentSession Resolve(string path)
        {
            return Resolve(new Uri(path));
        }

        public static IDocumentSession Resolve(Uri route)
        {
            Uri path = new Uri(route.ToString().ToLower());
            var ds = ResolveStore(path);
            var dbname = IsRootLocation(path) ? _rootDatabase : _routes[path].Database;
            return Open(ds, dbname);
        }

        private static bool IsRootLocation(Uri path)
        {
            return path.ToString().Equals(RootLocation, StringComparison.InvariantCultureIgnoreCase);
        }

        public static IDocumentSession Resolve(Enum configuredPath)
        {
            var cf = Catalog.Factory.Resolve<IConfig>();
            string uri = cf[configuredPath];
            return Resolve(uri);
        }

        public static IDocumentSession ResolveOrRoot(Enum configuredPath)
        {
            var cf = Catalog.Factory.Resolve<IConfig>();
            var uri = cf.Get(configuredPath, RootLocation);
            return Resolve(uri);
        }

        public static IDocumentSession ContextualResolve()
        {
            var storeType = GetStoreType();
            return ContextualResolve(storeType);
        }

        public static IDocumentStore ContextualResolveStore(out string dbName)
        {
            var storeType = GetStoreType();
            var tenancyUris = ContextRegistry.ContextsOf("Tenancy");
            var tenancyUri = tenancyUris.LastOrDefault();

            var cf = Catalog.Factory.Resolve<IConfig>();
            var uri = new Uri(cf.Get(CommonConfiguration.CoreDatabaseRoute, RootLocation));

            string tenancy = null;
            if (null != tenancyUri)
                tenancy = tenancyUri.Segments.Count() > 1 ? tenancyUri.Segments[1] : string.Empty;

            if (!tenancyUris.Any() || null == tenancy)
            {
                dbName = _rootDatabase;
                return ResolveStorePath(CommonConfiguration.CoreDatabaseRoute);
            }

            var path = string.Format("{0}{1}/{2}", SchemeRavenRoute, storeType, tenancy);
            uri = new Uri(path);
            var retval = ResolveStorePath(path);

            dbName = IsRootLocation(uri) ? _rootDatabase : _routes[uri].Database;

            return retval;
        }

        public static IDocumentStore ResolveStorePath(Enum configuredPath)
        {
            var cf = Catalog.Factory.Resolve<IConfig>();
            var uri = cf.Get(configuredPath, RootLocation);
            return ResolveStore(uri.ToString());
        }

        public static IDocumentStore ResolveStorePath(string storeRoute)
        {
            Uri path = new Uri(storeRoute.ToLower());
            var ds = ResolveStore(path);
            return ds;
        }

        public static string GetStoreType()
        {
            var contexts = ContextRegistry.ContextsOf(ContextRegistry.Kind);
            string storeType;
            if (contexts != null && contexts.Any())
            {
                var storeTypeUri = contexts.First();
                storeType = storeTypeUri.Segments.Count() > 1 ? storeTypeUri.Segments[1] : string.Empty;
            }
            else
            {
                var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Safe);
                var defaultStoreType = config.Get(CommonConfiguration.StoreType, string.Empty);
                storeType = defaultStoreType;
            }
            return storeType;
        }

        public static IDocumentSession ContextualResolve(string storeType)
        {
            var tenancy = GetContextualTenancy();

            if (string.IsNullOrEmpty(tenancy)) return ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute);
            else return Resolve(string.Format("{0}{1}/{2}", SchemeRavenRoute, storeType, tenancy));
        }

        public static string GetContextualTenancy()
        {
            var tenancy = string.Empty;
            var tenancyUris = ContextRegistry.ContextsOf("Tenancy");
            if (tenancyUris.Any())
            {
                var tenancyUri = tenancyUris.Last();
                tenancy = tenancyUri.Segments.Count() > 1 ? tenancyUri.Segments[1] : string.Empty;
            }
            return tenancy;
        }

        public static IDocumentStore ResolveStore(string path)
        {
            return ResolveStore(new Uri(path.ToLower()));
        }

        private const int RavenTimeoutInMilliseconds = 1800000;

        public static IDocumentStore ResolveStore(Uri otherPath)
        {
            Uri path = new Uri(otherPath.ToString().ToLower());
            IDocumentStore retval;
            if (IsRootLocation(path))
            {
                return _rootStore;
            }

            var normalized = new Uri(path.ToString().ToLower());
            if (!_documentStores.TryGetValue(normalized, out retval))
            {
                using (var dc = Open(_rootStore, _rootDatabase))
                {
                    var route = dc.Load<DocumentStoreRoute>(normalized.ToString().ToLower());
                    if (null == route)
                    {
                        throw new DocumentStoreNotFoundException(String.Format("No route found for path {0}", path));
                    }

                    NetworkCredential cred = null;
                    if (!string.IsNullOrEmpty(route.CredUser) && !string.IsNullOrEmpty(route.CredPassword))
                    {
                        cred = new NetworkCredential(route.CredUser, route.DecodePassword());
                    }

                    IDocumentStore ds;

                    if (string.IsNullOrEmpty(route.RegisteredShardStrategy))
                    {
                        DocumentStore docStore;
                        if (null != cred)
                            docStore = new DocumentStore {Url = route.Server, Credentials = cred};
                        else
                            docStore = new DocumentStore {Url = route.Server};


                        ds = docStore;
                    }
                    else
                    {
                        ShardStrategy shardStrategy;
                        if (!_shardStrats.TryGetValue(route.RegisteredShardStrategy, out shardStrategy))
                        {
                            throw new DocumentStoreNotFoundException(
                                string.Format("No shard strategy registered: {0}", shardStrategy));
                        }

                        var docStore = new ShardedDocumentStore(shardStrategy);

                        ds = docStore;
                    }

                    ds.Conventions.FindIdentityProperty = DataDocument.DocumentIdentityFinder;
                    ds.Conventions.AllowMultipuleAsyncOperations = true;
                    ds.Conventions.MaxNumberOfRequestsPerSession = 300;
                    ds.JsonRequestFactory.ConfigureRequest +=
                        (e, x) =>
                        {
                            var wr = ((HttpWebRequest) x.Request);
                            wr.Timeout = RavenTimeoutInMilliseconds;
                            wr.UnsafeAuthenticatedConnectionSharing = true;
                        };
                    ds.Conventions.CustomizeJsonSerializer =
                        serializer => serializer.TypeNameHandling = TypeNameHandling.All;

                    // add registered listeners
                    var dsBase = ds as DocumentStoreBase;

                    var storeListeners = Catalog.Factory.ResolveAll<IDocumentStoreListener>();
                    storeListeners.ForEach(it => dsBase.RegisterListener(it));

                    var deleteListeners = Catalog.Factory.ResolveAll<IDocumentDeleteListener>();
                    deleteListeners.ForEach(it => dsBase.RegisterListener(it));

                    var conversionListeners = Catalog.Factory.ResolveAll<IDocumentConversionListener>();
                    conversionListeners.ForEach(it => dsBase.RegisterListener(it));

                    var queryListeners = Catalog.Factory.ResolveAll<IDocumentQueryListener>();
                    queryListeners.ForEach(it => dsBase.RegisterListener(it));


                    ds.Initialize();


                    if (!String.IsNullOrEmpty(route.IndexCatalogKey))
                    {
                        var asms = (from reg in Catalog.Services.GetAllRegistrations()
                            where reg.Name == route.IndexCatalogKey
                            select reg.ResolvesTo.Assembly).Distinct();

                        foreach (var asm in asms)
                        {
                            IndexCreation.CreateIndexes(asm, ds);
                        }
                    }

                    _documentStores.TryAdd(path, ds);
                    _routes.TryAdd(path, route);

                    retval = ds;
                }
            }

            return retval;
        }

        public static IEnumerable<Uri> KnownRoutesMatching(Uri partialPath)
        {
            var segments = Enumerable.Empty<string>();

            Uri normalized;
            normalized = new Uri(partialPath.ToString().ToLower());
            segments = normalized.Segments;

            using (var dc = Open(_rootStore, _rootDatabase))
            {
                var routes = dc.Query<DocumentStoreRoute>().GetAllUnSafe().Select(dsr => dsr.Path);

                if (segments.Any())
                {
                    routes = from r in routes
                        where
                            r.Host == normalized.Host &&
                            (r.Segments.Length >= segments.Count()
                             && r.Segments.Take(segments.Count()).SequenceEqual(segments))
                        select r;
                }

                return routes;
            }
        }

        public static void AddRoute(
            Uri newRoute,
            string server,
            string databaseName,
            string user,
            string pw,
            string indexCatalogKey,
            string shardGroup = null)
        {
            var routeId = newRoute.ToString().ToLower();
            var route = new DocumentStoreRoute
            {
                Path = new Uri(routeId),
                Server = server,
                Database = databaseName,
                IndexCatalogKey = indexCatalogKey,
                CredUser = user,
                CredPassword = pw,
                RegisteredShardStrategy = shardGroup
            };

            //create the database
            _rootStore.DatabaseCommands.EnsureDatabaseExists(databaseName);

            using (var dc = Open(_rootStore, _rootDatabase))
            {
                var s = dc.Load<DocumentStoreRoute>(routeId);
                if (s != null) return;

                dc.Store(route);
                dc.SaveChanges();
            }
        }

        public static void RemoveRoute(Uri path)
        {
            Uri route = new Uri(path.ToString().ToLower());
            using (var dc = Open(_rootStore, _rootDatabase))
            {
                dc.Advanced.Defer(new DeleteCommandData {Key = string.Format("DocumentStoreRoutes/{0}", route)});

                dc.SaveChanges();
                DocumentStoreRoute _;
                _routes.TryRemove(route, out _);
            }
        }

        public static void RegisterShardStrategy(string name, ShardStrategy strat)
        {
            _shardStrats.TryAdd(name, strat);
        }

        public static Dictionary<string, IDocumentStore> CreateRoutedShards(IEnumerable<Tuple<string, Uri>> routes)
        {
            return routes.Select(r => new {k = r.Item1, ds = ResolveStore(r.Item2)}).ToDictionary(x => x.k, x => x.ds);
        }

        public static Dictionary<string, IDocumentStore> CreateRoutedShards(params Tuple<string, Uri>[] routes)
        {
            return CreateRoutedShards(routes.AsEnumerable());
        }

        public static IDocumentSession Open(IDocumentStore store, string databaseName = null)
        {
            IDocumentSession retval;
            if (string.IsNullOrEmpty(databaseName))
            {
                //opens session on default database
                retval = store.OpenSession();
            }
            else
            {
                // TODO set to false in production SERVER with SSL web server
                // TODO - MWB: what do we do here???
                //store.JsonRequestFactory.EnableBasicAuthenticationOverUnsecuredHttpEvenThoughPasswordsWouldBeSentOverTheWireInClearTextToBeStolenByHackers = true;

                try
                {
                    store.DatabaseCommands.EnsureDatabaseExists(databaseName);
                    // inexplicably throws http 304 webexception if the db is there
                }
                catch
                {
                }


                //opens session on a selected database
                retval = store.OpenSession(databaseName);
            }

            retval.Advanced.UseOptimisticConcurrency = true;
            retval.Advanced.MaxNumberOfRequestsPerSession = 1024;

            return retval;
        }

        public static void FlushDocumentStoreCache()
        {
            _documentStores.Clear();
            _routes.Clear();
        }

        #region internals

        private static readonly ConcurrentDictionary<Uri, IDocumentStore> _documentStores =
            new ConcurrentDictionary<Uri, IDocumentStore>();

        private static readonly ConcurrentDictionary<Uri, DocumentStoreRoute> _routes =
            new ConcurrentDictionary<Uri, DocumentStoreRoute>();

        private static readonly ConcurrentDictionary<string, ShardStrategy> _shardStrats =
            new ConcurrentDictionary<string, ShardStrategy>();

        private static IDocumentStore _rootStore;

        private static string _rootDatabase;

        private static object _syncCreate = new object();

        private static bool _created;

        static DocumentStoreLocator()
        {
            lock (_syncCreate)
            {
                if (!_created)
                {
                    var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Safe);

                    if (cf.SettingExists(CommonConfiguration.DefaultDataDatabase))
                    {
                        _rootDatabase = cf[CommonConfiguration.DefaultDataDatabase];
                    }

                    NetworkCredential cred = null;
                    if (cf.SettingExists(CommonConfiguration.DefaultDataUser)
                        && cf.SettingExists(CommonConfiguration.DefaultDataPassword))
                    {
                        cred = new NetworkCredential(
                            cf[CommonConfiguration.DefaultDataUser], cf[CommonConfiguration.DefaultDataPassword]);
                    }

                    var defaultDataConnection = cf[CommonConfiguration.DefaultDataConnection];
                    _rootStore = new DocumentStore
                    {
                        Url = defaultDataConnection,
                        Credentials = cred
                    };

                    _rootStore.Conventions.FindIdentityProperty = DataDocument.DocumentIdentityFinder;

                    try
                    {
                        _rootStore.Initialize();
                    }
                    catch (UriFormatException ufx)
                    {
                        var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                        var err = ufx.ToString();
                        if (null != ufx.InnerException)
                        {
                            err += ufx.InnerException.ToString();
                        }

                        err += Environment.NewLine + "-" + defaultDataConnection;

                        aa.RaiseAlert(ApplicationAlertKind.Services, err);
                        throw;
                    }

                    var assemblies =
                        AppDomain.CurrentDomain.GetAssemblies().Where(
                            a =>
                                !a.IsDynamic && !a.FullName.StartsWith("System.") &&
                                !a.FullName.StartsWith("Microsoft.")
                                && !a.FullName.StartsWith("DotNet"));

                    var indexerTags = from type in assemblies.SelectMany(a => a.GetTypes())
                        where type.BaseType == typeof (AbstractIndexCreationGroupAssemblyTag)
                        select type;

                    foreach (var type in indexerTags)
                    {
                        Catalog.Services.Register(type.Name, type, type);
                    }

                    _created = true;
                }
            }
        }

        #endregion
    }
}