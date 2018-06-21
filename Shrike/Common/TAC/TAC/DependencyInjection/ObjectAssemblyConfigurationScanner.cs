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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;

namespace AppComponents
{
    public static class ObjectAssemblyConfigurationScanner
    {
        public static void ScanForRegistrations(InstanceAssembler assembler)
        {
            string binPath = HttpContext.Current != null
                                 ? HttpContext.Current.Server.MapPath("/bin")
                                 : Environment.CurrentDirectory;
            ScanForRegistrations(assembler, binPath);
        }


        public static void ScanForRegistrations(InstanceAssembler container, string binPath,
                                                string filePattern = "*.dll")
        {
            var assemblyNames = Directory.GetFiles(binPath, filePattern);

            foreach (var filename in assemblyNames)
                InvokeRegistrationConfigurators(container, filename);
        }

        public static void InvokeRegistrationConfigurators(InstanceAssembler assembler, string filename)
        {
            var assembly = Assembly.LoadFile(filename);

            var registrars = assembly.GetExportedTypes()
                .Where(type => type.GetInterface(typeof (IObjectAssemblySpecifier).ToString()) != null);


            foreach (var registrar in registrars)
                (Activator.CreateInstance(registrar) as IObjectAssemblySpecifier).RegisterIn(assembler);
        }
    }
}