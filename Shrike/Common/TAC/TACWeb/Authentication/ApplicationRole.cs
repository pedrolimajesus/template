using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppComponents.Web
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public static class Tenants
    {
        public static string SuperAdmin = "SuperAdmin";
    }

    public enum ApplicationRoleType
    {
        Tenant = 0,

        Owner = 1
    }

    public class ApplicationRole
    {
        private const string DefaultNameSpaceFormat = "authorization/roles/{0}";

        public string Id { get; set; }

        private string GenerateId()
        {
            if (!String.IsNullOrEmpty(this.ParentRoleId))
            {
                return this.ParentRoleId + "/" + this.Name;
            }
            return this.ApplicationName == "/"
                       ? string.Format(DefaultNameSpaceFormat, Name)
                       : string.Format(DefaultNameSpaceFormat, this.ApplicationName + "/" + Name);
        }

        public string ApplicationName { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string ParentRoleId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ApplicationRoleType Type { get; set; }

        public ApplicationRole()
        {
            
        }

        public ApplicationRole(
            string name, string description, string applicationName, ApplicationRole parentApplicationRole)
        {
            this.Name = name;
            this.Description = description;
            this.ApplicationName = applicationName;
            if (parentApplicationRole != null)
            {
                this.ParentRoleId = parentApplicationRole.Id;
            }

            this.Id = this.GenerateId();
        }

        public ApplicationRole(
            string name,
            string description,
            string applicationName,
            ApplicationRole parentApplicationRole,
            ApplicationRoleType type)
            : this(name, description, applicationName, parentApplicationRole)
        {
            this.Type = type;
        }
    }
}
