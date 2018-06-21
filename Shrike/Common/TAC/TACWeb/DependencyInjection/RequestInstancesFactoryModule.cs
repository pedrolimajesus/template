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
using System.Web;

namespace AppComponents.InstanceFactories
{
    public class RequestInstancesFactoryModule : IHttpModule
    {
        #region IHttpModule Members

        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.EndRequest += context_EndRequest;
        }

        #endregion

        private void context_EndRequest(object sender, EventArgs e)
        {
            HttpRequestInstancesFactory.Disposer(sender, e);
        }
    }
}