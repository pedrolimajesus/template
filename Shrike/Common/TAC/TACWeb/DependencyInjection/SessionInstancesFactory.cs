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

using System.Web;

namespace AppComponents.InstanceFactories
{
    public class SessionInstancesFactory : IInstanceCreationStrategy
    {
        private readonly object _syncLock = new object();
        private HttpSessionStateBase _testSession;


        private HttpSessionStateBase Session
        {
            get
            {
                HttpSessionStateBase session = (HttpContext.Current != null)
                                                   ? new HttpSessionStateWrapper(HttpContext.Current.Session)
                                                   : _testSession;
                return session;
            }
        }

        #region  Members

        public object ActivateInstance(IObjectAssemblySpecification registration)
        {
            object instance = Session[registration.Key];
            if (instance == null)
            {
                lock (_syncLock)
                {
                    instance = Session[registration.Key];
                    if (instance == null)
                    {
                        instance = registration.CreateInstance();
                        Session[registration.Key] = instance;
                    }
                }
            }

            return instance;
        }


        public void FlushCache(IObjectAssemblySpecification registration)
        {
            Session.Remove(registration.Key);
        }

        #endregion

        public void SetContext(HttpContextBase context)
        {
            _testSession = context.Session;
        }
    }
}