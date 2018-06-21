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

namespace AppComponents
{
    partial class StateMachine<TStateType, TTriggerType>
    {
        #region Nested type: ParameterizedTrigger

        public abstract class ParameterizedTrigger
        {
            private readonly Type[] _argumentTypes;
            private readonly TTriggerType _underlyingTrigger;


            public ParameterizedTrigger(TTriggerType underlyingTrigger, params Type[] argumentTypes)
            {
                _underlyingTrigger = underlyingTrigger;
                _argumentTypes = argumentTypes;
            }


            public TTriggerType Trigger
            {
                get { return _underlyingTrigger; }
            }


            public void ValidateParameters(object[] args)
            {
                ParameterPackager.Validate(args, _argumentTypes);
            }
        }


        public class ParameterizedTrigger<TArg0> : ParameterizedTrigger
        {
            public ParameterizedTrigger(TTriggerType underlyingTrigger)
                : base(underlyingTrigger, typeof (TArg0))
            {
            }
        }


        public class ParameterizedTrigger<TArg0, TArg1> : ParameterizedTrigger
        {
            public ParameterizedTrigger(TTriggerType underlyingTrigger)
                : base(underlyingTrigger, typeof (TArg0), typeof (TArg1))
            {
            }
        }


        public class ParameterizedTrigger<TArg0, TArg1, TArg2> : ParameterizedTrigger
        {
            public ParameterizedTrigger(TTriggerType underlyingTrigger)
                : base(underlyingTrigger, typeof (TArg0), typeof (TArg1), typeof (TArg2))
            {
            }
        }

        #endregion
    }
}