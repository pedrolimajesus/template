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

using System.IO;
using System.Linq;
using System.Reflection;
using AppComponents.Extensions.EnumerableEx;

namespace AppComponents
{
    public static class Registrations
    {
        public static void LoadAndRegisterAssemblyPlugins(string pattern)
        {
            try
            {
                ILocalFileMirror fm = Catalog.Factory.Resolve<ILocalFileMirror>();

                var plugins = from f in Directory.EnumerateFiles(fm.TargetPath, pattern)
                              let a = TryLoadAssembly(f)
                              where a != null
                              select a;

                plugins.ForEach(p => Catalog.LoadFromAssembly(p));
            }
            catch
            {
            }
        }

        private static Assembly TryLoadAssembly(string fileName)
        {
            Assembly retval = null;

            try
            {
                retval = Assembly.LoadFrom(fileName);
            }
            catch
            {
            }

            return retval;
        }
    }
}