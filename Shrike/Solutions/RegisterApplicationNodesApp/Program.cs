using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using AppComponents;
using AppComponents.ControlFlow;
using AppComponents.Extensions.ExceptionEx;
using AppComponents.Raven;
using AppComponents.Topology;

namespace RegisterApplicationNodesApp
{
    public static class TopologyPath
    {
        public static readonly string Company = "Lo-K Systems";
        public static readonly string Product = "Control Aware";
        public static readonly string Version = "1.0.0.0";
        public static readonly string DBVersion = "1.0.0.0";
        public static readonly string EmailServerShared = "EmailServerShared";

    }

    class Program
    {
        static void Main(string[] args)
        {
            Program program = new Program();
            program.RegisterServerNodes();
            //program.RegisterServerConfig();

            Console.WriteLine("Complete. Press ente to finish");
            Console.ReadLine();
        }

        public void RegisterServerNodes()
        {
            try
            {
                Catalog.Services.Register<IConfig>(SpecialFactoryContexts.Safe, _ => new ApplicationConfiguration());
                Catalog.Services.Register<IRecurrence<object>>(_AppDomain => new ThreadedRecurrence<object>());

                var globalConfig = Catalog.Preconfigure()
                                     .Add(ApplicationTopologyLocalConfig.CompanyKey, TopologyPath.Company)
                                     .Add(ApplicationTopologyLocalConfig.ApplicationKey, TopologyPath.Product)
                                     .ConfiguredCreate(() => new RavenGlobalConfig());

                Catalog.Services.Register<IConfig>(
                    _ => new AggregateConfiguration(globalConfig, new ApplicationConfiguration())).AsAssemblerSingleton();
                Catalog.Services.Register<IConfig>(SpecialFactoryContexts.Safe, _ => globalConfig);

                globalConfig.Start();

                using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
                {
                    ApplicationNode node = new ApplicationNode()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ComponentType = "Rest Server",
                        MachineName = "Server 12",
                        State = ApplicationNodeStates.Running,
                        LastPing = DateTime.UtcNow,
                        ActivityLevel = 1,
                        Version = "1.1",
                        RequiredDBVersion = "1.2"
                    };

                    dc.Store(node);

                    node = new ApplicationNode()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ComponentType = "Control Server",
                        MachineName = "Server 13",
                        State = ApplicationNodeStates.Running,
                        LastPing = DateTime.UtcNow,
                        ActivityLevel = 1,
                        Version = "1.1",
                        RequiredDBVersion = "1.2"
                    };

                    dc.Store(node);

                    node = new ApplicationNode()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ComponentType = "Task Hub Server",
                        MachineName = "Server 13",
                        State = ApplicationNodeStates.Running,
                        LastPing = DateTime.UtcNow,
                        ActivityLevel = 1,
                        Version = "1.1",
                        RequiredDBVersion = "1.2"
                    };

                    dc.Store(node);

                    dc.SaveChanges();
                }


                var list = new List<ApplicationNode>();
                using (var session = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
                {
                    var val = session.Query<ApplicationNode>();
                    list = val.ToList();
                }

                foreach (var node in list)
                {
                    Console.WriteLine("ID Node {0}, IP Address {1}", node.Id, node.IPAddress);
                }


            }
            catch (Exception e)
            {
                Console.WriteLine("Error [{0}] Stack trace [{1}]",
                    e.Message, e.StackTrace);
            }

            
        }

        public void RegisterServerConfig()
        {
            try
            {
                Catalog.Services.Register<IConfig>(SpecialFactoryContexts.Safe, _ => new ApplicationConfiguration());
                Catalog.Services.Register<IRecurrence<object>>(_AppDomain => new ThreadedRecurrence<object>());

                var globalConfig = Catalog.Preconfigure()
                                     .Add(ApplicationTopologyLocalConfig.CompanyKey, TopologyPath.Company)
                                     .Add(ApplicationTopologyLocalConfig.ApplicationKey, TopologyPath.Product)
                                     .ConfiguredCreate(() => new RavenGlobalConfig());

                Catalog.Services.Register<IConfig>(
                    _ => new AggregateConfiguration(globalConfig, new ApplicationConfiguration())).AsAssemblerSingleton();
                Catalog.Services.Register<IConfig>(SpecialFactoryContexts.Safe, _ => globalConfig);

                globalConfig.Start();

                using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
                {
                    var db = dc.Load<DatabaseInfo>(TopologyPath.Product);
                    if (null == db)
                    {
                        db = new DatabaseInfo
                        {
                            Application = TopologyPath.Product,
                            DatabaseSchemaVersion = TopologyPath.DBVersion,
                            InstallDate = DateTime.UtcNow,
                            Url = "http://localhost:8080/raven/"
                        };
                        dc.Store(db);
                        
                        var fc = new GlobalConfigItem
                        {
                            Name = "DistributedFileShare",
                            Value = "C:\\Lok\\AppData"
                        };

                        dc.Store(fc);

                        var email = new EmailServerInfo()
                        {
                            Application = TopologyPath.EmailServerShared,
                            ConfiguredDate =  DateTime.UtcNow,
                            IsSsl = false,
                            SmtpServer = "localhost",
                            Username = "admin",
                            Password = "default",
                            Port = 10
                        };

                        dc.Store(email);

                        dc.SaveChanges();
                    }
                }

                var installer = Catalog.Preconfigure()
                                       .Add(ApplicationTopologyLocalConfig.CompanyKey, TopologyPath.Company)
                                       .Add(ApplicationTopologyLocalConfig.ApplicationKey, TopologyPath.Product)
                                       .ConfiguredCreate(() => new RavenTopologyInstaller());

                installer.LocalInstall(
                    Guid.NewGuid().ToString(),
                    "Development Environment",
                    TopologyPath.Version,
                    TopologyPath.DBVersion,
                    "http://localhost:8080",
                    "Control",
                    "Admin",
                    "b1f08cc1-7130-49d4-bffa-cd1211d2a743");

                Console.WriteLine("Complete.");

            }

            catch (Exception ex)
            {

                Console.WriteLine(ex.TraceInformation());
            }
        }
    }
}
