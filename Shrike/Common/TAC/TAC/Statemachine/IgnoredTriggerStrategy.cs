﻿// // 
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
        #region Nested type: IgnoredTriggerStrategy

        internal class IgnoredTriggerStrategy : TriggerStrategy
        {
            public IgnoredTriggerStrategy(TTriggerType trigger, Func<bool> triggerCondition)
                : base(trigger, triggerCondition)
            {
            }

            public override bool ResultsInTransitionFrom(TStateType source, object[] args, out TStateType destination)
            {
                destination = default(TStateType);
                return false;
            }
        }

        #endregion
    }
}