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

namespace AppComponents
{
    public partial class StateMachine<TStateType, TTriggerType>
    {
        #region Nested type: StateTransition

        public class StateTransition
        {
            private readonly TStateType _destination;
            private readonly TStateType _source;
            private readonly TTriggerType _trigger;


            public StateTransition(TStateType source, TStateType destination, TTriggerType trigger)
            {
                _source = source;
                _destination = destination;
                _trigger = trigger;
            }


            public TStateType Source
            {
                get { return _source; }
            }


            public TStateType Destination
            {
                get { return _destination; }
            }


            public TTriggerType Trigger
            {
                get { return _trigger; }
            }


            public bool IsReentry
            {
                get { return Source.Equals(Destination); }
            }
        }

        #endregion
    }
}