using System.Collections.Generic;

namespace Lok.Unik.ModelCommon.Client
{
    public class Navigation
    {
        public Navigation()
        {
            Role = string.Empty;
            NavigationItems = new List<NavigationItem>();
        }

        public string Role { get; set; }
    
        public List<NavigationItem> NavigationItems { get; set; }
    }

    public class NavigationItem
    {
        public string ImageSrc { get; set; }

        public string Title { get; set; }

        public List<ViewItem> ViewItems { get; set; }
    }

    public class ViewItem
    {
        public string ActionName { get; set; }

        public string AreaName { get; set; }

        public string ControllerName { get; set; }

        public List<ViewCommand> ViewCommands { get; set; }
    
    }

    public class ViewCommand
    {
        public string EventOnclick { get; set; }

        public string AltImage { get; set; }

        public string SrcImage { get; set; }

        public string Title { get; set; }
    }

    /// <summary>
    /// The JSON Array Object deserialized into a list of navigations
    /// </summary>
    public class NavigationWrapper
    {
        public List<Navigation> Navigations { get; set; }
    }
}
