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
using System.ComponentModel;
using System.Web.Mvc;
using AppComponents.ControlFlow;
using AppComponents.Extensions.StringEx;

namespace AppComponents.Web
{
    public class ContextDisplayNameAttribute : DisplayNameAttribute
    {
        public ContextDisplayNameAttribute(string resourceKey)
        {
            ResourceKey = resourceKey;
        }

        public override string DisplayName
        {
            get
            {
                string displayName = ContextualString.Get(ResourceKey);

                return string.IsNullOrEmpty(displayName)
                           ? string.Format("Resource Key not found: {0}", ResourceKey)
                           : displayName;
            }
        }

        private string ResourceKey { get; set; }
    }

    public class ContextualDataAnnotationsModelMetadataProvider : DataAnnotationsModelMetadataProvider
    {
        protected override ModelMetadata CreateMetadata(
            IEnumerable<Attribute> attributes,
            Type containerType,
            Func<object> modelAccessor,
            Type modelType,
            string propertyName)
        {
            var meta = base.CreateMetadata
                (attributes, containerType, modelAccessor, modelType, propertyName);

            if (string.IsNullOrEmpty(propertyName))
                return meta;

            if (meta.DisplayName == null)
            {
                var qualifiedKey = string.Format("{0}.{1}", modelType.Name, propertyName);
                meta.DisplayName = ContextualString.Get(qualifiedKey);
            }

            if (string.IsNullOrEmpty(meta.DisplayName))
                meta.DisplayName = string.Format("{0}", propertyName.ToSentenceCase());

            return meta;
        }
    }
}