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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AppComponents.Data;
using AppComponents.Extensions.EnumerableEx;
using Raven.Client.Linq;
using Raven.Client;

namespace AppComponents.Raven
{
    public class RavenPageBookmark : IPageBookmark
    {
        public RavenQueryStatistics Statistics;

        public RavenPageBookmark(int pageSize)
        {
            
            PageSize = pageSize;
            CurrentPage = 0;
            LastSkippedResults = 0;
            Statistics = null;
        }

        public RavenPageBookmark(IPageBookmark other)
        {
           
            PageSize = other.PageSize;
            CurrentPage = other.CurrentPage;
            LastSkippedResults = other.LastSkippedResults;
            Statistics = null;
        }

        #region IPageBookmark Members

        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int LastSkippedResults { get; set; }
        public int TotalResults { get; set; }

        public int TotalPages
        {
            get 
            {
                if (TotalResults <= 0)
                    return 0;
                else
                {
                    if (PageSize == 0)
                        return 1;

                    return TotalResults/PageSize;
                }
            }
        }

        public void Forward()
        {
            CurrentPage++;
            TotalResults = Statistics.TotalResults;
            LastSkippedResults += Statistics.SkippedResults;
        }

        public bool _manualMore = true;
        public bool More
        {
            get { return _manualMore && CurrentPage < TotalPages; }
            set { _manualMore = value; }
        }

        public bool Done
        {
            get { return !More; }
        }

        #endregion
    }

    public static class RavenExtensions
    {
        public const int BatchStoreSize = 50;

        public static void BatchStore<T>(this IDocumentSession ds, IEnumerable<T> items)
        {
            var batches = items.InBatchesOf(BatchStoreSize);
            foreach(var batch in batches)
                foreach(var it in batch)
                    ds.Store(it);
        }

        public static IRavenQueryable<T> Page<T>(this IRavenQueryable<T> that, RavenPageBookmark bm)
        {
            if (bm.CurrentPage > 0)
            {
                return (IRavenQueryable<T>)
                       that
                           .Statistics(out bm.Statistics)
                           .Skip((bm.CurrentPage*bm.PageSize) + bm.LastSkippedResults)
                           .Take(bm.PageSize);
            }
            else
            {
                return (IRavenQueryable<T>)
                       that
                           .Statistics(out bm.Statistics)
                           .Take(bm.PageSize);
            }
        }

        public static IEnumerable<T> GetAllUnSafe<T>(this IRavenQueryable<T> that, int pgSize = 1024)
        {
            RavenPageBookmark pbm = new RavenPageBookmark(pgSize);
            IEnumerable<T> currentPageResults = Enumerable.Empty<T>();
            IEnumerable<T> aggregatedResults = null;

            do
            {
                currentPageResults = that.Statistics(out pbm.Statistics).Page(pbm).ToArray();

                aggregatedResults = (null == aggregatedResults)
                                        ? currentPageResults
                                        : aggregatedResults.Concat(currentPageResults);


                pbm.Forward();
            } while (pbm.More && currentPageResults.Any());

            return aggregatedResults;
        }
    }
}