
namespace TACTracker.Console.Test
{
    using AppComponents;
    using AppComponents.Configuration;

    using TACBitTorrent.Configuration;
    using TACBitTorrent.Interfaces;

    using TACMonotorrent;

    public class TorrentTestRegistrations : IObjectAssemblySpecifier
    {
        public void RegisterIn(IObjectAssemblyRegistry registry)
        {
            // register configuration
            registry.Register<IConfig>(SpecialFactoryContexts.Safe, _ => new ApplicationConfiguration());
            registry.Register<IConfig>(
                factory =>
                new AggregateConfiguration(
                    new TorrentConfiguration(),
                    new AssemblyAttributeConfiguration(),
                    factory.Resolve<IConfig>(SpecialFactoryContexts.Safe),
                    new ResourcesConfiguration()));

            registry.Register<ITrackerFactory>(_ => new TrackerFactory());
            registry.Register<ITorrentClientManager>(_ => new TorrentClientManager());
        }
    }
}
