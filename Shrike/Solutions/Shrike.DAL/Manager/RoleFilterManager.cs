namespace Shrike.DAL.Manager
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using AppComponents;
    using AppComponents.Raven;
    using AppComponents.Web;

    public class RoleFilterManager
    {
        public IEnumerable<string> GetAllRoles()
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var query = (from r in session.Query<ApplicationRole>() select r).ToList();
                return query.Select(x => x.Name).ToArray();
            }
        }
    }
}
