namespace Shrike.Data.Reports.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class ListExtension
    {
        public static IEnumerable<TEntity> WhereInList<TEntity, TValue>(this IEnumerable<TEntity> query,
                                                      TValue value, Func<TEntity, IEnumerable<TValue>> selector)
        {
            List<TEntity> res = new List<TEntity>();
            foreach (TEntity ent in query)
            {
                var list2 = selector(ent);

                if (list2.Contains(value))
                {
                    res.Add(ent);
                }
            }
            return res;
        }
    }
}
