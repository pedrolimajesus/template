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
    public partial class InstanceAssembler : IAssembleObject
    {
        #region IAssembleObject Members

        public TType Resolve<TType>() where TType : class
       {
            return Resolve(string.Empty, typeof (TType)) as TType;
        }

        public TType Resolve<TType>(string name) where TType : class
        {
            return Resolve(name, typeof (TType)) as TType;
        }


        public object Resolve(Type type)
        {
            return Resolve(string.Empty, type);
        }


        public object Resolve(string name, Type type)
        {
            try
            {
                return _typeRegistry.Get(name, type).GetInstance();
            }
            catch (KeyNotFoundException knfe)
            {
                return HandleUnResolved(knfe, name, type);
            }
        }

        public bool CanResolve<TType>()
            where TType : class
        {
            return CanResolve(string.Empty, typeof (TType));
        }


        public bool CanResolve<TType>(string name)
            where TType : class
        {
            return CanResolve(name, typeof (TType));
        }


        public bool CanResolve(Type type)
        {
            return CanResolve(string.Empty, type);
        }


        public bool CanResolve(string name, Type type)
        {
            return _typeRegistry.ContainsKey(name, type);
        }

        public Func<TType> LazyResolve<TType>() where TType : class
        {
            return LazyResolve<TType>(string.Empty);
        }


        public Func<TType> LazyResolve<TType>(string name) where TType : class
        {
            return () => Resolve<TType>(name);
        }


        public Func<Object> LazyResolve(Type type)
        {
            return LazyResolve(null, type);
        }


        public Func<Object> LazyResolve(string name, Type type)
        {
            return () => Resolve(name, type);
        }

        public TType Resolve<TType>(Enum token) where TType : class
        {
            return Resolve<TType>(token.EnumName());
        }

        public TType Resolve<TType>(params Enum[] tokens) where TType : class
        {
            return Resolve<TType>(EnumExtensions.CombinedToken(tokens));
        }

        public object Resolve(Enum token, Type type)
        {
            return Resolve(token.EnumName(), type);
        }

        public object Resolve(Type type, params Enum[] tokens)
        {
            return Resolve(EnumExtensions.CombinedToken(tokens), type);
        }

        public Func<TType> LazyResolve<TType>(Enum token) where TType : class
        {
            return LazyResolve<TType>(token.EnumName());
        }

        public Func<TType> LazyResolve<TType>(params Enum[] tokens) where TType : class
        {
            return LazyResolve<TType>(EnumExtensions.CombinedToken(tokens));
        }

        public Func<object> LazyResolve(Type type, params Enum[] tokens)
        {
            return LazyResolve(EnumExtensions.CombinedToken(tokens), type);
        }

        public bool CanResolve<TType>(Enum token) where TType : class
        {
            return CanResolve<TType>(token.EnumName());
        }

        public bool CanResolve<TType>(params Enum[] tokens) where TType : class
        {
            return CanResolve<TType>(EnumExtensions.CombinedToken(tokens));
        }

        public bool CanResolve(Enum token, Type type)
        {
            return CanResolve(token.EnumName(), type);
        }

        public bool CanResolve(Type type, params Enum[] tokens)
        {
            return CanResolve(EnumExtensions.CombinedToken(tokens), type);
        }

        #endregion

        #region Resolve All Methods

        public IEnumerable<TType> ResolveAll<TType>() where TType : class
        {
            return ResolveAll(typeof (TType)).Cast<TType>();
        }


        public IEnumerable<object> ResolveAll(Type type)
        {
            var registrations = _typeRegistry.GetDerived(type);
            var instances = new List<object>();
            foreach (var reg in registrations)
            {
                instances.Add(reg.GetInstance());
            }
            return instances;
        }

        #endregion

        private object HandleUnResolved(Exception knfe, string name, Type type)
        {
            if (type.IsGenericType)
            {
                object result = ResolveUsingOpenType(knfe, name, type);
                if (result != null)
                    return result;
            }

            if (type.IsClass)
            {
                try
                {
                    var func = CreateInstanceDelegateFactory.Create(type);
                    Register(name, type, func);

                    return _typeRegistry.Get(name, type).GetInstance();
                }
                catch
                {
                    throw new KeyNotFoundException(ResolveFailureMessage(type), knfe);
                }
            }

            if (type.IsInterface)
            {
                var regs = _typeRegistry.GetDerived(name, type);
                var reg = regs.FirstOrDefault();
                if (reg != null)
                {
                    object instance = reg.GetInstance();
                    Register(name, type, (c) => c.Resolve(name, instance.GetType()));
                    return instance;
                }
                else
                    throw new KeyNotFoundException(ResolveFailureMessage(type), knfe);
            }
            throw new KeyNotFoundException(ResolveFailureMessage(type), knfe);
        }

        private object ResolveUsingOpenType(Exception knfe, string name, Type type)
        {
            if (type.ContainsGenericParameters)
                throw new KeyNotFoundException(ResolveFailureMessage(type), knfe);
            else
            {
                var definition = type.GetGenericTypeDefinition();
                var arguments = type.GetGenericArguments();
                if (_openTypeRegistry.ContainsKey(name, definition))
                {
                    var reg = _openTypeRegistry.Get(name, definition);
                    var implType = reg.ImplType;
                    var newType = implType.MakeGenericType(arguments);
                    try
                    {
                        if (CanResolve(name, newType))
                            return Resolve(name, newType);

                        Register(name, type, newType).WithInstanceCreationStrategy(reg._instanceFactory);
                        return _typeRegistry.Get(name, type).GetInstance();
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            return null;
        }

        private static string ResolveFailureMessage(Type type)
        {
            return String.Format("failed to resolve {0}", type);
        }
    }
}