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

namespace AppComponents.Dynamic
{
    public abstract class Aspect
    {
        #region CommonCategories enum

        public enum CommonCategories
        {
            Caching = 500,
            Coordination = 950,
            DataBinding = 900,
            Marshalling = 200,
            Persistence = 1000,
            Security = 300,
            Threading = 600,
            Trampoline = 700,
            Diagnostics = 400,
            UnitOfWork = 800,
            Validation = 100,

            RunUrgent = 0,
            RunEarly = 110,
            RunSoon = 310,
            RunLate = 610,
            RunVeryLate = 910,

            Unknown = 5000
        };

        #endregion

        #region InterceptMode enum

        [Flags]
        public enum InterceptMode
        {
            Never = 0,
            Before = 1,
            After = 2,
            Instead = 4
        };

        #endregion

        private bool _isInitialized;

        protected Aspect(Enum category, InterceptMode mode = InterceptMode.Before)
        {
            InstanceId = Guid.NewGuid();
            Mode = mode;
            Category = category;
        }

        protected Aspect(InterceptMode mode = InterceptMode.Before)
        {
            Mode = mode;
            Category = CommonCategories.Unknown;
        }

        public Enum Category { get; set; }
        public InterceptMode Mode { get; set; }
        public Guid InstanceId { get; private set; }

        public void MaybeInitialize(ShapeableExpando extensions, object target)
        {
            if (!_isInitialized)
            {
                Initialize(extensions, target);
                _isInitialized = true;
            }
        }


        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var that = (Aspect) obj;
            return InstanceId == that.InstanceId;
        }


        public override int GetHashCode()
        {
            return InstanceId.GetHashCode();
        }

        public virtual void Initialize(ShapeableExpando extensions, object target)
        {
        }

        public virtual bool InterceptBefore(Invocation invocation, object target, ShapeableExpando extensions,
                                            out object resultData)
        {
            resultData = null;
            return true;
        }

        public virtual bool InterceptInstead(Invocation invocation, object target, ShapeableExpando extensions,
                                             out object resultData)
        {
            resultData = null;
            return true;
        }

        public virtual bool InterceptAfter(Invocation invocation, object target, ShapeableExpando extensions,
                                           out object resultData)
        {
            resultData = null;
            return true;
        }
    }
}