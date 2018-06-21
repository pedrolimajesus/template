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
        #region Nested type: StateSpecifier

        public class StateSpecifier
        {
            private static readonly Func<bool> Promiscuous = () => true;
            private readonly StateSpecification _specification;
            private readonly Func<TStateType, StateSpecification> _specificationLookup;

            internal StateSpecifier(StateSpecification specification, Func<TStateType, StateSpecification> lookup)
            {
                _specification = specification;
                _specificationLookup = lookup;
            }


            public StateSpecifier Permit(TTriggerType trigger, TStateType destinationState)
            {
                EnforceNotIdentityTransition(destinationState);
                return InternalPermit(trigger, destinationState);
            }


            public StateSpecifier PermitIf(TTriggerType trigger, TStateType destinationState,
                                           Func<bool> triggerCondition)
            {
                EnforceNotIdentityTransition(destinationState);
                return InternalPermitIf(trigger, destinationState, triggerCondition);
            }


            public StateSpecifier PermitReentry(TTriggerType trigger)
            {
                return InternalPermit(trigger, _specification.UnderlyingState);
            }


            public StateSpecifier PermitReentryIf(TTriggerType trigger, Func<bool> triggerCondition)
            {
                return InternalPermitIf(trigger, _specification.UnderlyingState, triggerCondition);
            }

            public StateSpecifier Ignore(TTriggerType trigger)
            {
                return IgnoreIf(trigger, Promiscuous);
            }


            public StateSpecifier IgnoreIf(TTriggerType trigger, Func<bool> triggerCondition)
            {
                _specification.AddTriggerStrategy(new IgnoredTriggerStrategy(trigger, triggerCondition));
                return this;
            }


            public StateSpecifier OnEntry(Action entryAction)
            {
                return OnEntry(t => entryAction());
            }


            public StateSpecifier OnEntry(Action<StateTransition> entryAction)
            {
                _specification.AddEntryAction((t, args) => entryAction(t));
                return this;
            }

            public StateSpecifier OnEntryFrom(TTriggerType trigger, Action entryAction)
            {
                return OnEntryFrom(trigger, t => entryAction());
            }


            public StateSpecifier OnEntryFrom(TTriggerType trigger, Action<StateTransition> entryAction)
            {
                _specification.AddEntryAction(trigger, (t, args) => entryAction(t));
                return this;
            }


            public StateSpecifier OnEntryFrom<TArg0>(ParameterizedTrigger<TArg0> trigger, Action<TArg0> entryAction)
            {
                return OnEntryFrom(trigger, (a0, t) => entryAction(a0));
            }


            public StateSpecifier OnEntryFrom<TArg0>(ParameterizedTrigger<TArg0> trigger,
                                                     Action<TArg0, StateTransition> entryAction)
            {
                _specification.AddEntryAction(trigger.Trigger, (t, args) => entryAction(
                    ParameterPackager.Unpack<TArg0>(args, 0), t));
                return this;
            }


            public StateSpecifier OnEntryFrom<TArg0, TArg1>(ParameterizedTrigger<TArg0, TArg1> trigger,
                                                            Action<TArg0, TArg1> entryAction)
            {
                return OnEntryFrom(trigger, (a0, a1, t) => entryAction(a0, a1));
            }


            public StateSpecifier OnEntryFrom<TArg0, TArg1>(ParameterizedTrigger<TArg0, TArg1> trigger,
                                                            Action<TArg0, TArg1, StateTransition> entryAction)
            {
                _specification.AddEntryAction(trigger.Trigger, (t, args) => entryAction(
                    ParameterPackager.Unpack<TArg0>(args, 0),
                    ParameterPackager.Unpack<TArg1>(args, 1), t));
                return this;
            }


            public StateSpecifier OnEntryFrom<TArg0, TArg1, TArg2>(ParameterizedTrigger<TArg0, TArg1, TArg2> trigger,
                                                                   Action<TArg0, TArg1, TArg2> entryAction)
            {
                return OnEntryFrom(trigger, (a0, a1, a2, t) => entryAction(a0, a1, a2));
            }


            public StateSpecifier OnEntryFrom<TArg0, TArg1, TArg2>(ParameterizedTrigger<TArg0, TArg1, TArg2> trigger,
                                                                   Action<TArg0, TArg1, TArg2, StateTransition>
                                                                       entryAction)
            {
                _specification.AddEntryAction(trigger.Trigger, (t, args) => entryAction(
                    ParameterPackager.Unpack<TArg0>(args, 0),
                    ParameterPackager.Unpack<TArg1>(args, 1),
                    ParameterPackager.Unpack<TArg2>(args, 2), t));
                return this;
            }


            public StateSpecifier OnExit(Action exitAction)
            {
                return OnExit(t => exitAction());
            }


            public StateSpecifier OnExit(Action<StateTransition> exitAction)
            {
                _specification.AddExitAction(exitAction);
                return this;
            }


            public StateSpecifier SubstateOf(TStateType superstate)
            {
                var superRepresentation = _specificationLookup(superstate);
                _specification.Superstate = superRepresentation;
                superRepresentation.AddSubstate(_specification);
                return this;
            }


            public StateSpecifier Permit(TTriggerType trigger, Func<TStateType> destinationStateSelector)
            {
                return PermitIf(trigger, destinationStateSelector, Promiscuous);
            }


            public StateSpecifier Permit<TArg0>(ParameterizedTrigger<TArg0> trigger,
                                                Func<TArg0, TStateType> destinationStateSelector)
            {
                return PermitIf(trigger, destinationStateSelector, Promiscuous);
            }


            public StateSpecifier Permit<TArg0, TArg1>(ParameterizedTrigger<TArg0, TArg1> trigger,
                                                       Func<TArg0, TArg1, TStateType> destinationStateSelector)
            {
                return PermitIf(trigger, destinationStateSelector, Promiscuous);
            }


            public StateSpecifier Permit<TArg0, TArg1, TArg2>(ParameterizedTrigger<TArg0, TArg1, TArg2> trigger,
                                                              Func<TArg0, TArg1, TArg2, TStateType>
                                                                  destinationStateSelector)
            {
                return PermitIf(trigger, destinationStateSelector, Promiscuous);
            }


            public StateSpecifier PermitIf(TTriggerType trigger, Func<TStateType> destinationStateSelector,
                                           Func<bool> triggerCondition)
            {
                return PermitIfInternal(trigger, args => destinationStateSelector(), triggerCondition);
            }


            public StateSpecifier PermitIf<TArg0>(ParameterizedTrigger<TArg0> trigger,
                                                  Func<TArg0, TStateType> destinationStateSelector, Func<bool> guard)
            {
                return PermitIfInternal(
                    trigger.Trigger,
                    args => destinationStateSelector(
                        ParameterPackager.Unpack<TArg0>(args, 0)),
                    guard);
            }


            public StateSpecifier PermitIf<TArg0, TArg1>(ParameterizedTrigger<TArg0, TArg1> trigger,
                                                         Func<TArg0, TArg1, TStateType> destinationStateSelector,
                                                         Func<bool> guard)
            {
                return PermitIfInternal(
                    trigger.Trigger,
                    args => destinationStateSelector(
                        ParameterPackager.Unpack<TArg0>(args, 0),
                        ParameterPackager.Unpack<TArg1>(args, 1)),
                    guard);
            }


            public StateSpecifier PermitIf<TArg0, TArg1, TArg2>(ParameterizedTrigger<TArg0, TArg1, TArg2> trigger,
                                                                Func<TArg0, TArg1, TArg2, TStateType>
                                                                    destinationStateSelector, Func<bool> guard)
            {
                return PermitIfInternal(
                    trigger.Trigger,
                    args => destinationStateSelector(
                        ParameterPackager.Unpack<TArg0>(args, 0),
                        ParameterPackager.Unpack<TArg1>(args, 1),
                        ParameterPackager.Unpack<TArg2>(args, 2)),
                    guard);
            }

            private void EnforceNotIdentityTransition(TStateType destination)
            {
                if (destination.Equals(_specification.UnderlyingState))
                {
                    throw new ArgumentException("Cannot transition to same state.");
                }
            }

            private StateSpecifier InternalPermit(TTriggerType trigger, TStateType destinationState)
            {
                return InternalPermitIf(trigger, destinationState, () => true);
            }

            private StateSpecifier InternalPermitIf(TTriggerType trigger, TStateType destinationState,
                                                    Func<bool> triggerCondition)
            {
                _specification.AddTriggerStrategy(new StateTransitionTriggerStrategy(trigger, destinationState,
                                                                                     triggerCondition));
                return this;
            }

            private StateSpecifier InternalPermitDynamic(TTriggerType trigger,
                                                         Func<object[], TStateType> destinationStateSelector)
            {
                return PermitIfInternal(trigger, destinationStateSelector, Promiscuous);
            }

            private StateSpecifier PermitIfInternal(TTriggerType trigger,
                                                    Func<object[], TStateType> destinationStateSelector,
                                                    Func<bool> triggerCondition)
            {
                _specification.AddTriggerStrategy(new DestinationFunctionTriggerStrategy(trigger,
                                                                                         destinationStateSelector,
                                                                                         triggerCondition));
                return this;
            }
        }

        #endregion
    }
}