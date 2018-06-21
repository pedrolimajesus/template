using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using AppComponents;
using AppComponents.Topology;
using Lok.Unik.ModelCommon.Client;
using Shrike.DAL.Manager;
using Shrike.UserManagement.BusinessLogic.Business.NodeTestingProviders;

namespace Shrike.UserManagement.BusinessLogic.Business
{
    public class DeploymentBusinessLogic
    {
        private DeploymentManager _manager;

        public DeploymentBusinessLogic()
        {
            _manager = new DeploymentManager();
        }

        public IEnumerable<ApplicationNode> GetAllApplicationNodes()
        {
            var list = _manager.GetAllApplicationNodes();

            return list;
        }

        public ApplicationNode GetApplicationNodeById(string id)
        {
            var node = _manager.GetApplicationNodeById(id);

            return node;
        }

        public void DeleteApplicationNode(string idNode)
        {
            _manager.DeleteApplicationNode(idNode);
        }

        public DatabaseInfo GetDataBaseConfiguration(string id)
        {
            var info = _manager.GetDataBaseConfiguration(id);
            return info;
        }

        public GlobalConfigItem GetFileStoreConfiguration()
        {
            var info = _manager.GetFileStoreConfiguration();
            return info;
        }

        public void SaveDatabaseConfiguration(string id, DatabaseInfo db)
        {
            _manager.SaveDatabaseConfiguration(id, db);
        }

        public void CreateDatabaseConfiguration(DatabaseInfo db)
        {
            _manager.CreateDatabaseConfiguration(db);
        }

        public void SaveFileStoreConfiguration(GlobalConfigItem info)
        {
            _manager.SaveGlobalConfigItem(info);
        }

        public EmailServerInfo GetEmailServerConfiguration()
        {
            return _manager.GetEmailServerConfiguration();
        }

        public void SaveEmailServerConfiguration(EmailServerInfo info)
        {
            _manager.SaveEmailServerConfiguration(info);
        }

        public void UpdateLoggingConfiguration(string applicationNode, LoggingConfiguration logging)
        {
            _manager.UpdateLoggingConfiguration(applicationNode, logging);
        }

        public bool TestDataBaseConnection(string databaseUrl, string username, string password)
        {
            var provider = new DataBaseTestingProvider();
            var info = new DatabaseInfo {Url = databaseUrl};
            return provider.TestNode(info);
        }

        public bool TestEmailServerConnection(EmailServerInfo info, string email = null)
        {
            var provider = new EmailServerTestingProvider();
            return provider.TestNode(info, email);
        }

        public bool TestFileStoreConnection(GlobalConfigItem item)
        {
            var provider = new FileServerTestingProvider();

            return provider.TestNode(item);
        }

        public bool ApplicationNodeToggleActive(string idNode)
        {
            return _manager.ApplicationNodeToggleActive(idNode);
        }

        public void DeleteAlert(string idNode, string idAlert)
        {
            _manager.DeleteAlert(idNode, idAlert);
        }
    }
}
