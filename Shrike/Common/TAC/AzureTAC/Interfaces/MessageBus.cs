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
using System.Diagnostics.Contracts;
using System.Threading;

namespace AppComponents
{
#if true
    // obsolete Message bus shape

    public enum MessageBusLocalConfig
    {
        OptionalNamedMessageBus
    }


    [ContractClass(typeof (IPublisherContract))]
    public interface IPublisher
    {
        void Publish<T>(T msgObject, string route = "");

        void Publish(string msg, string type, string route = "");
    }

    [ContractClass(typeof (IListenerContract))]
    public interface IListener
    {
        void Subscribe<T>(object listener, Action<CancellationToken, T> subscription, string routeFilter = null);
        void UnSubscribe(object listener);
    }


    public interface IMessageBusWorker
    {
        bool ProcessMessage(CancellationToken ct);
    }


    public interface IDistributionMatrix
    {
        IEnumerable<IPublisher> GetDistributionBusNames(string distributionScope);
    }

    public enum BusBroadcasterLocalConfig
    {
        DistributionMatrix,
    }

    [RequiresConfiguration]
    public interface IBusBroadcaster
    {
        void Broadcast<T>(string distributionScope, T msgObject, string route = "");
        void Broadcast(string distributionScope, string msg, string type, string route = "");
    }

    [ContractClassFor(typeof (IPublisher))]
    internal abstract class IPublisherContract : IPublisher
    {
        #region IPublisher Members

        public void Publish<T>(T msgObject, string route = "")
        {
            Contract.Requires(null != msgObject);
        }

        public void Publish(string msg, string type, string route = "")
        {
            Contract.Requires(!string.IsNullOrEmpty(msg));
            Contract.Requires(!string.IsNullOrEmpty(type));
        }

        #endregion
    }


    [ContractClassFor(typeof (IListener))]
    internal abstract class IListenerContract : IListener
    {
        #region IListener Members

        public void Subscribe<T>(object listener, Action<CancellationToken, T> subscription, string routeFilter = null)
        {
            Contract.Requires(null != subscription);
            Contract.Requires(null != listener);
        }

        public void UnSubscribe(object listener)
        {
            Contract.Requires(null != listener);
        }

        #endregion
    }

#endif
}