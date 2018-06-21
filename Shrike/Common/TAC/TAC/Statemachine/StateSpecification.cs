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
using System.Linq;

namespace AppComponents
{
    public partial class StateMachine<TStateType, TTriggerType>
    {
        #region Nested type: StateSpecification

        internal class StateSpecification
        {
            private readonly ICollection<Action<StateTransition, object[]>> _entryActions =
                new List<Action<StateTransition, object[]>>();

            private readonly ICollection<Action<StateTransition>> _exitActions = new List<Action<StateTransition>>();
            private readonly TStateType _state;

            private readonly ICollection<StateSpecification> _substates = new List<StateSpecification>();

            private readonly IDictionary<TTriggerType, ICollection<TriggerStrategy>> _triggerStrategies =
                new Dictionary<TTriggerType, ICollection<TriggerStrategy>>();

            private StateSpecification _superstate;

            public StateSpecification(TStateType state)
            {
                _state = state;
            }

            public StateSpecification Superstate
            {
                get { return _superstate; }
                set { _superstate = value; }
            }

            public TStateType UnderlyingState
            {
                get { return _state; }
            }

            public IEnumerable<TTriggerType> PermittedTriggers
            {
                get
                {
                    var result = _triggerStrategies
                        .Where(t => t.Value.Any(a => a.ConditionPositive))
                        .Select(t => t.Key);

                    if (Superstate != null)
                        result = result.Union(Superstate.PermittedTriggers);

                    return result.ToArray();
                }
            }

            public bool CanHandle(TTriggerType trigger)
            {
                TriggerStrategy unused;
                return HasHandlerFor(trigger, out unused);
            }

            public bool HasHandlerFor(TTriggerType trigger, out TriggerStrategy handler)
            {
                return (HasLocalHandlerFor(trigger, out handler) ||
                        (Superstate != null && Superstate.HasHandlerFor(trigger, out handler)));
            }

            private bool HasLocalHandlerFor(TTriggerType trigger, out TriggerStrategy handler)
            {
                ICollection<TriggerStrategy> possible;
                if (!_triggerStrategies.TryGetValue(trigger, out possible))
                {
                    handler = null;
                    return false;
                }

                var actual = possible.Where(at => at.ConditionPositive).ToArray();

                if (actual.Count() > 1)
                    throw new InvalidOperationException(
                        string.Format("Ambiguous trigger transition on trigger {0} on state {1}",
                                      trigger, _state));

                handler = actual.FirstOrDefault();
                return handler != null;
            }

            public void AddEntryAction(TTriggerType trigger, Action<StateTransition, object[]> action)
            {
                _entryActions.Add((t, args) =>
                                      {
                                          if (t.Trigger.Equals(trigger))
                                              action(t, args);
                                      });
            }

            public void AddEntryAction(Action<StateTransition, object[]> action)
            {
                _entryActions.Add(action);
            }

            public void AddExitAction(Action<StateTransition> action)
            {
                _exitActions.Add(action);
            }

            public void Enter(StateTransition transition, params object[] entryArgs)
            {
                if (transition.IsReentry)
                {
                    ExecuteEntryActions(transition, entryArgs);
                }
                else if (!Includes(transition.Source))
                {
                    if (_superstate != null)
                        _superstate.Enter(transition, entryArgs);

                    ExecuteEntryActions(transition, entryArgs);
                }
            }

            public void Exit(StateTransition transition)
            {
                if (transition.IsReentry)
                {
                    ExecuteExitActions(transition);
                }
                else if (!Includes(transition.Destination))
                {
                    ExecuteExitActions(transition);
                    if (_superstate != null)
                        _superstate.Exit(transition);
                }
            }

            private void ExecuteEntryActions(StateTransition transition, object[] entryArgs)
            {
                foreach (var action in _entryActions)
                    action(transition, entryArgs);
            }

            private void ExecuteExitActions(StateTransition transition)
            {
                foreach (var action in _exitActions)
                    action(transition);
            }

            public void AddTriggerStrategy(TriggerStrategy triggerBehaviour)
            {
                ICollection<TriggerStrategy> allowed;
                if (!_triggerStrategies.TryGetValue(triggerBehaviour.Trigger, out allowed))
                {
                    allowed = new List<TriggerStrategy>();
                    _triggerStrategies.Add(triggerBehaviour.Trigger, allowed);
                }
                allowed.Add(triggerBehaviour);
            }

            public void AddSubstate(StateSpecification substate)
            {
                _substates.Add(substate);
            }

            public bool Includes(TStateType state)
            {
                return _state.Equals(state) || _substates.Any(s => s.Includes(state));
            }

            public bool IsIncludedIn(TStateType state)
            {
                return
                    _state.Equals(state) ||
                    (_superstate != null && _superstate.IsIncludedIn(state));
            }
        }

        #endregion
    }
}