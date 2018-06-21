using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PropertyTools;

namespace TAC.Wpf
{
    public class KeySelectorViewModel : Observable
    {
        private string _key;
        public string Key
        {
            get { return _key; }
            set { SetValue(ref _key, value, () => Key); }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetValue(ref _title, value, () => Title); }
        }

        public override string ToString()
        {
            
            return string.Format("{0}: {1}", Key, Title);
        }
    }
}
