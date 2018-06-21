// // 
// //  Copyright 2012 David Gressett
// // 
// //    Licensed under the Apache License, Version 2.0 (the "License");
// //    you may not use this file except in compliance with the License.
// //    You may obtain a copy of the License at
// // 
// //        http://www.apache.org/licenses/LICENSE-2.0
// // 
// //    Unless required by applicable law or agreed to in writing, software
// //    distributed under the License is distributed on an "AS IS" BASIS,
// //    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //    See the License for the specific language governing permissions and
// //    limitations under the License.

using System;
using System.Linq;
using System.Linq.Expressions;
using AppComponents.Data;

namespace AppComponents.Extensions.QuerySpecificationEx
{
    public static class QuerySpecificationExtensions
    {
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, string sortColumn, SortOrder direction)
        {
            string methodName = string.Format("OrderBy{0}",
                                              direction == SortOrder.Descending ? "descending" : "");

            ParameterExpression parameter = Expression.Parameter(query.ElementType, "p");

            MemberExpression memberAccess = null;
            foreach (var property in sortColumn.Split('.'))
                memberAccess = Expression.Property
                    (memberAccess ?? (parameter as Expression), property);

            LambdaExpression orderByLambda = Expression.Lambda(memberAccess, parameter);

            MethodCallExpression result = Expression.Call(
                typeof (Queryable),
                methodName,
                new[] {query.ElementType, memberAccess.Type},
                query.Expression,
                Expression.Quote(orderByLambda));

            return query.Provider.CreateQuery<T>(result);
        }


        public static IQueryable<T> Where<T>(
            this IQueryable<T> query,
            string column,
            object value,
            Test comparisonTest)
        {
            if (string.IsNullOrEmpty(column))
                return query;

            ParameterExpression parameter = Expression.Parameter(query.ElementType, "p");

            MemberExpression memberAccess = null;
            foreach (var property in column.Split('.'))
                memberAccess = Expression.Property
                    (memberAccess ?? (parameter as Expression), property);

            ConstantExpression filter = Expression.Constant
                (
                    Convert.ChangeType(value, memberAccess.Type)
                );


            Expression condition = null;
            LambdaExpression lambda = null;
            switch (comparisonTest)
            {
                case Test.Equal:
                    condition = Expression.Equal(memberAccess, filter);
                    lambda = Expression.Lambda(condition, parameter);
                    break;


                case Test.NotEqual:
                    condition = Expression.NotEqual(memberAccess, filter);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                case Test.LessThan:
                    condition = Expression.LessThan(memberAccess, filter);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                case Test.LessThanEqual:
                    condition = Expression.LessThanOrEqual(memberAccess, filter);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                case Test.GreaterThan:
                    condition = Expression.GreaterThan(memberAccess, filter);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                case Test.GreaterThanEqual:
                    condition = Expression.GreaterThanOrEqual(memberAccess, filter);
                    lambda = Expression.Lambda(condition, parameter);
                    break;

                case Test.Contains:
                    condition = Expression.Call(memberAccess,
                                                typeof (string).GetMethod("Contains"),
                                                Expression.Constant(value));
                    lambda = Expression.Lambda(condition, parameter);
                    break;
            }


            MethodCallExpression result = Expression.Call(
                typeof (Queryable), "Where",
                new[] {query.ElementType},
                query.Expression,
                lambda);

            return query.Provider.CreateQuery<T>(result);
        }

        public static IQueryable<T> Page<T>(this IQueryable<T> that, IPageBookmark bm)
        {
            if (bm.CurrentPage > 0)
            {
                return
                       that
                           .Skip((bm.CurrentPage * bm.PageSize) + bm.LastSkippedResults)
                           .Take(bm.PageSize);
            }
            else
            {
                return
                       that
                           .Take(bm.PageSize);
            }
        }

        public static IQueryable<T> ApplySpecFilter<T>(this IQueryable<T> that, QuerySpecification spec)
        {
            IQueryable<T> retval = that;
            if (spec.Where != null)
            {
                if (spec.Where.Rules.Count == 1)
                {
                    var rule = spec.Where.Rules.First();
                    retval = that.Where(rule.Field, rule.Data, rule.Test);
                }
                else
                {
                    if (spec.Where.PredicateJoin == PredicateJoin.And)
                    {
                        foreach (var rule in spec.Where.Rules)
                        {
                            retval = retval.Where(rule.Field, rule.Data, rule.Test);
                        }
                    }
                    else
                    {
                        foreach (var rule in spec.Where.Rules)
                        {
                            var part = that.Where(rule.Field, rule.Data, rule.Test);
                            retval = retval.Concat(part);
                        }

                        retval = retval.Distinct();
                    }
                }
            }

            if (!string.IsNullOrEmpty(spec.SortOn))
                retval = retval.OrderBy(spec.SortOn, spec.SortOrder);


            return retval;
        }

        /*public static IQueryable<T> ApplySpecFilter<T>(this IQueryable<T> that, QuerySpecification spec)
        {
            IQueryable<T> retval = that;
            if (spec.Where != null)
            {
                if (spec.Where.PredicateJoin == PredicateJoin.And)
                {
                    foreach (var rule in spec.Where.Rules)
                    {
                        retval = retval.Where(rule.Field, rule.Data, rule.Test);
                    }
                }
                else
                {
                    foreach (var rule in spec.Where.Rules)
                    {
                        var part = that.Where(rule.Field, rule.Data, rule.Test);
                        retval = retval.Concat(part);
                    }

                    retval = retval.Distinct();
                }
            }

            if(!string.IsNullOrEmpty(spec.SortOn))
                retval = retval.OrderBy(spec.SortOn, spec.SortOrder);


            return retval;
        }   */
    }
}