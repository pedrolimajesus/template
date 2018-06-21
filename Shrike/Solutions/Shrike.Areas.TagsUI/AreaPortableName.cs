using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shrike.Areas.TagsUI
{
    public class AreaPortableName
    {
        public static Shrike.Areas.TagsUI.TagsUI.TagsUIAreaRegistration areaTagsUI = new TagsUI.TagsUIAreaRegistration();

        public static string AreaName
        {
            get { return areaTagsUI.AreaName;}
        }
    }
}