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
using System.Web;

namespace AppComponents.InstanceFactories
{
    public class HttpRequestInstancesFactory : IInstanceCreationStrategy
    {
        private const string RequestItemsKey = "TACRequestLtItemsKey";
        private static HttpContextBase _testContext;
        private readonly object _lock = new object();


        private static HttpContextBase Context
        {
            get
            {
                HttpContextBase context = (HttpContext.Current != null)
                                              ? new HttpContextWrapper(HttpContext.Current)
                                              : _testContext;
                return context;
            }
        }

        private static Dictionary<string, object> RequestLifetimeInstances
        {
            get
            {
                Dictionary<string, object> requestLifetimeInstances =
                    Context.Items[RequestItemsKey] as Dictionary<string, object>;
                if (requestLifetimeInstances == null)
                {
                    requestLifetimeInstances = new Dictionary<string, object>();
                    Context.Items[RequestItemsKey] = requestLifetimeInstances;
                }
                return requestLifetimeInstances;
            }
        }

        #region  Members

        public object ActivateInstance(IObjectAssemblySpecification registration)
        {
            object instance;
            if (!RequestLifetimeInstances.TryGetValue(registration.Key, out instance))
            {
                lock (_lock)
                {
                    if (!RequestLifetimeInstances.TryGetValue(registration.Key, out instance))
                    {
                        instance = registration.CreateInstance();
                        RequestLifetimeInstances[registration.Key] = instance;
                    }
                }
            }

            return instance;
        }


        public void FlushCache(IObjectAssemblySpecification registration)
        {
            RequestLifetimeInstances.Remove(registration.Key);
        }

        #endregion

        public static void Disposer(object sender, EventArgs e)
        {
            var application = (HttpApplication) sender;
            var _context = application.Context;
            if (_context != null)
            {
                Dictionary<string, object> requestLifetimeInstances =
                    (Dictionary<string, object>) _context.Items[RequestItemsKey];
                if (requestLifetimeInstances != null)
                {
                    foreach (var item in requestLifetimeInstances.Values)
                    {
                        if (item is IDisposable)
                            (item as IDisposable).Dispose();
                    }
                }
            }
        }

        public static void SetContext(HttpContextBase context)
        {
            _testContext = context;
        }
    }
}