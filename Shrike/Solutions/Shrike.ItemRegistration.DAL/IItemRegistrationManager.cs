using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shrike.ItemRegistration.DAL
{
    using Lok.Unik.ModelCommon.ItemRegistration;

    public interface IItemRegistrationManager<out TItem>
    {
        TItem RegisterItem(ItemRegistrationResult result, ItemRegistration itemRegistration);
    }
}
