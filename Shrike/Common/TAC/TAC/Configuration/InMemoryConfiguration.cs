using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppComponents.Configuration
{
    public class InMemoryConfiguration : DictionaryConfigurationBase
    {
        public InMemoryConfiguration()
        {
            MaybeInitialize();
        }

        public override void FillDictionary()
        {
            
        }

        public object this[string name]
        {
            get { return _configurationCache[name]; }

            set { _configurationCache[name] = value; }
        }
    }
}
