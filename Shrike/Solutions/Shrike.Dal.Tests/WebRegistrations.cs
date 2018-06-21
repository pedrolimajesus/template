namespace Shrike.Dal.Tests
{
    using AppComponents;
    using AppComponents.ControlFlow;
    using AppComponents.Files;
    using AppComponents.Raven;
    using AppComponents.Configuration;
    using AppComponents.Web;
    using AppComponents.Web.Email;

    using Shrike.Dal.Tests.Context;
    using Shrike.Data.Reports.Repository;

    public enum NamedRegistrations
    {
        WebPrincipal,

        RequestCulture,

        TenancyContext,

        ApplicationType
    }

    public class WebRegistrations : IObjectAssemblySpecifier
    {
        public void RegisterIn(IObjectAssemblyRegistry registry)
        {
            // register configuration
            registry.Register<IConfig>(SpecialFactoryContexts.Safe, _ => new ApplicationConfiguration());
            registry.Register<IConfig>(
                factory =>
                new AggregateConfiguration(
                    new WebEmailConfiguration(),
                    new AssemblyAttributeConfiguration(),
                    new AppComponents.DocumentStoreConfigurationService(
                    factory.Resolve<IConfig>(SpecialFactoryContexts.Safe)),
                    factory.Resolve<IConfig>(SpecialFactoryContexts.Safe),
                    new ResourcesConfiguration()));

            //Context Providers
            registry.Register<IContextProvider>(
                PrincipalTenancyContextProviderConfiguration.PrincipalContextFactoryKey,
                _ => new WebPrincipalContextProvider());

            registry.Register<IContextProvider>(
                //NamedRegistrations.TenancyContext, _ => new PrincipalTenancyContextProvider<ApplicationUser>());
                NamedRegistrations.TenancyContext, _ => new TenancyContextProvider());

            registry.Register<IContextProvider>(
                NamedRegistrations.RequestCulture, _ => new RequestCultureContextProvider());

            registry.Register<IContextProvider>(
                NamedRegistrations.ApplicationType, _ => new ApplicationTypeContextProvider());


            registry.Register<IReportDataStorage>(_ => new FileReportDataStorage()).AsAssemblerSingleton();

            //Generic file types
            registry.Register(typeof(IBlobContainer<>), typeof(FileStoreBlobContainer<>));

            //File container
            registry.Register<IFilesContainer>(_ => new FileStoreBlobFileContainer());

            registry.Register<IDistributedMutex>(_ => new DocumentDistributedMutex());

            //smtp email settings
            registry.Register<IMessagePublisher>(_ => new SMTPMessagePublisher());
            registry.Register<IBlobContainer<EmailTemplate>>(_ => new FileStoreBlobContainer<EmailTemplate>());

            //Register for Alerts Log
            registry.Register<IApplicationAlert>(_ => new AppEventLogApplicationAlert());
        }
    }
}