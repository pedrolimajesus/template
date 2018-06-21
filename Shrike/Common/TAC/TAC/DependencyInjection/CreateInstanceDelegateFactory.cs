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

namespace AppComponents
{
    internal class CreateInstanceDelegateFactory
    {
        private const string _constructorNotFound =
            "The requested class {0} does not have a public constructor.";


        public static Func<IAssembleObject, object> Create(Type implType)
        {
            ParameterExpression container = Expression.Parameter(typeof (IAssembleObject), "container");
            NewExpression exp = ConstructResolveExpression(implType, container);
            return Expression.Lambda<Func<IAssembleObject, object>>(
                exp,
                new[] {container}
                ).Compile();
        }

        private static NewExpression ConstructResolveExpression(Type type, ParameterExpression container)
        {
            if (!type.IsGenericTypeDefinition)
            {
                ConstructorInfo constructor = ExtractConstructor(type);
                ParameterInfo[] parameters = constructor.GetParameters();


                List<Expression> arguments = new List<Expression>();

                foreach (var parmInfo in parameters)
                {
                    var p = Expression.Call(container, "Resolve", new[] {parmInfo.ParameterType},
                                            new Expression[] {});
                    arguments.Add(p);
                }


                return Expression.New(constructor, arguments);
            }
            return null;
        }

        private static ConstructorInfo ExtractConstructor(Type implType)
        {
            var constructors = implType.GetConstructors();
            var constructor = constructors
                .OrderBy(c => c.GetParameters().Length)
                .LastOrDefault();
            if (constructor == null)
                throw new ArgumentException(String.Format(_constructorNotFound, implType));

            return constructor;
        }
    }
}