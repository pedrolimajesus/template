using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Interfaces
{
    public enum FilterType
    {
        Device,
        User,
        Content,
        Root,
        Default
    }

    public interface IFilter<E>
    {
        Guid Id
        {
            get;
            set;
        }

        string Name
        {
            get;
            set;
        }

        FilterType Type
        {
            get;
            set;
        }

        List<E> Filters
        {
            get;
            set;
        }
    }

    public interface ITimeFilter
    {
        DateTime TimeRegistered { get; set; }

        DateTime TimeUpdated { get; set; }
    }

}
