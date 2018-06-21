using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AppComponents;
using AppComponents.Extensions.EnumEx;
using AppComponents.Raven;
using AppComponents.Topology;
using log4net;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Linq;

namespace Shrike.DAL.Manager
{
    public class DeploymentManager
    {
        private readonly ILog _log = ClassLogger.Create(typeof (DeploymentManager));
        private readonly IApplicationTopologyManager _topologyManager;

        public const string ErrorLoadingConfigValue = "error loading value";

        public DeploymentManager()
        {
            RavenGlobalConfig.MaybeInitDefaults();
            _topologyManager = Catalog.Factory.Resolve<IApplicationTopologyManager>();

        }

        public Boolean TestDataBaseConnection(string url, string username, string password)
        {
            _log.DebugFormat("Test DataBase Connection {0}", url);
            bool response;
            try
            {

                using (IDocumentStore documentStore = new DocumentStore() {Url = url})
                {
                    documentStore.Initialize();
                    using (IDocumentSession session = documentStore.OpenSession())
                    {
                        var query = session.Query<ApplicationNode>();
                        var list = query.ToList();
                        session.SaveChanges();
                        response = true;
                    }
                }
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Test DataBase Connection Failed {0}. Message [{1}] Trace [{1}]", url, e.Message,
                    e.StackTrace);
                response = false;
                //register the error on log files
            }

            return response;
        }

        public IEnumerable<ApplicationNode> GetAllApplicationNodes()
        {
            return _topologyManager.GetTopology();


        }

        public ApplicationNode GetApplicationNodeById(string id)
        {
            _log.DebugFormat("GetApplicationNodeById {0}", id);
            return _topologyManager.GetTopology().SingleOrDefault(it => it.Id == id);

        }

        public void DeleteApplicationNode(string idNode)
        {
            _log.DebugFormat("Delete Application Node {0}", idNode);
            _topologyManager.DeleteNodeInformation(idNode);

        }

        private IDictionary<string, GlobalConfigItem> GetGlobalConfig()
        {
            using (var session = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
            {
                var qry = session.Query<GlobalConfigItem>();
                return qry.GetAllUnSafe().ToDictionary(it => it.Name);
            }
        }

        public DatabaseInfo GetDataBaseConfiguration(string id)
        {

            using (var session = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
            {
                var dataBaseInfo = session.Load<DatabaseInfo>(id);

                if (dataBaseInfo == null)
                {
                    var cf = GetGlobalConfig();

                    var url = ErrorLoadingConfigValue;
                    if (cf.ContainsKey(GlobalConfigItemEnum.DefaultDataConnection.EnumName()))
                        url = cf[GlobalConfigItemEnum.DefaultDataConnection.EnumName()].Value;

                    return new DatabaseInfo
                    {
                        Url = url
                    };
                }

                return dataBaseInfo;
            }
        }

        public GlobalConfigItem GetFileStoreConfiguration()
        {
            var cf = GetGlobalConfig();
            var fs = ErrorLoadingConfigValue;
            if (cf.ContainsKey(GlobalConfigItemEnum.DistributedFileShare.EnumName()))
                fs = cf[GlobalConfigItemEnum.DistributedFileShare.EnumName()].Value;

            return new GlobalConfigItem
            {
                Name = GlobalConfigItemEnum.DistributedFileShare.EnumName(),
                Value = fs
            };

        }

        public void SaveDatabaseConfiguration(string id, DatabaseInfo data)
        {

            using (var session = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
            {
                var database = session.Load<DatabaseInfo>(id);

                if (database == null)
                {
                    database = new DatabaseInfo
                    {
                        Application = "Control Aware",
                        DatabaseSchemaVersion = "1.0.0.0",
                        InstallDate = DateTime.UtcNow,
                        Url = data.Url,
                    };

                    session.Store(database);
                }

                database.Url = data.Url;
                session.SaveChanges();
            }

            SaveGlobalConfigItem(new GlobalConfigItem
            {
                Name = GlobalConfigItemEnum.DefaultDataConnection.EnumName(),
                Value = data.Url
            }, GlobalConfigItemEnum.DefaultDataConnection.EnumName());
        }

        public void CreateDatabaseConfiguration(DatabaseInfo data)
        {
            _log.DebugFormat("Save Application Node [{0}]. databse Url [{1}]", data.Url);
            using (var session = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
            {
                session.Store(data);
                session.SaveChanges();
            }
        }

        public void SaveGlobalConfigItem(GlobalConfigItem info)
        {
            using (var session = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
            {
                var item = session.Load<GlobalConfigItem>(GlobalConfigItemEnum.DistributedFileShare.ToString());
                if (item != null)
                {
                    item.Value = info.Value;
                }
                else
                {
                    item = new GlobalConfigItem
                    {
                        Value = info.Value,
                        Name = GlobalConfigItemEnum.DistributedFileShare.ToString()
                    };
                    session.Store(item);
                }

                session.SaveChanges();
            }
        }

        private void SaveGlobalConfigItem(GlobalConfigItem info, string configItemName)
        {
            using (var session = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
            {
                var item = session.Load<GlobalConfigItem>(configItemName);
                if (item != null)
                {
                    item.Value = info.Value;
                }
                else
                {
                    item = new GlobalConfigItem
                    {
                        Value = info.Value,
                        Name = configItemName
                    };
                    session.Store(item);
                }

                session.SaveChanges();
            }
        }

        private string GFGetDef(IDictionary<string, GlobalConfigItem> cf, Enum key, string def)
        {
            var keyName = key.EnumName();
            return !cf.ContainsKey(keyName) ? def : cf[keyName].Value;
        }

        public EmailServerInfo GetEmailServerConfiguration()
        {
            RavenGlobalConfig.MaybeInitDefaults();
            var cf = GetGlobalConfig();

            int port;
            int.TryParse(GFGetDef(cf, GlobalConfigItemEnum.EmailPort, "443"), out port);

            bool isSsl;
            bool.TryParse(GFGetDef(cf, GlobalConfigItemEnum.UseSSL, "true"), out isSsl);

            const string errDef = ErrorLoadingConfigValue;

            return new EmailServerInfo
            {
                SmtpServer = GFGetDef(cf, GlobalConfigItemEnum.EmailServer, errDef),
                Port = port,
                IsSsl = isSsl,
                Username = GFGetDef(cf, GlobalConfigItemEnum.EmailAccount, errDef),
                Password = GFGetDef(cf, GlobalConfigItemEnum.EmailPassword, errDef),
                ReplyAddress = GFGetDef(cf, GlobalConfigItemEnum.EmailReplyAddress, errDef)
            };
        }

        public void SaveEmailServerConfiguration(EmailServerInfo info)
        {
            SaveGlobalConfigItem(
                new GlobalConfigItem {Name = GlobalConfigItemEnum.EmailServer.EnumName(), Value = info.SmtpServer},
                GlobalConfigItemEnum.EmailServer.EnumName());

            var emailReplyAddressEnum = GlobalConfigItemEnum.EmailReplyAddress.EnumName();
            SaveGlobalConfigItem(
                new GlobalConfigItem {Name = emailReplyAddressEnum, Value = info.ReplyAddress},
                emailReplyAddressEnum);

            SaveGlobalConfigItem(
                new GlobalConfigItem {Name = GlobalConfigItemEnum.EmailAccount.EnumName(), Value = info.Username},
                GlobalConfigItemEnum.EmailAccount.EnumName());

            SaveGlobalConfigItem(
                new GlobalConfigItem {Name = GlobalConfigItemEnum.EmailPassword.EnumName(), Value = info.Password},
                GlobalConfigItemEnum.EmailPassword.EnumName());

            SaveGlobalConfigItem(
                new GlobalConfigItem
                {
                    Name = GlobalConfigItemEnum.EmailPort.EnumName(),
                    Value = info.Port.ToString(CultureInfo.InvariantCulture)
                },
                GlobalConfigItemEnum.EmailPort.EnumName());

            SaveGlobalConfigItem(
                new GlobalConfigItem {Name = GlobalConfigItemEnum.UseSSL.EnumName(), Value = info.IsSsl.ToString()},
                GlobalConfigItemEnum.UseSSL.EnumName());
        }

        public void UpdateLoggingConfiguration(string applicationNode, LoggingConfiguration logging)
        {
            _topologyManager.ConfigureLogging(applicationNode, logging.ClassFilter, logging.LogLevel, logging.File);

        }

        public void ResetLoggingConfiguration(string applicationNode)
        {
            _topologyManager.ResetDefaultLoggingConfiguration(applicationNode);
        }

        public bool ApplicationNodeToggleActive(string idNode)
        {
            var nodes = GetAllApplicationNodes();

            var node = nodes.Single(it => it.Id == idNode);
            if (node.State == ApplicationNodeStates.Running)
            {
                _topologyManager.PauseNode(idNode);
                return false;
            }
            else
            {
                _topologyManager.RunNode(idNode);
                return true;
            }
        }

        public void DeleteAlert(string idNode, string alertId)
        {
            _topologyManager.SetAlertHandled(idNode, alertId);
        }
    }
}