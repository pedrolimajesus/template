using System.Linq;

namespace Shrike.DAL.Manager
{
    using System;
    using System.IO;

    using AppComponents;
    using AppComponents.Raven;
    using Lok.Unik.ModelCommon.Client;

    using Raven.Client;

    using Helper;

    public class NavigationManager
    {

        public void LoadNavigation(NavigationWrapper navigator)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                LoadNavigation(navigator, session, true);
            }
        }

        public void LoadNavigation(NavigationWrapper navigator, IDocumentSession session, bool commit = false)
        {
            var current = session.Query<Navigation>().ToArray();
            if (current.Any())
            {
                return;
            }

            foreach (var item in navigator.Navigations)
            {
                session.Store(item);
            }

            if (commit)
            {
                session.SaveChanges();
            }

        }

        public Navigation GetNavigation(string role)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var query = session.Query<Navigation>().FirstOrDefault(x => x.Role == role);
                return query;
            }
        }

        private const string NavigationFileFormat = "{0}.json";

        public NavigationWrapper LoadNavigationFromJsonFile()
        {
            var cf = Catalog.Factory.Resolve<IConfig>();
            var filePath = string.Format(NavigationFileFormat, cf[ContentFileStorage.NavigationConfig]);
            filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            var navigator = JsonFileSerializer.ExtractObject<NavigationWrapper>(filePath);
            return navigator;
        }
    }

}
