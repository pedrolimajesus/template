using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using AppComponents;
using AppComponents.Topology;
using Shrike.Areas.UserManagementUI.UserManagementUI.Models;
using Shrike.UserManagement.BusinessLogic.Business;

namespace Shrike.Areas.UserManagementUI.UserManagementUI
{
    public class DeploymentUILogic
    {
        private const String DatabaseId = "Control Aware";

        private DeploymentBusinessLogic _deployBusLogic;

        public DeploymentUILogic()
        {
            _deployBusLogic = new DeploymentBusinessLogic();
        }

        public IEnumerable<ApplicationNode> GetAllApplicationNodes()
        {
            var list = _deployBusLogic.GetAllApplicationNodes();
            return list;
        }

        public ApplicationNode GetApplicationNode(string id)
        {
            var node = _deployBusLogic.GetApplicationNodeById(id);

            return node;
        }

        public DataBase GetDataBaseConfiguration()
        {
            var info = _deployBusLogic.GetDataBaseConfiguration(DatabaseId);
            var database = new DataBase();
            if (info != null)
            {
                database.DatabaseUrl = info.Url;
            }

            return database;
        }

        public FileStore GetFileStoreConfiguration()
        {
            var filestore = new FileStore();
            var info = _deployBusLogic.GetFileStoreConfiguration();
            if (info != null)
            {
                filestore.FolderPath = info.Value;
            }

            return filestore;
        }

        public void DeleteApplicationNode(string idNode)
        {
            _deployBusLogic.DeleteApplicationNode(idNode);
        }

        public void SaveDatabaseConfiguration(DataBase newdb)
        {
            var db = _deployBusLogic.GetDataBaseConfiguration(DatabaseId);
            if (db != null)
            {
                db.Url = newdb.DatabaseUrl;
                _deployBusLogic.SaveDatabaseConfiguration(DatabaseId, db);
            }
            else
            {
                var database = new DatabaseInfo
                    {
                        Application = DatabaseId,
                        Url = newdb.DatabaseUrl
                    };
                _deployBusLogic.CreateDatabaseConfiguration(database);
            }

        }

        public void SaveFileStoreConfiguration(FileStore fileStore)
        {
            var info = _deployBusLogic.GetFileStoreConfiguration();
            if (info != null)
            {
                info.Value = fileStore.FolderPath;
            }
            else
            {
                info = new GlobalConfigItem() {Value = fileStore.FolderPath};
            }

            _deployBusLogic.SaveFileStoreConfiguration(info);
        }

        public EmailServer GetEmailServerConfiguration()
        {
            var info = _deployBusLogic.GetEmailServerConfiguration();
            var emailserver = new EmailServer()
                {
                    IsSsl = info.IsSsl,
                    Port = info.Port,
                    SmtpServer = info.SmtpServer,
                    Username = info.Username,
                    Password = info.Password,
                    ReplyAddress = info.ReplyAddress
                };

            return emailserver;
        }

        public void SaveEmailServerConfiguration(EmailServer data)
        {
            var info = _deployBusLogic.GetEmailServerConfiguration() ?? new EmailServerInfo();

            info.IsSsl = data.IsSsl;
            info.Password = data.Password;
            info.Port = data.Port;
            info.Username = data.Username;
            info.SmtpServer = data.SmtpServer;
            info.ReplyAddress = data.ReplyAddress;

            _deployBusLogic.SaveEmailServerConfiguration(info);
        }

        public void UpdateLoggingConfiguration(string applicationNode, LoggingConfiguration logging)
        {
            _deployBusLogic.UpdateLoggingConfiguration(applicationNode, logging);
        }

        public bool TestDataBaseConnection(DataBase data)
        {
            var response = _deployBusLogic.TestDataBaseConnection(data.DatabaseUrl, "", "");
            return response;
        }

        public bool TestEmailServerConnection(EmailServer data, string email = null)
        {
            var info = new EmailServerInfo
                           {
                               IsSsl = data.IsSsl,
                               Username = data.Username,
                               Password = data.Password,
                               SmtpServer = data.SmtpServer,
                               Port = data.Port,
                               ReplyAddress = data.ReplyAddress
                           };

            return _deployBusLogic.TestEmailServerConnection(info, email);

        }

        public bool TestFileStoreConnection(FileStore fStore)
        {
            GlobalConfigItem item = new GlobalConfigItem();
            item.Value = fStore.FolderPath;
            return _deployBusLogic.TestFileStoreConnection(item);
        }

        internal bool ApplicationNodeToggleActive(string idNode)
        {
            return _deployBusLogic.ApplicationNodeToggleActive(idNode);
        }

        public void DeleteAlert(string idNode, string idAlert)
        {
            _deployBusLogic.DeleteAlert(idNode, idAlert);

        }

    }
}