using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using AppComponents.Extensions.EnumEx;
using AppComponents.Extensions.ExceptionEx;
using AppComponents.RandomNumbers;
using AppComponents.Raven;
using Raven.Abstractions.Exceptions;
using Raven.Client;
using Raven.Client.Linq;
using log4net;

namespace AppComponents.Topology
{
    public class RavenGlobalConfig: IConfig
    {
        public static void MaybeInitDefaults()
        { 
            try
            {
                for (int retry = 0; retry < 5; retry++)
                {
                    try
                    {
                        if (retry > 0)
                            System.Threading.Thread.Sleep(500);

                        using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
                        {
                            var qry = (IRavenQueryable<GlobalConfigItem>)dc.Query<GlobalConfigItem>();
                            var config = qry.GetAllUnSafe().ToDictionary(it => it.Name);
                            bool changed = false;

                            var dbi = dc.Query<DatabaseInfo>().FirstOrDefault();
                            string dbUrl = (null == dbi) ?
                                                AssResource.RavenGlobalConfig_MaybeInitDefaults_RavenDB_database_url :
                                                dbi.Url;

                            changed = changed ||
                                      MaybeInitDefault(dc, config, GlobalConfigItemEnum.DistributedFileShare, AssResource.RavenGlobalConfig_MaybeInitDefaults_set_distributed_file_path);
                            changed = changed ||
                                      MaybeInitDefault(dc, config, GlobalConfigItemEnum.EmailServer, AssResource.RavenGlobalConfig_MaybeInitDefaults_set_email_server);

                            changed = changed ||
                                      MaybeInitDefault(dc, config, GlobalConfigItemEnum.UseSSL, "false");

                            changed = changed ||
                                      MaybeInitDefault(dc, config, GlobalConfigItemEnum.DefaultDataConnection,
                                                       dbUrl);
                            changed = changed ||
                                      MaybeInitDefault(dc, config, GlobalConfigItemEnum.EmailPort,
                                                       AssResource.RavenGlobalConfig_MaybeInitDefaults_set_email_port);
                            changed = changed ||
                                      MaybeInitDefault(dc, config, GlobalConfigItemEnum.EmailAccount,
                                                       AssResource.RavenGlobalConfig_MaybeInitDefaults_set_email_account_name);
                            changed = changed ||
                                      MaybeInitDefault(dc, config, GlobalConfigItemEnum.EmailPassword,
                                                       AssResource.RavenGlobalConfig_MaybeInitDefaults_set_email_account_password);
                            changed = changed ||
                                      MaybeInitDefault(dc, config, GlobalConfigItemEnum.UseSSL,
                                                       AssResource.RavenGlobalConfig_MaybeInitDefaults_email_uses_ssl);
                            if (changed)
                                dc.SaveChanges();
                        }

                        break;
                    }
                    catch (ConcurrencyException) // lots of potential for
                                                 // these due to race conditions
                                                 // between application nodes
                    {
                        
                        
                    }

                }
            }
            catch (Exception ex)
            {
                var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                aa.RaiseAlert(ApplicationAlertKind.Unknown, ex.TraceInformation());
            }
        }

        private static bool MaybeInitDefault(IDocumentSession dc, IDictionary<string,GlobalConfigItem> cf, Enum key, string defaultVal)
        {
            if (!cf.ContainsKey(key.EnumName()))
            {
                var existing = dc.Load<GlobalConfigItem>(key.EnumName());

                if (null == existing)
                {
                    dc.Store(new GlobalConfigItem
                        {
                            Name = key.EnumName(),
                            Value = defaultVal
                        });
                }

                return true;
            }

            return false;
        }


        private IRecurrence<object> _updateCycle;
        private Dictionary<string, string> _cache = new Dictionary<string, string>();
        private object _lock = new object();
     
        private ILog _log;
        private DebugOnlyLogger _dbLog;
        private ApplicationNodeRegistry _reg;

        public RavenGlobalConfig()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var company = cf[ApplicationTopologyLocalConfig.CompanyKey];
            var product = cf[ApplicationTopologyLocalConfig.ApplicationKey];
            _reg = new ApplicationNodeRegistry(company,product);

            _log = ClassLogger.Create(GetType());
            _dbLog = DebugOnlyLogger.Create(_log);

            _updateCycle = Catalog.Factory.Resolve<IRecurrence<object>>();
        }

        public void Start()
        {
            var rnd = GoodSeedRandom.Create();
            _updateCycle.Recur(TimeSpan.FromSeconds(90.0 + (rnd.NextDouble() * 30.0)), Action, null);
        }

        private void Action(object o)
        {
            try
            {
                using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
                {
                    var qry = (IRavenQueryable<GlobalConfigItem>) dc.Query<GlobalConfigItem>();
                    var all = qry.GetAllUnSafe();

                    lock (_lock)
                    {
                        foreach (var globalConfigItem in all)
                        {
                            if (null == globalConfigItem.Value)
                                continue;

                            _cache[globalConfigItem.Name] = globalConfigItem.Value;
                            var settings = ConfigurationManager.AppSettings;

                            try
                            {
                                if (settings.AllKeys.Contains(globalConfigItem.Name))
                                    settings.Remove(globalConfigItem.Name);
                                settings.Add(globalConfigItem.Name, globalConfigItem.Value);

                            }
                            catch 
                            {
                                // config is read-only ...
                                
                            }
                            
                            if (globalConfigItem.Name == CommonConfiguration.DefaultDataConnection.EnumName())
                            {
                                if(_reg.DBConnection != globalConfigItem.Value) _reg.DBConnection = globalConfigItem.Value;
                                
                            }
                            if (globalConfigItem.Name == CommonConfiguration.DefaultDataDatabase.EnumName())
                            {
                                if(_reg.RootDB != globalConfigItem.Value) _reg.RootDB = globalConfigItem.Value;
                                
                            }
                            if (globalConfigItem.Name == CommonConfiguration.DefaultDataUser.EnumName())
                            {
                                if(_reg.DBUser != globalConfigItem.Value) _reg.DBUser = globalConfigItem.Value;
                                
                            }
                            if (globalConfigItem.Name == CommonConfiguration.DefaultDataPassword.EnumName())
                            {
                                if(_reg.DBPassword != globalConfigItem.Value) _reg.DBPassword = globalConfigItem.Value;
                                
                            }
                            if (globalConfigItem.Name == CommonConfiguration.DistributedFileShare.EnumName())
                            {
                                if(_reg.DBPassword != globalConfigItem.Value) _reg.FileShare = globalConfigItem.Value;
                            }
                            
                        }  
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Fatal(ex.TraceInformation());
                if (Catalog.Factory.CanResolve<IApplicationAlert>())
                {
                    var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                    aa.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());
                }

            }
            
        }

        private bool MaybeGetSetting(string name, out string value)
        {
            bool retval = false;
            value = null;
            
            lock (_lock)
            {
                if (_cache.ContainsKey(name))
                {
                    value = _cache[name];
                    retval = true;
                }
            }

            if (!retval)
            {
                value = ConfigurationManager.AppSettings[name];
                retval = null != value;
            }

            if (!retval)
            {
                if (name == CommonConfiguration.DefaultDataConnection.EnumName())
                {
                    value = _reg.DBConnection;
                    retval = true;
                }
                if (name == CommonConfiguration.DefaultDataDatabase.EnumName())
                {
                    value = _reg.RootDB;
                    retval = true;
                }
                if (name == CommonConfiguration.DefaultDataUser.EnumName())
                {
                    value = _reg.DBUser;
                    retval = true;
                }
                if (name == CommonConfiguration.DefaultDataPassword.EnumName())
                {
                    value = _reg.DBPassword;
                    retval = true;
                }
                if (name == CommonConfiguration.DistributedFileShare.EnumName())
                {
                    value = _reg.FileShare;
                    retval = true;
                }
            }

            return retval;
        }

        public string this[Enum id]
        {
            get { return this[id.EnumName()]; }
        }

        public string this[string id]
        {
            get
            {
                string retval;
                if (!MaybeGetSetting(id, out retval))
                    throw new SettingNotFoundException(id);
                return retval;
            }
        }

        public bool Get(Enum id, bool defaultValue)
        {
            return Get(id.EnumName(), defaultValue);
        }

        public bool Get(string id, bool defaultValue)
        {
            string str = null;
            if (!MaybeGetSetting(id, out str))
                return defaultValue;
            return bool.Parse(str);
        }

        public int Get(Enum id, int defaultValue)
        {
            return Get(id.EnumName(), defaultValue);
        }

        public int Get(string id, int defaultValue)
        {
            string str = null;
            if (!MaybeGetSetting(id, out str))
                return defaultValue;
            return int.Parse(str);
        }

        public string Get(Enum id, string defaultValue)
        {
            return Get(id.EnumName(), defaultValue);
        }

        public string Get(string id, string defaultValue)
        {
            string str = null;
            if (!MaybeGetSetting(id, out str))
                return defaultValue;
            return str;
        }

        public T Get<T>(Enum id)
        {
            return Get<T>(id.EnumName());
        }

        public T Get<T>(string id)
        {
            string str = null;
            if(!MaybeGetSetting(id, out str))
                throw new SettingNotFoundException(id);
            if (typeof (T) == typeof (string))
                return (T) (object) str;
            return (T) Convert.ChangeType(str, typeof (T));
        }

        public bool SettingExists(Enum id)
        {
            return SettingExists(id.EnumName());
        }

        public bool SettingExists(string id)
        {
            string dummy = null;
            return MaybeGetSetting(id, out dummy);
        }
    }
}
