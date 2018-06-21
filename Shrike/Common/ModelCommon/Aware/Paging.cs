using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Lok.Unik.ModelCommon.Aware
{
    public class FilterPaging<T>: Paging
    {
        public Expression<Func<T, bool>> Filter { get; set; }
        
    }

    public class Paging
    {
        public int Take { get; set; }

        public int Skip { get; set; }

        public int TotalResults { get; set; }

    }
}
