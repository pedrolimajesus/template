using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shrike.Resources
{
    public class LocDescriptionAttribute : DescriptionAttribute
    {
        public LocDescriptionAttribute()
        {
        } 

        
        public LocDescriptionAttribute(string key, string defaultDescription) :
            base(defaultDescription)
        {
            Key = key;
            DefaultDescription = defaultDescription;
        }
        public string Key { get; set; }

       
        public string DefaultDescription { get; set; }

        
        public override string Description
        {
            get
            {
                
                string description = ResourceEn.ResourceManager.GetString(Key);
                if (string.IsNullOrEmpty(description))
                {
                    description = DefaultDescription;
                }
                return description;
            }
        } 
    }
}
