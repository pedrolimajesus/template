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
    internal class ObjectAssemblySpecification : IObjectAssemblySpecification
    {
        private readonly Type _implementingType;

        private readonly IAssembleObject _objectAssembler;
        private readonly string _registrationKey;
        private readonly object _syncRoot = new object();
        private readonly Type _toAssembleType;
        internal Func<IAssembleObject, object> _factoryCreate;
        public object _instance;
        internal IInstanceCreationStrategy _instanceFactory;


        public ObjectAssemblySpecification(
            IAssembleObject container,
            string name,
            Type regType,
            Func<IAssembleObject, object> factory)
        {
            _instanceFactory = null;
            _objectAssembler = container;
            _factoryCreate = factory;
            Name = name;
            _toAssembleType = regType;
            _implementingType = null;
            _registrationKey = String.Format("[{0}]:{1}", (name ?? "null"), regType.FullName);
        }

        public ObjectAssemblySpecification(
            IAssembleObject container,
            string name,
            Type regType,
            Type implType)
        {
            _instanceFactory = null;
            _objectAssembler = container;
            _factoryCreate = null;
            Name = name;
            _toAssembleType = regType;
            _implementingType = implType;
            _registrationKey = String.Format("[{0}]:{1}", (name ?? "null"), implType.FullName);
        }

        public Type ImplType
        {
            get { return _implementingType; }
        }

        #region IObjectAssemblySpecification Members

        public string Key
        {
            get { return _registrationKey; }
        }

        public Type ResolvesTo
        {
            get { return _toAssembleType; }
        }

        public string Name { get; private set; }

        public IObjectAssemblySpecification WithInstanceCreationStrategy(IInstanceCreationStrategy manager)
        {
            _instanceFactory = manager;
            return this;
        }

        public object CreateInstance()
        {
            return _factoryCreate(_objectAssembler);
        }

        public void FlushCache()
        {
            _instance = null;
            if (_instanceFactory != null)
                _instanceFactory.FlushCache(this);
        }

        #endregion

        public object ProvisionInstanceFromCache()
        {
            if (_instance == null)
                lock (_syncRoot)
                {
                    if (_instance == null)
                        _instance = _factoryCreate(_objectAssembler);
                }
            return _instance;
        }

        public object GetInstance()
        {
            return _instance ??
                   ((_instanceFactory != null)
                        ? _instanceFactory.ActivateInstance(this)
                        : _factoryCreate(_objectAssembler));
        }
    }
}