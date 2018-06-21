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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AppComponents.Extensions.Predicate
{
    public static class Evaluator
    {
        public static MemberInfo ExtractPropertyInfoFuzzy(Expression expr)
        {
            MemberInfo retval = null;

            try
            {
                var propExpr = ((LambdaExpression) expr).Body as MemberExpression;
                retval = propExpr.Member;
            }
            catch
            {
                throw new ArgumentException("Property expression must be of the form 'x => x.SomeProperty'");
            }

            return retval;
        }

        public static MemberInfo ExtractPropertyInfo<Entity, R>(Expression<Func<Entity, R>> expr)
        {
            return ExtractPropertyInfoFuzzy(expr);
        }

        /// <summary>
        ///   var m = Evaluator.ExtractMethodInfo[MyInterface](x=>x.DoSomething(null,null);
        /// </summary>
        /// <typeparam name="T"> </typeparam>
        /// <param name="action"> </param>
        /// <returns> </returns>
        public static MemberInfo ExtractMethodInfo<T>(Expression<Action<T>> action)
        {
            MethodCallExpression methodCall = action.Body as MethodCallExpression;
            if (methodCall == null)
            {
                throw new ArgumentException("Only method calls are supported");
            }
            return methodCall.Method;
        }

        /// <summary>
        ///   var m = Evaluator.ExtractMethodInfoSpec[IMyInterface]( x=> new Action[string, string](x.DoSomething) );
        /// </summary>
        /// <typeparam name="T"> </typeparam>
        /// <param name="expression"> </param>
        /// <returns> </returns>
        public static MemberInfo ExtractMethodInfoSpec<T>(Expression<Func<T, Delegate>> expression)
        {
            var unaryExpression = (UnaryExpression) expression.Body;
            var methodCallExpression = (MethodCallExpression) unaryExpression.Operand;
            var methodInfoExpression = (ConstantExpression) methodCallExpression.Arguments.Last();
            var methodInfo = (MemberInfo) methodInfoExpression.Value;
            return methodInfo;
        }

        public static string Translate(this Expression that)
        {
            var ex = PartialEval(that);
            return ex.ToString();
        }


        public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
        {
            return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
        }


        public static Expression PartialEval(Expression expression)
        {
            return PartialEval(expression, CanBeEvaluatedLocally);
        }

        private static bool CanBeEvaluatedLocally(Expression expression)
        {
            return expression.NodeType != ExpressionType.Parameter;
        }

        #region Nested type: Nominator

        internal class Nominator : ExpressionVisitor
        {
            private HashSet<Expression> candidates;
            private bool cannotBeEvaluated;
            private Func<Expression, bool> fnCanBeEvaluated;

            internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
            {
                this.fnCanBeEvaluated = fnCanBeEvaluated;
            }

            internal HashSet<Expression> Nominate(Expression expression)
            {
                candidates = new HashSet<Expression>();
                Visit(expression);
                return candidates;
            }

            public override Expression Visit(Expression expression)
            {
                if (expression != null)
                {
                    bool saveCannotBeEvaluated = cannotBeEvaluated;
                    cannotBeEvaluated = false;
                    base.Visit(expression);
                    if (!cannotBeEvaluated)
                    {
                        if (fnCanBeEvaluated(expression))
                        {
                            candidates.Add(expression);
                        }
                        else
                        {
                            cannotBeEvaluated = true;
                        }
                    }
                    cannotBeEvaluated |= saveCannotBeEvaluated;
                }
                return expression;
            }
        }

        #endregion

        #region Nested type: SubtreeEvaluator

        internal class SubtreeEvaluator : ExpressionVisitor
        {
            private HashSet<Expression> candidates;

            internal SubtreeEvaluator(HashSet<Expression> candidates)
            {
                this.candidates = candidates;
            }

            internal Expression Eval(Expression exp)
            {
                return Visit(exp);
            }

            public override Expression Visit(Expression exp)
            {
                if (exp == null)
                {
                    return null;
                }
                if (candidates.Contains(exp))
                {
                    return Evaluate(exp);
                }
                return base.Visit(exp);
            }


            private Expression Evaluate(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant)
                {
                    return e;
                }
                LambdaExpression lambda = Expression.Lambda(e);
                Delegate fn = lambda.Compile();
                return Expression.Constant(fn.DynamicInvoke(null), e.Type);
            }
        }

        #endregion
    }
}