using System;
using System.Linq;
using System.Web.Security;
using System.Collections.Specialized;
using AppComponents.Raven;
using Raven.Client.Linq;
using log4net;


namespace AppComponents.Web
{
    using System.Web;

    using AppComponents.Web.Interfaces;

    public class RavenDBRoleProvider : RoleProvider
	{
        private const string ProviderName = "RavenDBRoleProvider";

        private readonly ILog _log = ClassLogger.Create(typeof(RavenDBRoleProvider));

        private string CurrentTenancy
        {
            get
            {
                return HttpContext.Current.Request.RequestContext.RouteData.GetRequiredString("tenant");
            }
        }

		public override void Initialize(string name, NameValueCollection config)
		{
		    if(config == null)
                throw new ArgumentNullException("config", "There are not membership configuration settings.");
            if(string.IsNullOrEmpty(name))
                name = ProviderName;
            if(string.IsNullOrEmpty(config["description"]))
                config["description"] = "An Asp.Net membership provider for the RavenDB document database.";
         
            ApplicationName = string.IsNullOrEmpty(config["applicationName"]) ? System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath : config["applicationName"];

            base.Initialize(name, config);
            
            InitConfigSettings(config);			
		}

        private void InitConfigSettings(NameValueCollection config) {

        }

		public override string ApplicationName { get; set; }

		public override void AddUsersToRoles(string[] usernames, string[] roleNames)
		{
			if (usernames.Length == 0 || roleNames.Length == 0)
			{
				return;
			}

			using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
			{
				try
				{
                    var users = (from u in session.Query<ApplicationUser>()
                                where u.UserName.In(usernames)
                                && u.Tenancy == CurrentTenancy
                                select u).ToList();  
                    
                    var roles = (from r in session.Query<ApplicationRole>()
                                where r.Name.In(roleNames)
                                && r.ApplicationName == ApplicationName
                                select r.Id).ToList();
					
					foreach (var roleId in roles)
					{
					    var id = roleId;
					    foreach (var user in users.Where(user => !user.AccountRoles.Contains(id)))
						{
						    user.AccountRoles.Add(roleId);
						}
					}
				    session.SaveChanges();
				}
				catch (Exception ex)
				{
				    var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                    aa.RaiseAlert(ApplicationAlertKind.Security, ex);
					throw;
				}
			}
		}

        public override void CreateRole(string roleName)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                try
                {
                    
                   
                    
                    var role = new ApplicationRole(roleName, roleName, this.ApplicationName, null);
                    
                    session.Store(role);
                    session.SaveChanges();
                }
                catch (Exception ex)
                {
                    var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                    aa.RaiseAlert(ApplicationAlertKind.Security, ex);
                    throw;
                }
            }
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
		{
			using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
			{
				try
				{
					var role = (from r in session.Query<ApplicationRole>()
							   where r.Name == roleName && r.ApplicationName == ApplicationName
							   select r).SingleOrDefault();
					if (role != null)
					{
						// also find users that have this role (in all tenancies)
						var users = (from u in session.Query<ApplicationUser>()
									where u.AccountRoles.Any(roleId => roleId == role.Id)
									select u).ToList();
						if (users.Any() && throwOnPopulatedRole)
						{
							throw new ApplicationException(String.Format("ApplicationRole {0} contains members and cannot be deleted.", role.Name));
						}

						foreach (var user in users)
						{
							user.AccountRoles.Remove(role.Id);
						}

						session.Delete(role);
						session.SaveChanges();
						return true;
					}
					return false;
				}
				catch (Exception ex)
				{
                    var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                    aa.RaiseAlert(ApplicationAlertKind.Security, ex);
					throw;
				}
			}
		}

		public override string[] FindUsersInRole(string roleName, string usernameToMatch)
		{
			using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
			{
			
				var role = (from r in session.Query<ApplicationRole>()
							where r.Name == roleName && r.ApplicationName == ApplicationName
							select r).FirstOrDefault();
				if (role != null)
				{
			
					var users = from u in session.Query<ApplicationUser>()
								where u.AccountRoles.Any(x => x == role.Id) && u.UserName == usernameToMatch
								select u.UserName;
					return users.ToArray();
				}
				return null;
			}
		}

		public override string[] GetAllRoles()
		{
			using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
			{
				var roles = (from r in session.Query<ApplicationRole>()
							where r.ApplicationName == ApplicationName
							select r).ToList();
				return roles.Select(r => r.Name).ToArray();
			}
		}

		public override string[] GetRolesForUser(string username)
		{
			using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
			{
				var user = (from u in session.Query<ApplicationUser>()
							where u.UserName == username
                            && u.Tenancy == CurrentTenancy
							select u).SingleOrDefault();
				
                if (user!=null && user.AccountRoles.Any())
				{
                    var dbRoles = session.Query<ApplicationRole>().Where(x => x.Id.In(user.AccountRoles));
					return dbRoles.Select(r => r.Name).ToArray();
				}
				return new string[0];
			}
		}

        public override string[] GetUsersInRole(string roleName)
        {
            using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var role =
                    (from r in session.Query<ApplicationRole>()
                     where r.Name == roleName && r.ApplicationName == ApplicationName
                     select r).SingleOrDefault();

                if (role != null)
                {
                    if (role.Type == ApplicationRoleType.Owner)
                    {
                        return
                            (from u in session.Query<ApplicationUser>()
                             where u.AccountRoles.Any(x => x == role.Id)
                             select u.UserName).ToArray();
                    }

                    var usernames = from u in session.Query<ApplicationUser>()
                                    where u.AccountRoles.Any(x => x == role.Id) && u.Tenancy == this.CurrentTenancy
                                    select u.UserName;
                    return usernames.ToArray();
                }

                return null;
            }
        }

        public override bool IsUserInRole(string username, string roleName)
		{
			using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
			{
                var user = session.Query<ApplicationUser>().FirstOrDefault(u => u.UserName == username && u.Tenancy == CurrentTenancy);

				if (user != null)
				{
					var role = (from r in session.Query<ApplicationRole>()
								where r.Name == roleName && r.ApplicationName == ApplicationName
								select r.Id).FirstOrDefault();
					if (role != null)
					{
						return user.AccountRoles.Any(x => x == role);
					}
				}

				return false;
			}
		}

		public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
		{
			if (usernames.Length == 0 || roleNames.Length == 0)
			{
				return;
			}

			using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
			{
				try
				{
                    var users = (from u in session.Query<ApplicationUser>()
                                 where u.UserName.In(usernames)
                                 && u.Tenancy == CurrentTenancy
                                 select u).ToList();

                    var roles = (from r in session.Query<ApplicationRole>()
                                 where r.Name.In(roleNames) && r.ApplicationName == ApplicationName
                                 select r.Id).ToList();
                        
					foreach (var roleId in roles)
					{
					    var id = roleId;
					    var usersWithRole = users.Where(u => u.AccountRoles.Any(x => x == id));
						foreach (var user in usersWithRole)
						{
							user.AccountRoles.Remove(roleId);
						}
					}
					session.SaveChanges();
				}
				catch (Exception ex)
				{
					_log.ErrorFormat("An exception occured with the following message: {0}", ex.Message);
					Console.WriteLine(ex.ToString());
					throw;
				}
			}
		}

		public override bool RoleExists(string roleName)
		{
			using (var session = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
			{
				return session.Query<ApplicationRole>().Any(r => r.Name == roleName || r.Id == roleName);
			}
		}
	}
}
