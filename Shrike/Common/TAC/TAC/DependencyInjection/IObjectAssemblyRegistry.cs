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
    public interface IObjectAssemblyRegistry
    {
        IInstanceCreationStrategy DefaultInstanceCreationStrategy { get; set; }

        IObjectAssemblySpecification Register<TType>(Func<IAssembleObject, TType> func) where TType : class;

        IObjectAssemblySpecification Register<TType>(string name, Func<IAssembleObject, TType> func) where TType : class;

        IObjectAssemblySpecification Register<TType>(Enum token, Func<IAssembleObject, TType> func) where TType : class;

        IObjectAssemblySpecification Register<TType>(Func<IAssembleObject, TType> func, params Enum[] tokens)
            where TType : class;

        IObjectAssemblySpecification Register(Type type, Func<IAssembleObject, object> func);

        IObjectAssemblySpecification Register(string name, Type type, Func<IAssembleObject, object> func);

        IObjectAssemblySpecification Register(Enum token, Type type, Func<IAssembleObject, object> func);

        IObjectAssemblySpecification Register(Type type, Func<IAssembleObject, object> func, params Enum[] tokens);

        IObjectAssemblySpecification RegisterInstance<TType>(TType instance) where TType : class;

        IObjectAssemblySpecification RegisterInstance<TType>(string name, TType instance) where TType : class;

        IObjectAssemblySpecification RegisterInstance<TType>(Enum token, TType instance) where TType : class;

        IObjectAssemblySpecification RegisterInstance<TType>(TType instance, params Enum[] tokens) where TType : class;

        IObjectAssemblySpecification RegisterInstance(Type type, object instance);

        IObjectAssemblySpecification RegisterInstance(string name, Type type, object instance);

        IObjectAssemblySpecification RegisterInstance(Enum token, Type type, object instance);

        IObjectAssemblySpecification RegisterInstance(Type type, object instance, params Enum[] tokens);

        IObjectAssemblySpecification Register<TType, TImpl>()
            where TType : class
            where TImpl : class, TType;

        IObjectAssemblySpecification Register<TType, TImpl>(string name)
            where TType : class
            where TImpl : class, TType;

        IObjectAssemblySpecification Register<TType, TImpl>(Enum token)
            where TType : class
            where TImpl : class, TType;

        IObjectAssemblySpecification Register<TType, TImpl>(params Enum[] token)
            where TType : class
            where TImpl : class, TType;

        IObjectAssemblySpecification Register(Type tType, Type tImpl);

        IObjectAssemblySpecification Register(string name, Type tType, Type tImpl);

        IObjectAssemblySpecification Register(Enum token, Type tType, Type tImpl);

        IObjectAssemblySpecification Register(Type tType, Type tImpl, params Enum[] tokens);

        void Remove(IObjectAssemblySpecification ireg);

        IObjectAssemblySpecification GetRegistration<TType>() where TType : class;

        IObjectAssemblySpecification GetRegistration<TType>(string name) where TType : class;

        IObjectAssemblySpecification GetRegistration<TType>(Enum token) where TType : class;

        IObjectAssemblySpecification GetRegistration<TType>(params Enum[] tokens) where TType : class;

        IObjectAssemblySpecification GetRegistration(Type type);

        IObjectAssemblySpecification GetRegistration(string name, Type type);

        IObjectAssemblySpecification GetRegistration(Enum token, Type type);

        IObjectAssemblySpecification GetRegistration(Type type, params Enum[] tokens);

        IEnumerable<IObjectAssemblySpecification> GetRegistrations<TType>() where TType : class;

        IEnumerable<IObjectAssemblySpecification> GetRegistrations(Type type);

        IEnumerable<IObjectAssemblySpecification> GetAllRegistrations();
    }
}