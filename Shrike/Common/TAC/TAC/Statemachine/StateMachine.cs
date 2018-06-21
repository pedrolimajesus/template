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
        private readonly Func<TStateType> _stateReader;

        private readonly IDictionary<TStateType, StateSpecification> _stateSpecification =
            new Dictionary<TStateType, StateSpecification>();

        private readonly Action<TStateType> _stateWriter;

        private readonly IDictionary<TTriggerType, ParameterizedTrigger> _triggerSpecification =
            new Dictionary<TTriggerType, ParameterizedTrigger>();

        private Action<TStateType, TTriggerType> _unhandledTriggerAction = DefaultUnhandledTriggerAction;


        public StateMachine(Func<TStateType> reader, Action<TStateType> writer)
        {
            _stateReader = reader;
            _stateWriter = writer;
        }


        public StateMachine(TStateType initialState)
        {
            var reference = new StateRef {State = initialState};
            _stateReader = () => reference.State;
            _stateWriter = s => reference.State = s;
        }


        public TStateType State
        {
            get { return _stateReader(); }
            private set { _stateWriter(value); }
        }


        public IEnumerable<TTriggerType> PermittedTriggers
        {
            get { return Specification.PermittedTriggers; }
        }

        private StateSpecification Specification
        {
            get { return GetStateSpecification(State); }
        }

        private StateSpecification GetStateSpecification(TStateType state)
        {
            StateSpecification result;

            if (!_stateSpecification.TryGetValue(state, out result))
            {
                result = new StateSpecification(state);
                _stateSpecification.Add(state, result);
            }

            return result;
        }


        public StateSpecifier Specify(TStateType state)
        {
            return new StateSpecifier(GetStateSpecification(state), GetStateSpecification);
        }


        public void Fire(TTriggerType trigger)
        {
            FireInternal(trigger, new object[0]);
        }


        public void Fire<TArg0>(ParameterizedTrigger<TArg0> trigger, TArg0 arg0)
        {
            FireInternal(trigger.Trigger, arg0);
        }


        public void Fire<TArg0, TArg1>(ParameterizedTrigger<TArg0, TArg1> trigger, TArg0 arg0, TArg1 arg1)
        {
            FireInternal(trigger.Trigger, arg0, arg1);
        }


        public void Fire<TArg0, TArg1, TArg2>(ParameterizedTrigger<TArg0, TArg1, TArg2> trigger, TArg0 arg0, TArg1 arg1,
                                              TArg2 arg2)
        {
            FireInternal(trigger.Trigger, arg0, arg1, arg2);
        }

        private void FireInternal(TTriggerType trigger, params object[] args)
        {
            ParameterizedTrigger configuration;
            if (_triggerSpecification.TryGetValue(trigger, out configuration))
                configuration.ValidateParameters(args);

            TriggerStrategy triggerStrategy;
            if (!Specification.HasHandlerFor(trigger, out triggerStrategy))
            {
                _unhandledTriggerAction(Specification.UnderlyingState, trigger);
                return;
            }

            var source = State;
            TStateType destination;
            if (triggerStrategy.ResultsInTransitionFrom(source, args, out destination))
            {
                var transition = new StateTransition(source, destination, trigger);

                Specification.Exit(transition);
                State = transition.Destination;
                Specification.Enter(transition, args);
            }
        }


        public void OnUnhandledTrigger(Action<TStateType, TTriggerType> unhandledTriggerAction)
        {
            if (unhandledTriggerAction == null) throw new InvalidOperationException("unhandled trigger action");
            _unhandledTriggerAction = unhandledTriggerAction;
        }


        public bool IsInState(TStateType state)
        {
            return Specification.IsIncludedIn(state);
        }


        public bool CanFire(TTriggerType trigger)
        {
            return Specification.CanHandle(trigger);
        }


        public override string ToString()
        {
            return string.Format(
                "StateMachine {{ State = {0}, PermittedTriggers = {{ {1} }}}}",
                State,
                string.Join(", ", PermittedTriggers.Select(t => t.ToString()).ToArray()));
        }


        public ParameterizedTrigger<TArg0> SetTriggerParameters<TArg0>(TTriggerType trigger)
        {
            var specification = new ParameterizedTrigger<TArg0>(trigger);
            SaveTriggerSpecification(specification);
            return specification;
        }


        public ParameterizedTrigger<TArg0, TArg1> SetTriggerParameters<TArg0, TArg1>(TTriggerType trigger)
        {
            var specification = new ParameterizedTrigger<TArg0, TArg1>(trigger);
            SaveTriggerSpecification(specification);
            return specification;
        }


        public ParameterizedTrigger<TArg0, TArg1, TArg2> SetTriggerParameters<TArg0, TArg1, TArg2>(TTriggerType trigger)
        {
            var specification = new ParameterizedTrigger<TArg0, TArg1, TArg2>(trigger);
            SaveTriggerSpecification(specification);
            return specification;
        }

        private void SaveTriggerSpecification(ParameterizedTrigger trigger)
        {
            if (_triggerSpecification.ContainsKey(trigger.Trigger))
                throw new InvalidOperationException(
                    string.Format("trigger {0} already has been specified", trigger));

            _triggerSpecification.Add(trigger.Trigger, trigger);
        }

        private static void DefaultUnhandledTriggerAction(TStateType state, TTriggerType trigger)
        {
            throw new InvalidOperationException(
                string.Format(
                    "trigger {0} on {1} unhandled.",
                    trigger, state));
        }
    }
}