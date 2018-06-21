using System.Collections.Generic;

namespace Lok.Unik.ModelCommon.Client
{
    using AppComponents.Web;

    public class RoleSpec
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ApplicationRoleType Type { get; set; }
    }

    public class RoleSpecWrapper
    {
        public List<RoleSpec> RoleSpecs { get; set; }
    }
}
