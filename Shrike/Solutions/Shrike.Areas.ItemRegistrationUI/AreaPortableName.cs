using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shrike.Areas.ItemRegistrationUI
{
    public class AreaPortableName
    {
        public static Shrike.Areas.ItemRegistrationUI.ItemRegistrationUI.ItemRegistrationUIAreaRegistration
            areaItemRegistrationUI = new ItemRegistrationUI.ItemRegistrationUIAreaRegistration();

        public static string AreaName
        {
            get {return areaItemRegistrationUI.AreaName;}
        }
    }
}