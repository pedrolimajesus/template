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

using AppComponents.Dynamic;
using log4net;

namespace AppComponents.Aspects
{
    public class TraceAspect : Aspect
    {
        #region Variavles

        private ILog _log;

        #endregion

        public override bool InterceptBefore(Invocation invocation, object target, ShapeableExpando extensions, out object resultData)
        {
            _log = ClassLogger.Create(target.GetType());
            var msg = string.Format("Intercept the method {0}, with name{1}", invocation.Kind, invocation.Name);
            if (invocation.Arguments.Length > 0)
            {
                msg += string.Format(" that has the following parameters:{0}", invocation.Arguments);
            }
            _log.Info(msg);
            resultData = null;
            return true;
        }

        public override bool InterceptInstead(Invocation invocation, object target, ShapeableExpando extensions, out object resultData)
        {
            _log = ClassLogger.Create(target.GetType());
            var msg = string.Format("Intercept the method {0}, with name {1}.", invocation.Kind, invocation.Name);
            _log.Info(msg);
            resultData = null;
            return true;
        }

        public override bool InterceptAfter(Invocation invocation, object target, ShapeableExpando extensions, out object resultData)
        {
            _log = ClassLogger.Create(target.GetType());
            var msg = string.Format("Intercept the method {0}, with name {1}.", invocation.Kind, invocation.Name);
            _log.Info(msg);

            resultData = null;
            return true;
        }
    }
}