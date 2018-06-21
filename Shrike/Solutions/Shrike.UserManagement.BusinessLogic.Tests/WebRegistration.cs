using System;
using AppComponents;
using AppComponents.ControlFlow;
using AppComponents.Extensions.ExceptionEx;
using AppComponents.Files;
using AppComponents.Raven;
using AppComponents.Topology;
using AppComponents.Web;
using AppComponents.Web.ControlFlow;
using AppComponents.Web.Email;
using log4net;
using RegisterApplicationNodesApp;
using Shrike.Data.Reports.Repository;

namespace Shrike.UserManagement.BusinessLogic.Tests
{
    public enum NamedRegistrations
    {
        WebPrincipal,

        RequestCulture,

        TenancyContext,

        ApplicationType
    }

    public class WebRegistrations : IObjectAssemblySpecifier
    {
        #region IObjectAssemblySpecifier Members

        public void RegisterIn(IObjectAssemblyRegistry registry)
        {
            // register configuration
            log4net.Config.XmlConfigurator.Configure();
            var log = LogManager.GetLogger(GetType());

            try
            {
                registry.Register<IRecurrence<object>>(_ => new WebRecurrence<object>());
                var globalConfig =
                    Catalog.Preconfigure().Add(ApplicationTopologyLocalConfig.CompanyKey, TopologyPath.Company).Add(
                        ApplicationTopologyLocalConfig.ApplicationKey, TopologyPath.Product).ConfiguredCreate(
                            () => new RavenGlobalConfig());

                registry.Register<IConfig>(
                    _ =>
                        new AggregateConfiguration(globalConfig, new ApplicationConfiguration())).AsAssemblerSingleton();

                registry.Register<IConfig>(SpecialFactoryContexts.Safe, _ => globalConfig);


                globalConfig.Start();


                //Context Providers
                registry.Register<IContextProvider>(
                    PrincipalTenancyContextProviderConfiguration.PrincipalContextFactoryKey,
                    _ => new WebPrincipalContextProvider());

                registry.Register<IContextProvider>(
                    NamedRegistrations.TenancyContext, _ => new PrincipalTenancyContextProvider<ApplicationUser>());

                registry.Register<IContextProvider>(
                    NamedRegistrations.RequestCulture, _ => new RequestCultureContextProvider());

                registry.Register<IContextProvider>(
                    NamedRegistrations.ApplicationType, _ => new ApplicationTypeContextProvider());


                registry.Register<IReportDataStorage>(_ => new FileReportDataStorage()).AsAssemblerSingleton();

                //Generic file types
                registry.Register(typeof (IBlobContainer<>), typeof (FileStoreBlobContainer<>));

                //File container
                registry.Register<IFilesContainer>(_ => new FileStoreBlobFileContainer());

                registry.Register<IDistributedMutex>(_ => new DocumentDistributedMutex());

                //smtp email settings
                registry.Register<IMessagePublisher>(_ => new SMTPMessagePublisher());
                registry.Register<IBlobContainer<EmailTemplate>>(_ => new FileStoreBlobContainer<EmailTemplate>());

                registry.Register<IApplicationNodeGatherer>(
                    _ =>
                    {
                        var retval = new PerfCounterGatherer(TopologyPath.Company, TopologyPath.Product);
                        StdPerfCounters.LoadCLRPerfCounters(retval);
                        StdPerfCounters.LoadASPPerfCounters(retval);
                        return retval;
                    }).AsAssemblerSingleton();

                registry.Register<IApplicationAlert>(
                    _ => (IApplicationAlert) Catalog.Factory.Resolve<IApplicationNodeGatherer>());

                var appReg = new ApplicationNodeRegistry(TopologyPath.Company, TopologyPath.Product);

                registry.Register<IApplicationNodeRunner>(
                    _ => Catalog.Preconfigure().Add(
                        ApplicationTopologyLocalConfig.CompanyKey, TopologyPath.Company).Add(
                            ApplicationTopologyLocalConfig.ApplicationKey, TopologyPath.Product).Add(
                                ApplicationTopologyLocalConfig.ComponentType, appReg.ComponentType).Add(
                                    ApplicationTopologyLocalConfig.LogFilePath, "C:\\AppData\\Logs").
                        ConfiguredCreate(() => new RavenApplicationNodeRunner())).AsAssemblerSingleton();

                registry.Register<IApplicationTopologyManager>(_ => new RavenTopologyManager());
            }
            catch (Exception ex)
            {
                //log the exception
                log.ErrorFormat("TRACE: {0}", ex.TraceInformation());
                throw;
            }
        }

        #endregion
    }
}