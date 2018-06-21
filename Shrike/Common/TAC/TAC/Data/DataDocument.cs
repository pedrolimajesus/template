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
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace AppComponents
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DocumentIdentifierAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DocumentETagAttribute : Attribute
    {
    }

    public static class DataDocument
    {
        private static ConcurrentDictionary<Type, PropertyInfo> _typeIdCache =
            new ConcurrentDictionary<Type, PropertyInfo>();

        private static ConcurrentDictionary<Type, PropertyInfo> _typeEtagCache = 
            new ConcurrentDictionary<Type, PropertyInfo>();

        public static bool DocumentIdentityFinder(PropertyInfo pi)
        {
            return
                pi.GetCustomAttributes(typeof (DocumentIdentifierAttribute), true).Select(
                    att => att as DocumentIdentifierAttribute).Any() ||
                pi.Name == "Id" || pi.Name == "Identifier";
        }

        public static string GetDocumentId<T>(T document)
        {
            var retval = String.Empty;
            PropertyInfo prop = IdPropertyForDocument<T>();
            retval = prop.GetValue(document, null).ToString();
            return retval;
        }

        public static PropertyInfo IdPropertyForDocument<T>()
        {
            PropertyInfo prop;
            if (!_typeIdCache.TryGetValue(typeof (T), out prop))
            {
                prop = typeof (T).GetProperties().First(DocumentIdentityFinder);
                _typeIdCache.TryAdd(typeof (T), prop);
            }
            return prop;
        }

        public static void NixId<T>(T document)
        {
            var prop = IdPropertyForDocument<T>();

            if (prop.PropertyType.IsValueType)
            {
                var val = Activator.CreateInstance(prop.PropertyType);
                prop.SetValue(document, val, null);
            }
            else
                prop.SetValue(document, null, null);
        }

        public static bool HasEtag<T>()
        {
            return typeof (T).GetProperties().Any(DocumentEtagFinder);
        }

        public static bool DocumentEtagFinder(PropertyInfo pi)
        {
            return
                pi.GetCustomAttributes(typeof (DocumentETagAttribute), true).Select(att => att as DocumentETagAttribute)
                    .Any() || pi.Name.ToLowerInvariant() == "etag";
        }

        public static Guid GetDocumentEtag<T>(T document)
        {
            var retval = Guid.NewGuid();
            PropertyInfo prop = EtagPropertyForDocument<T>();
            if (prop.PropertyType == typeof(Guid))
            {
                retval = (Guid) prop.GetValue(document, null);
            }

            return retval;
        }

        public static void SetDocumentEtagNew<T>(T document)
        {

            try
            {
                PropertyInfo prop = EtagPropertyForDocument<T>();
                if (prop.PropertyType == typeof(Guid))
                {
                    prop.SetValue(document, Guid.NewGuid(), null);
                }
            }
            catch (Exception)
            {
                
            }



        }

        public static PropertyInfo EtagPropertyForDocument<T>()
        {
            PropertyInfo prop;
            if (!_typeEtagCache.TryGetValue(typeof(T), out prop))
            {
                prop = typeof (T).GetProperties().First(DocumentEtagFinder);
                _typeEtagCache.TryAdd(typeof (T), prop);
            }

            return prop;
        }
    }
}