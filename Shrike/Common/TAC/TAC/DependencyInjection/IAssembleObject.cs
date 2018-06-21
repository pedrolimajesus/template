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

namespace AppComponents
{
    public interface IAssembleObject
    {
        TType Resolve<TType>() where TType : class;

        TType Resolve<TType>(string name) where TType : class;

        TType Resolve<TType>(Enum token) where TType : class;

        TType Resolve<TType>(params Enum[] tokens) where TType : class;

        object Resolve(Type type);

        object Resolve(string name, Type type);

        object Resolve(Enum token, Type type);

        object Resolve(Type type, params Enum[] tokens);

        IEnumerable<TType> ResolveAll<TType>() where TType : class;

        IEnumerable<object> ResolveAll(Type type);

        Func<TType> LazyResolve<TType>() where TType : class;

        Func<TType> LazyResolve<TType>(string name) where TType : class;

        Func<TType> LazyResolve<TType>(Enum token) where TType : class;

        Func<TType> LazyResolve<TType>(params Enum[] tokens) where TType : class;

        Func<object> LazyResolve(Type type);

        Func<object> LazyResolve(string name, Type type);

        Func<object> LazyResolve(Type type, params Enum[] tokens);

        bool CanResolve<TType>() where TType : class;

        bool CanResolve<TType>(string name) where TType : class;

        bool CanResolve<TType>(Enum token) where TType : class;

        bool CanResolve<TType>(params Enum[] tokens) where TType : class;

        bool CanResolve(Type type);

        bool CanResolve(string name, Type type);

        bool CanResolve(Enum token, Type type);

        bool CanResolve(Type type, params Enum[] tokens);
    }
}