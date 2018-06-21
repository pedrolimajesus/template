using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppComponents.Web.Interfaces
{
    interface IRoleManager
    {
        ApplicationRole GetCompleteRole(string roleName, string applicationName);
    }
}
