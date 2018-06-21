using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Client
{
    using Lok.Unik.ModelCommon.Interfaces;

    public class FilterTag : IFilter<String>
    {
        public FilterTag()
        {
            ChildTag = new HashSet<FilterTag>();
        }

        public virtual string Name
        {
            get;
            set;
        }

        public virtual FilterType Type
        {
            get;
            set;
        }

        public virtual List<String> Filters
        {
            get;
            set;
        }

        public virtual Guid Id
        {
            get;
            set;
        }


        public ICollection<FilterTag> ChildTag
        {
            get;
            set;
        }
    }
}
