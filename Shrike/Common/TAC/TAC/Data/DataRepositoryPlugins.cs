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
using System.Reflection;
using AppComponents.Dynamic.Projection;

namespace AppComponents.Data
{
    public class SelectSummarizer<TDataType, TSummaryType> : ISummarizer<TDataType, TSummaryType>
        where TDataType : class, new()
        where TSummaryType : class
    {
        private Func<TSummaryType, string> _identifier;
        private Func<TDataType, TSummaryType> _selection;

        public SelectSummarizer(Func<TDataType, TSummaryType> selection, Func<TSummaryType, string> identifier = null)
        {
            _selection = selection;
            _identifier = identifier;
            if (null == _identifier)
            {
                _identifier = DataDocument.GetDocumentId;
            }
        }

        #region ISummarizer<TDataType,TSummaryType> Members

        public TSummaryType Summarize(TDataType item)
        {
            return _selection(item);
        }

        public string Identify(TSummaryType summary)
        {
            return _identifier(summary);
        }

        #endregion
    }

    public class CastSummarizer<TDataType, TSummaryType> : ISummarizer<TDataType, TSummaryType>
        where TDataType : class, new()
        where TSummaryType : class
    {
        private Func<TSummaryType, string> _identifier;


        public CastSummarizer() : this(null)
        {
        }

        public CastSummarizer(Func<TSummaryType, string> identifier = null)
        {
            _identifier = identifier;
            if (null == _identifier)
            {
                _identifier = DataDocument.GetDocumentId;
            }
        }

        #region ISummarizer<TDataType,TSummaryType> Members

        public TSummaryType Summarize(TDataType item)
        {
            return item as TSummaryType;
        }

        public string Identify(TSummaryType summary)
        {
            return _identifier(summary);
        }

        #endregion
    }

    public class IdentitySummarizer<TDataType, TSummaryType> : ISummarizer<TDataType, TSummaryType>
        where TDataType : class, new()
        where TSummaryType: class

    {
        private readonly Func<TDataType, string> _identifier;

        public IdentitySummarizer(Func<TDataType, string> identifier = null)
        {
            _identifier = identifier;
            if (null == _identifier)
            {
                _identifier = DataDocument.GetDocumentId;
            }
        }

        #region ISummarizer<TDataType,TDataType> Members

        public TSummaryType Summarize(TDataType item)
        {
            return (TSummaryType) (object) item;
        }

        public string Identify(TSummaryType summary)
        {
            return _identifier((TDataType) (object)summary);
        }

        #endregion
    }

    public class Updater<T> where T : class
    {
        private readonly IEnumerable<PropertyInfo> _propertiesSelected;

        public Updater(Func<FromClass<T>, IEnumerable<MemberProjection>> propertySelector)
        {
            _propertiesSelected =
                propertySelector(new FromClass<T>()).Where(mp => mp.MemberType == MemberTypes.Property && mp.IsPublic).Select(
                    mp => mp.MemberInfo as PropertyInfo);
        }

        public void Update(T original, T update)
        {
            foreach (var pi in _propertiesSelected)
                pi.SetValue(original, pi.GetValue(update, null), null);
        }

        public static Action<T, T> AssignProperties(Func<FromClass<T>, IEnumerable<MemberProjection>> propertySelector)
        {
            return new Updater<T>(propertySelector).Update;
        }
    }

    public class NoContextFilter : IContextFilter
    {
        #region IContextFilter Members

        public IQueryable<T> ApplyContextFilter<T>(IQueryable<T> query)
        {
            return query;
        }


        public bool InContext<T>(T _)
        {
            return true;
        }

        #endregion
    }

    public class NoMetadata
    {
        
    }

    public class NoMetadataProvider<TDataType>: IMetadataProvider<TDataType, NoMetadata> where TDataType: class
    {

        public NoMetadata Metadata
        {
            get { return new NoMetadata(); }
        }
    }
}