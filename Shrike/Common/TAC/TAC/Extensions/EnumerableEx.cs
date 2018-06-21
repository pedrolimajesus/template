using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace AppComponents.Extensions.EnumerableEx
{
    public static partial class EnumerableExtensions
    {
        public static Dictionary<TKey1, Dictionary<TKey2, TValue>> Pivot3<TSource, TKey1, TKey2, TValue>(
            this IEnumerable<TSource> source
            , Func<TSource, TKey1> key1Selector
            , Func<TSource, TKey2> key2Selector
            , Func<IEnumerable<TSource>, TValue> aggregate)
        {
            return source.GroupBy(key1Selector).Select(
                x => new
                         {
                             X = x.Key,
                             Y = source.GroupBy(key2Selector).Select(
                                 z => new
                                          {
                                              Z = z.Key,
                                              V = aggregate(from item in source
                                                            where key1Selector(item).Equals(x.Key)
                                                                  && key2Selector(item).Equals(z.Key)
                                                            select item
                                          )
                                          }
                         ).ToDictionary(e => e.Z, o => o.V)
                         }
                ).ToDictionary(e => e.X, o => o.Y);
        }

        public static DataTable ToDataTable<T>(this IEnumerable<T> varlist)
        {
            var dtReturn = new DataTable();

            // column names 
            PropertyInfo[] oProps = null;

            if (varlist == null) return dtReturn;

            foreach (T rec in varlist)
            {
                // Use reflection to get property names, to create table, Only first time, others will follow 
                if (oProps == null)
                {
                    oProps = (rec.GetType()).GetProperties();
                    foreach (PropertyInfo pi in oProps)
                    {
                        Type colType = pi.PropertyType;

                        if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition() == typeof (Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }

                        dtReturn.Columns.Add(new DataColumn(pi.Name, colType));
                    }
                }

                DataRow dr = dtReturn.NewRow();

                foreach (PropertyInfo pi in oProps)
                {
                    dr[pi.Name] = pi.GetValue(rec, null) == null
                                      ? DBNull.Value
                                      : pi.GetValue
                                            (rec, null);
                }

                dtReturn.Rows.Add(dr);
            }
            return dtReturn;
        }

       
        public static int FindIndex<T>(this IEnumerable<T> list, Predicate<T> finder)
        {
           
            return list.Select((item, index) => new { item, index })
                .Where(p => finder(p.item))
                .Select(p => p.index + 1)
                .FirstOrDefault() - 1;
        }

        private static Random _rng = new Random();
        public static T RandomSelect<T>(this IEnumerable<T> list)
        {
            var index = _rng.Next()%list.Count();
            return list.ElementAt(index);
        }
    }
}