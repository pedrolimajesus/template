using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents.Raven;

namespace AppComponents.Topology
{
    public class RavenTopologyInstaller : IApplicationTopologyInstaller
    {

        private ApplicationNodeRegistry _reg;
        private string _product;

        public RavenTopologyInstaller()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            var company = cf[ApplicationTopologyLocalConfig.CompanyKey];
            _product = cf[ApplicationTopologyLocalConfig.ApplicationKey];
            
            _reg = new ApplicationNodeRegistry(company,_product);
        }

        public void LocalInstall(
                string id, string componentType, string version, string requiredDB,
                string dbConnection,
                string dbName,
                string dbUN,
                string dbPW)
        {

            _reg.DBConnection = dbConnection;
            _reg.DBPassword = dbPW;
            _reg.DBUser = dbUN;
            _reg.RootDB = dbName;

            using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
            {
                var db = dc.Load<DatabaseInfo>(_product);
                if(null == db)
                    throw new ApplicationNodeNotInstalledException("Database");
                if(db.DatabaseSchemaVersion != requiredDB)
                    throw new ApplicationNodeNotInstalledException(string.Format("Require database {0}", requiredDB));
            }
            
            if(string.IsNullOrEmpty(_reg.Id))
                _reg.Id = id;

            if (string.IsNullOrEmpty(_reg.ComponentType))
            {
                _reg.ComponentType = componentType;
            }
            else
            {
                _reg.ComponentType = _reg.ComponentType + ", " + componentType;
            }

            _reg.Version = version;
            _reg.InstallDate = DateTime.UtcNow;
            
            

        }

        
        public string LocalUninstall(string componentType)
        {
            var appNodeId = _reg.Id;
            _reg.Delete();
            return appNodeId;
        }

        public void AddHotfix(string hotfixId)
        {
            using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
            {
                var an = dc.Load<ApplicationNode>(_reg.Id);
                if(null == an)
                    throw new ApplicationNodeNotInstalledException();

                if (!an.HotfixNames.Contains(hotfixId))
                {
                    an.HotfixNames.Add(hotfixId);
                    dc.SaveChanges();
                }
            }
        }
    }
}
