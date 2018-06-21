using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Shrike.Tenancy.Web
{
    public class ContextAuthorizeAttribute: AuthorizeAttribute
    {
         /// <summary>
        /// Authorize to Item or Controller name
        /// </summary>
        /// <param name="authTo">Authorize item or Controllers</param>
        public ContextAuthorizeAttribute(string authTo)
        {
            string rolesValue;

            if (ContextRoleRegister.RegisteredRoles.TryGetValue(authTo, out rolesValue))
                Roles = rolesValue;
            else
                throw new NotSupportedException(string.Format("Auth '{0}' is not Supported.", authTo));
        }
    }

    /// <summary>
    /// Provides Context role register for Authorize.
    /// </summary>
    public static class ContextRoleRegister
    {
        static ContextRoleRegister()
        {
            RegisteredRoles = new Dictionary<string, string>();
        }

        public static Dictionary<string, string> RegisteredRoles { get; set; }
    }
}
