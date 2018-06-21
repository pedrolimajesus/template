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
    public partial class InstanceAssembler : ISpecifyCreationStrategy, IDisposable
    {
        private readonly ObjectAssemblyRegistry _openTypeRegistry = new ObjectAssemblyRegistry();
        private readonly ObjectAssemblyRegistry _typeRegistry = new ObjectAssemblyRegistry();

        private bool _isDisposed;


        public InstanceAssembler()
        {
            RegisterInstance(this);
            RegisterInstance<IObjectAssemblyRegistry>(this);
            RegisterInstance<IAssembleObject>(this);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IObjectAssemblyRegistry Members

        public IInstanceCreationStrategy DefaultInstanceCreationStrategy { get; set; }

        #endregion

        #region ISpecifyCreationStrategy Members

        public ISpecifyCreationStrategy UsesDefaultInstanceCreationStrategyOf(IInstanceCreationStrategy lifetimeManager)
        {
            DefaultInstanceCreationStrategy = lifetimeManager;
            return this;
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _typeRegistry.Dispose();
                    _openTypeRegistry.Dispose();
                }
            }
            _isDisposed = true;
        }

        ~InstanceAssembler()
        {
            Dispose(false);
        }
    }
}