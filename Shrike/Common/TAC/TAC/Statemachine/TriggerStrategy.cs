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
    public partial class StateMachine<TStateType, TTriggerType>
    {
        #region Nested type: TriggerStrategy

        internal abstract class TriggerStrategy
        {
            private readonly TTriggerType _trigger;
            private readonly Func<bool> _triggerCondition;

            protected TriggerStrategy(TTriggerType trigger, Func<bool> triggerCondition)
            {
                _trigger = trigger;
                _triggerCondition = triggerCondition;
            }

            public TTriggerType Trigger
            {
                get { return _trigger; }
            }

            public bool ConditionPositive
            {
                get { return _triggerCondition(); }
            }

            public abstract bool ResultsInTransitionFrom(TStateType source, object[] args, out TStateType destination);
        }

        #endregion
    }
}