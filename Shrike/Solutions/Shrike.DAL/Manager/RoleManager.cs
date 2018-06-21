using System;
using System.Collections.Generic;
using System.Linq;

using AppComponents;
using AppComponents.Raven;
using AppComponents.Web;

namespace Shrike.DAL.Manager
{
    using System.IO;

    using System.Web.Security;

    using Lok.Unik.ModelCommon.Client;

    using Raven.Client;

    using Helper;

    public static class DefaultRoles
    {
        public const string SuperAdmin = "SuperAdmin";

        public const string TenantOwner = "TenantOwner";
    }

    public class RoleManager
    {
        public static readonly string[] TenantRoles = GetAllTenantRoleNames(Roles.ApplicationName);

        private static string[] GetAllTenantRoleNames(string applicationName)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var roleNames =
                    (from ar in session.Query<ApplicationRole>()
                     where ar.ApplicationName == applicationName
                     && ar.Type == ApplicationRoleType.Tenant
                     select ar.Name).ToArray();
                    return roleNames;
            }
        }

        private static IEnumerable<ApplicationRole> GetAllTenantRoles(string applicationName)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var roles =
                    (from ar in session.Query<ApplicationRole>()
                     where ar.ApplicationName == applicationName
                     && ar.Type == ApplicationRoleType.Tenant
                     select ar).ToArray();
                return roles;
            }
        }

        private static IDictionary<string, string> tenantRoleDescriptions;

        public static IDictionary<string, string> TenantRoleDescriptions
        {
            get
            {
                if (tenantRoleDescriptions != null) return tenantRoleDescriptions;

                tenantRoleDescriptions = new Dictionary<string, string>();
                foreach (var tenantRole in GetAllTenantRoles(Roles.ApplicationName))
                {
                    tenantRoleDescriptions.Add(tenantRole.Id.ToLowerInvariant(), tenantRole.Name);
                }

                return tenantRoleDescriptions;
            }
        }

        private static IDictionary<string, string> tenantRoleIds;

        public static IDictionary<string, string> TenantRoleIds
        {
            get
            {
                if (tenantRoleIds != null) return tenantRoleIds;

                tenantRoleIds = new Dictionary<string, string>();
                foreach (var tenantRole in GetAllTenantRoles(Roles.ApplicationName))
                {
                    tenantRoleIds.Add(tenantRole.Id.ToLowerInvariant(), tenantRole.Name);
                }

                return tenantRoleDescriptions;
            }
        }

        public string GetRoleNameById(string roleId, string applicationName)
        {
            var roleName = String.Empty;
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var role =
                    (from ar in session.Query<ApplicationRole>()
                     where ar.Id == roleId && ar.ApplicationName == applicationName
                     select ar).FirstOrDefault();
                if (role != null)
                {
                    roleName = role.Name;
                }
            }
            return roleName;
        }

        public string GetRoleIdByRoleName(string roleName, string applicationName)
        {
            var roleId = String.Empty;
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var role =
                    (from ar in session.Query<ApplicationRole>()
                     where ar.Name == roleName && ar.ApplicationName == applicationName
                     select ar).FirstOrDefault();
                if (role != null)
                {
                    roleId = role.Id;
                }
            }
            return roleId;
        }

        public string GetRoleDescriptionById(string roleId, string applicationName)
        {
            var roleDescription = String.Empty;
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var role =
                    (from ar in session.Query<ApplicationRole>()
                     where ar.Id == roleId && ar.ApplicationName == applicationName
                     select ar).FirstOrDefault();
                if (role != null)
                {
                    roleDescription = role.Description;
                    if (String.IsNullOrEmpty(roleDescription))
                    {
                        roleDescription = role.Name;
                    }
                }
            }
            return roleDescription;
        }

        public static void CreateRolesAndNavigationInfo()
        {
            using (var coreSession = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                CreateRolesAndNavigationInfo(coreSession, true);
            }
        }

        private static void CreateRolesAndNavigationInfo(IDocumentSession coreSession, bool commit = false)
        {
            var rolesQuery = coreSession.Query<ApplicationRole>();

            if (rolesQuery.Any())
            {
                return;
            }

            var roleSpecWrapper = LoadRoleSpecsFromJson();

            var navigationManager = new NavigationManager();
            var navigationWrapper = navigationManager.LoadNavigationFromJsonFile();

            //Read from file and create roles from RoleSpec
            if (roleSpecWrapper.RoleSpecs.Any())
            {
                foreach (var appRole in roleSpecWrapper.RoleSpecs.Select(roleSpec => new ApplicationRole(roleSpec.Id, roleSpec.Description, Roles.ApplicationName, null, roleSpec.Type)))
                {
                    coreSession.Store(appRole);
                }
            }

            //Read from navigation and create navigation items
            navigationManager.LoadNavigation(navigationWrapper, coreSession, false);

            if (commit)
            {
                coreSession.SaveChanges();
            }
        }

        public static RoleSpecWrapper LoadRoleSpecsFromJson()
        {
            const string RolesFileFormat = "{0}.json";
            var cf = Catalog.Factory.Resolve<IConfig>();
            
            var filePath = string.Format(RolesFileFormat, cf[ContentFileStorage.RolesSpecConfiguration]);

            filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);

            var rolesSpec = JsonFileSerializer.ExtractObject<RoleSpecWrapper>(filePath);
            return rolesSpec;
        }
    }

}
