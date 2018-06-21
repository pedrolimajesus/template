using System;
using System.ComponentModel.Composition.Hosting;
using AppComponents.Raven;
using ModelCommon.RavenDB;
using Raven.Client;
using Raven.Client.Indexes;

namespace Shrike.DAL.Manager
{
    public static class IndexesManager
    {
        public static void CreateIndexes(IDocumentStore store, string dbName, Type indexType)
        {
            var assemblyToScanForIndexingTasks = indexType.Assembly;
            var catalog = new CompositionContainer(new AssemblyCatalog(assemblyToScanForIndexingTasks));

            var dbCommands = store.DatabaseCommands.ForDatabase(dbName);
            IndexCreation.CreateIndexes(catalog, dbCommands, store.Conventions);
        }

        public static void CreateIndexes(string dbName, Type indexType)
        {
            using (
                var session =
                    DocumentStoreLocator.Resolve(
                        string.Format(
                            "{0}{1}/{2}",
                            DocumentStoreLocator.SchemeRavenRoute,
                            UnikContextTypes.UnikTenantContextResourceKind,
                            dbName)))
            {
                CreateIndexes(session.Advanced.DocumentStore, dbName, indexType);
            }
        }
    }
}
