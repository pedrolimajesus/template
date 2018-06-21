using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppComponents;

namespace Shrike.Dal.Tests.Context
{
    using AppComponents.Extensions.EnumerableEx;

    using Shrike.DAL.Manager;

    public class TenancyContextProvider : IContextProvider
    {
        private const string DefaultTenancy = DefaultRoles.SuperAdmin;

        private static string tenancy = DefaultTenancy;

        public static string Tenancy
        {
            get
            {
                return tenancy;
            }
            set
            {
                tenancy = value;
            }
        }

        public IEnumerable<Uri> ProvideContexts()
        {
            return EnumerableEx.OfOne(new Uri(string.Format("context://Tenancy/{0}", Tenancy)));
        }
    }
}