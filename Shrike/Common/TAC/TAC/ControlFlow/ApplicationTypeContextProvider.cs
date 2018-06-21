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
using System.Reflection;
using AppComponents.Extensions.EnumerableEx;

namespace AppComponents.ControlFlow
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class ApplicationTypeContextAttribute : Attribute
    {
        public ApplicationTypeContextAttribute(string applicationType, string resourceFile)
        {
            ApplicationType = applicationType;
            ResourceFile = resourceFile;
        }

        public string ApplicationType { get; private set; }
        public string ResourceFile { get; private set; }
    }

    public enum ApplicationTypeConfiguration
    {
        ApplicationType,
        ApplicationResourcesFile
    }

    public class ApplicationTypeContextProvider : IContextProvider
    {
        public const string UntypedApplication = "UntypedApplication";
        public const string DefaultResources = "Application.StringResources";

        private const string Format = "context://ApplicationType/{0}/{1}";

        #region IContextProvider Members

        public IEnumerable<Uri> ProvideContexts()
        {
            var contextAtt =
                Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (ApplicationTypeContextAttribute), false).
                    Select(o => (ApplicationTypeContextAttribute) o).SingleOrDefault();

            if (null == contextAtt)
            {
                var cf = Catalog.Factory.Resolve<IConfig>();
                var uri =
                    new Uri(string.Format(Format,
                                          cf.Get(ApplicationTypeConfiguration.ApplicationType, UntypedApplication),
                                          cf.Get(ApplicationTypeConfiguration.ApplicationResourcesFile, DefaultResources)));
                return EnumerableEx.OfOne(uri);
            }

            return
                EnumerableEx.OfOne(new Uri(string.Format(Format, contextAtt.ApplicationType, contextAtt.ResourceFile)));
        }

        #endregion
    }
}