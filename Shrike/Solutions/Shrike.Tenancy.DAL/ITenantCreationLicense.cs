using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shrike.Tenancy.DAL
{
    public interface ITenantCreationLicense
    {
        void CheckTenantCreationLicensed();
    }
}
