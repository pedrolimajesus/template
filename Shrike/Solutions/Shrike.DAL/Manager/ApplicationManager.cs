using Lok.Unik.ModelCommon.Interfaces;

namespace Shrike.DAL.Manager
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using AppComponents;
    using Lok.Unik.ModelCommon.Client;
    using log4net;
    using System.IO;
    using AppComponents.ControlFlow;
    using AppComponents.Extensions.ExceptionEx;
    using AppComponents.Raven;
    using Raven.Client;


    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class ApplicationManager
    {
        private const string ConfigFileName = "config.json";
        private const string SignageConfigJson = @"{
                                                    ""BasePath"" : ""C:\\LoK\\AppData\\signs\\"",
                                                    ""DynamicPath"" : ""Dynamic"",
                                                    ""DynamicConfig"" : ""signsconfig.json"",
                                                    ""DataManifest"" : ""signsmanifest.json""
                                                    }";

        private readonly ILog _log;

        public ApplicationManager()
        {
            _log = ClassLogger.Create(this.GetType());
        }

        public Application GetApplicationByName(string name)
        {
            return (from app in this.GetAllAplications() where app.Name == name select app).FirstOrDefault();
        }

        public IEnumerable<Application> GetAllAplications()
        {
            using (var nctxs = ContextRegistry.NamedContextsFor(this.GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var list = session.Query<Application>().ToArray();
                    return list;
                }
            }
        }

        public static void CreateApplications()
        {
            var applications = Default().ToList();

            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var query = (from apps in session.Query<Application>() select apps).ToList();

                if (!query.Any())
                {
                    Storage(ref applications);
                    foreach (var application in applications)
                        session.Store(application);
                    session.SaveChanges();
                }

                var q2 = (from tnts in session.Query<Tenant>() select tnts).ToList();

                if (q2.Any())
                    DistributeDefaultApplications(session, q2);
            }
        }

        public static IEnumerable<Application> Default()
        {
            var explicitIds = new List<Guid>
                                  {
                                      Guid.NewGuid(),
                                      Guid.NewGuid(),
                                      Guid.NewGuid(),
                                      Guid.NewGuid()
                                  };

            var applications = new List<Application>
                                  {
                                      new Application
                                          {
                                              Id = explicitIds[0],
                                              Name = ApplicationType.Signage.ToString(),
                                              ManagedAppController = ManagedAppControllerKeys
                                              .LoKMediaEngineXCopyManagedApps.ToString(),
                                               RelativeExePath = "signs.exe",
                                              InstallationPath = "Signage",
                                              RelativeConfigPath = "signsconfig.json",
                                              Version = "1.0",
                                              AdditionalArgs = "None",
                                              IconPath = "signs.ico",
                                              ContentIndex = "Default",
                                              ControllerArgs = "None"
                                          },
                                      new Application
                                          {
                                              Id = explicitIds[1],
                                              Name = ApplicationType.Directory.ToString(),
                                              ManagedAppController = ManagedAppControllerKeys
                                              .LoKMediaEngineXCopyManagedApps.ToString(),
                                              RelativeExePath = "mapedit.exe",
                                              InstallationPath = "Directory",
                                              RelativeConfigPath = "mapeditconfig.json",
                                              Version = "1.0",
                                              AdditionalArgs = "None",
                                              IconPath = "directory.ico",
                                              ContentIndex = "Default",
                                              ControllerArgs = "None"
                                          },
                                      new Application
                                          {
                                              Id = explicitIds[2],
                                              Name = ApplicationType.PowerPoint.ToString(),
                                              ManagedAppController = ManagedAppControllerKeys
                                              .LoKMediaEngineXCopyManagedApps.ToString(),
                                              RelativeExePath = "powerpoint.exe",
                                              InstallationPath = "PowerPoint",
                                              RelativeConfigPath = "powerpointconfig.json",
                                              Version = "1.0",
                                              AdditionalArgs = "None",
                                              IconPath = "ppt.ico",
                                              ContentIndex = "Default",
                                              ControllerArgs = "None"
                                          },
                                      new Application
                                          {
                                              Id = explicitIds[3],
                                              Name = ApplicationType.WebSite.ToString(),
                                              ManagedAppController = ManagedAppControllerKeys
                                              .LoKMediaEngineXCopyManagedApps.ToString(),
                                              RelativeExePath = "website.exe",
                                              InstallationPath = "WebSite",
                                              RelativeConfigPath = "websiteconfig.json",
                                              Version = "1.0",
                                              AdditionalArgs = "None",
                                              IconPath = "website.ico",
                                              ContentIndex = "Default",
                                              ControllerArgs = "None"
                                          }
                                  };

            foreach (var application in applications)
                application.Tags.Add(new TagManager().AddDefault<Application>(application.Name, application.Id.ToString()));

            return applications;
        }



        private static void Storage(ref List<Application> applications)
        {
            //var log = ClassLogger.Create(typeof(ApplicationManager));

            //var cf = Catalog.Factory.Resolve<IConfig>();
            //var preloadedPath = cf[ContentFileStorage.PreLoadedContainer];
            //var containerAppPath = cf[ContentFileStorage.ApplicationContainer];

            //var relativePath = cf.Get("DistributedFileShare", string.Empty);
            //var filesPath = Path.Combine(relativePath, cf[ContentFileStorage.ContentFilesContainer]);
            //var appReadyPath = Path.Combine(filesPath, containerAppPath);

            //var fileStorage = new FileStorageHelper();

            //foreach (var app in applications)
            //{
            //    if (app.Name.Equals(ApplicationType.Signage.ToString()))
            //    {
            //        var appName = string.Format("{0}.zip", app.Name);
            //        var appPath = Path.Combine(filesPath, preloadedPath);

            //        var appRoot = Path.Combine(appPath, appName);

            //        if (File.Exists(appRoot))
            //        {
            //            using (var zip = ZipFile.Read(appRoot))
            //            {
            //                if (!zip.Any(entry => entry.FileName.EndsWith(ConfigFileName)))
            //                {
            //                    zip.AddEntry(ConfigFileName, SignageConfigJson);
            //                    zip.Save();

            //                    log.InfoFormat("{0} has been added to {1} Application successfully", ConfigFileName, appName);
            //                }

            //                zip.Dispose();
            //            }
            //        }
            //    }

            //    try
            //    {
            //        var objId = string.Format("{0}.zip", app.Id);
            //        var existedApp = Path.Combine(appReadyPath, objId);

            //        if (!File.Exists(existedApp))
            //        {
            //            var objName = string.Format("{0}.zip", app.Name);
            //            var loadData = fileStorage.LoadData(preloadedPath, objName);
            //            fileStorage.SaveData(containerAppPath, objId, loadData);
            //            app.DeploymentFileSize = Convert.ToDouble(loadData.Length);
            //            log.InfoFormat("The {0} Application  has been stored in '{1}' successfully", objName, appReadyPath);
            //        }
            //    }
            //    catch (Exception exception)
            //    {
            //        log.ErrorFormat(string.Format("An exception occurred with the following message:  {0}", exception.Message));
            //        var applicationAlert = Catalog.Factory.Resolve<IApplicationAlert>();
            //        applicationAlert.RaiseAlert(ApplicationAlertKind.System, exception.TraceInformation());
            //    }
            //}
        }

        private static void DistributeDefaultApplications(IDocumentSession coreSession, IEnumerable<Tenant> tenants)
        {
            var log = ClassLogger.Create(typeof(ApplicationManager));
            var applications = new List<Application>();
            var query = (from apps in coreSession.Query<Application>() select apps).ToList();

            if (query.Any())
                applications.AddRange(query);

            foreach (var tenant in tenants)
            {
                using (var session = DocumentStoreLocator.Resolve(tenant.Site))
                {
                    var q = (from apps in session.Query<Application>() select apps).ToList();

                    if (!q.Any())
                    {
                        Storage(ref applications);
                        foreach (var app in applications)
                        {
                            session.Store(app);
                            log.InfoFormat("The {0} Application has been Stored in {1} Tenant sucessfully", app.Name, tenant.Name);
                        }
                    }

                    session.SaveChanges();
                }
            }
        }

        public enum ApplicationType
        {
            Signage = 0,

            Directory = 1,

            PowerPoint = 2,

            WebSite = 3
        }

    }
}
