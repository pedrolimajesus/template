using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lok.Unik.ModelCommon.Interfaces;

namespace Lok.Unik.ModelCommon.Client
{
    public class CriteriaFilterTag : ICriteria<Tag, FilterTag>
    {
        public List<Tag> Criteria(List<Tag> entities, FilterTag filter)
        {

            if (filter.Type == FilterType.Default)
            {
                 return entities;
            }

            var tags = from t in entities where filter.Filters.Any(f => f == t.Value) select t;
            return tags.ToList();
        }
    }
}
