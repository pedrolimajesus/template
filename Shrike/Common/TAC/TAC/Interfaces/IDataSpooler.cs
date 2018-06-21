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
using System.Linq;

namespace AppComponents.Data
{
    public class SpoolTracking
    {
        public IPageBookmark PageBookmark { get; set; }
        public int LastPageNumber { get; set; }
        public bool Completed { get; set; }
    }

    public enum DataSpoolerLocalConfig
    {
        SpoolId,
        PageSize,
        OptionalLifeTimeMinutes
    }

    [RequiresConfiguration]
    [ContractClass(typeof (IDataSpoolerContract<>))]
    public interface IDataSpooler<X>
        where X : class
    {
        SpoolTracking BeginSpooling();
        void EndSpooling(IPageBookmark bookmark);
        void SpoolBatch(IEnumerable<X> data);
    }

    [ContractClassFor(typeof (IDataSpooler<>))]
    internal abstract class IDataSpoolerContract<X> : IDataSpooler<X>
        where X : class
    {
        #region IDataSpooler<X> Members

        public SpoolTracking BeginSpooling()
        {
            return default(SpoolTracking);
        }

        public void EndSpooling(IPageBookmark bookmark)
        {
            Contract.Requires(null != bookmark);
        }

        public void SpoolBatch(IEnumerable<X> data)
        {
            Contract.Requires(null != data);
            Contract.Requires(data.Any());
        }

        #endregion

        public void Initialize(string spoolId, int pageSize, TimeSpan? lifeTime = null)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(spoolId));
            Contract.Requires(pageSize >= 1);
        }
    }
}