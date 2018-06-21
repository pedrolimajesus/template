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

using AppComponents.InstanceFactories;

namespace AppComponents
{
    public static class ObjectCreationExtensions
    {
        private static readonly AssemblerInstancePoolFactory _assemblerInstanceFactory =
            new AssemblerInstancePoolFactory();


        private static readonly ThreadLocalStorageInstanceFactory _threadLocalInstanceFactory =
            new ThreadLocalStorageInstanceFactory();

        private static readonly InstanceCacheFactory _instanceCacheFactory = new InstanceCacheFactory();

        public static IObjectAssemblySpecification AsAlwaysNew(this IObjectAssemblySpecification reg)
        {
            return reg.WithInstanceCreationStrategy(null);
        }


        public static IObjectAssemblySpecification AsAssemblerSingleton(this IObjectAssemblySpecification reg)
        {
            return reg.WithInstanceCreationStrategy(_assemblerInstanceFactory);
        }


        public static IObjectAssemblySpecification AsThreadSingleton(this IObjectAssemblySpecification reg)
        {
            return reg.WithInstanceCreationStrategy(_threadLocalInstanceFactory);
        }

        public static IObjectAssemblySpecification AsInstanceCacheInstance(this IObjectAssemblySpecification reg)
        {
            return reg.WithInstanceCreationStrategy(_instanceCacheFactory);
        }
    }
}