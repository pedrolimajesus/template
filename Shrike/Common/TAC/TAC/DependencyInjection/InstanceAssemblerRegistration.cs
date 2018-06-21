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
using AppComponents.Extensions.EnumEx;

namespace AppComponents
{
    public partial class InstanceAssembler : IObjectAssemblyRegistry
    {
        private const string RegistrationNotFound = "Cannot locate object assembly registration for {0}";

        #region IObjectAssemblyRegistry Members

        public IObjectAssemblySpecification Register<TType>(Func<IAssembleObject, TType> func)
            where TType : class
        {
            return Register(string.Empty, typeof (TType), c => (func(c) as Object));
        }


        public IObjectAssemblySpecification Register<TType>(string name, Func<IAssembleObject, TType> func)
            where TType : class
        {
            return Register(name, typeof (TType), c => (func(c) as Object));
        }


        public IObjectAssemblySpecification Register(Type type, Func<IAssembleObject, object> func)
        {
            return Register(string.Empty, type, func);
        }


        public IObjectAssemblySpecification Register(string name, Type type, Func<IAssembleObject, object> func)
        {
            if (func == null)
                throw new ArgumentNullException("func");

            var entry = new ObjectAssemblySpecification(this, name, type, func);
            entry.WithInstanceCreationStrategy(DefaultInstanceCreationStrategy);
            _typeRegistry.Add(entry);
            return entry;
        }


        public IObjectAssemblySpecification Register<TType, TImpl>()
            where TType : class
            where TImpl : class, TType
        {
            return Register(typeof (TType), typeof (TImpl));
        }


        public IObjectAssemblySpecification Register<TType, TImpl>(string name)
            where TType : class
            where TImpl : class, TType
        {
            return Register(name, typeof (TType), typeof (TImpl));
        }


        public IObjectAssemblySpecification Register(Type tType, Type tImpl)
        {
            return Register(string.Empty, tType, tImpl);
        }


        public IObjectAssemblySpecification Register(string name, Type tType, Type tImpl)
        {
            if (tType.ContainsGenericParameters)
                return RegisterOpenType(name, tType, tImpl);
            else
                return Register(name, tType, CreateInstanceDelegateFactory.Create(tImpl));
        }


        public IObjectAssemblySpecification RegisterInstance<TType>(TType instance) where TType : class
        {
            return Register(c => instance).WithInstanceCreationStrategy(null);
        }


        public IObjectAssemblySpecification RegisterInstance<TType>(string name, TType instance) where TType : class
        {
            return Register(name, c => instance).WithInstanceCreationStrategy(null);
        }


        public IObjectAssemblySpecification RegisterInstance(Type type, object instance)
        {
            return Register(type, c => instance).WithInstanceCreationStrategy(null);
        }


        public IObjectAssemblySpecification RegisterInstance(string name, Type type, object instance)
        {
            return Register(name, type, c => instance).WithInstanceCreationStrategy(null);
        }


        public void Remove(IObjectAssemblySpecification ireg)
        {
            _typeRegistry.Remove(ireg);
        }


        public IObjectAssemblySpecification GetRegistration<TType>() where TType : class
        {
            return GetRegistration(string.Empty, typeof (TType));
        }


        public IObjectAssemblySpecification GetRegistration<TType>(string name) where TType : class
        {
            return GetRegistration(name, typeof (TType));
        }


        public IObjectAssemblySpecification GetRegistration(Type type)
        {
            return GetRegistration(string.Empty, type);
        }


        public IObjectAssemblySpecification GetRegistration(string name, Type type)
        {
            try
            {
                return _typeRegistry.Get(name, type);
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(String.Format(RegistrationNotFound, type), ex);
            }
        }


        public IEnumerable<IObjectAssemblySpecification> GetRegistrations<TType>() where TType : class
        {
            return GetRegistrations(typeof (TType));
        }


        public IEnumerable<IObjectAssemblySpecification> GetRegistrations(Type type)
        {
            return _typeRegistry.All(type).Cast<IObjectAssemblySpecification>();
        }


        public IEnumerable<IObjectAssemblySpecification> GetAllRegistrations()
        {
            return _typeRegistry.All().Cast<IObjectAssemblySpecification>();
        }


        public IObjectAssemblySpecification Register<TType>(Enum token, Func<IAssembleObject, TType> func)
            where TType : class
        {
            return Register(token.EnumName(), func);
        }

        public IObjectAssemblySpecification Register<TType>(Func<IAssembleObject, TType> func, params Enum[] tokens)
            where TType : class
        {
            return Register(EnumExtensions.CombinedToken(tokens), func);
        }

        public IObjectAssemblySpecification Register(Enum token, Type type, Func<IAssembleObject, object> func)
        {
            return Register(token.EnumName(), type, func);
        }

        public IObjectAssemblySpecification Register(Type type, Func<IAssembleObject, object> func, params Enum[] tokens)
        {
            return Register(EnumExtensions.CombinedToken(tokens), type, func);
        }

        public IObjectAssemblySpecification RegisterInstance<TType>(Enum token, TType instance) where TType : class
        {
            return RegisterInstance(token.EnumName(), instance);
        }

        public IObjectAssemblySpecification RegisterInstance<TType>(TType instance, params Enum[] tokens)
            where TType : class
        {
            return RegisterInstance(EnumExtensions.CombinedToken(tokens), instance);
        }

        public IObjectAssemblySpecification RegisterInstance(Enum token, Type type, object instance)
        {
            return RegisterInstance(token.EnumName(), type, instance);
        }

        public IObjectAssemblySpecification RegisterInstance(Type type, object instance, params Enum[] tokens)
        {
            return RegisterInstance(EnumExtensions.CombinedToken(tokens), type, instance);
        }

        public IObjectAssemblySpecification Register<TType, TImpl>(Enum token)
            where TType : class
            where TImpl : class, TType
        {
            return Register<TType, TImpl>(token.EnumName());
        }

        public IObjectAssemblySpecification Register<TType, TImpl>(params Enum[] tokens)
            where TType : class
            where TImpl : class, TType
        {
            return Register<TType, TImpl>(EnumExtensions.CombinedToken(tokens));
        }

        public IObjectAssemblySpecification Register(Enum token, Type tType, Type tImpl)
        {
            return Register(token.EnumName(), tType, tImpl);
        }

        public IObjectAssemblySpecification Register(Type tType, Type tImpl, params Enum[] tokens)
        {
            return Register(EnumExtensions.CombinedToken(tokens), tType, tImpl);
        }

        public IObjectAssemblySpecification GetRegistration<TType>(Enum token) where TType : class
        {
            return GetRegistration<TType>(token.EnumName());
        }

        public IObjectAssemblySpecification GetRegistration<TType>(params Enum[] tokens) where TType : class
        {
            return GetRegistration<TType>(EnumExtensions.CombinedToken(tokens));
        }

        public IObjectAssemblySpecification GetRegistration(Enum token, Type type)
        {
            return GetRegistration(token.EnumName(), type);
        }

        public IObjectAssemblySpecification GetRegistration(Type type, params Enum[] tokens)
        {
            return GetRegistration(EnumExtensions.CombinedToken(tokens), type);
        }

        #endregion

        private IObjectAssemblySpecification RegisterOpenType(string name, Type tType, Type tImpl)
        {
            var entry = new ObjectAssemblySpecification(this, name, tType, tImpl);
            entry.WithInstanceCreationStrategy(DefaultInstanceCreationStrategy);
            _openTypeRegistry.Add(entry);
            return entry;
        }
    }
}