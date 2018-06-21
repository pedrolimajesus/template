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
using Raven.Imports.Newtonsoft.Json.Linq;
//using Newtonsoft.Json.Linq;

namespace AppComponents.Extensions
{
    using System.Linq;

    public class JTokenComparer : IComparer<JToken>, IEqualityComparer<JToken>
    {
        #region IComparer<JToken> Members

        public int Compare(JToken x, JToken y)
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;

            if (x.Type != y.Type)
                return (x.Type - y.Type);

            switch (x.Type)
            {
                case JTokenType.None:
                case JTokenType.Undefined:
                case JTokenType.Null:
                    return 0; // all are nil
                case JTokenType.Object:

                    var xObj = (JObject) x;
                    var yObj = (JObject) y;
                    foreach (var prop in yObj)
                    {
                        JToken value;
                        if (xObj.TryGetValue(prop.Key, out value) == false)
                            continue;
                        var compare = Compare(value, prop.Value);
                        if (compare != 0)
                            return compare;
                    }
                    return 0;
                case JTokenType.Array:
                    var xArray = (JArray) x;
                    var yArray = (JArray) y;

                    for (int i = 0; i < xArray.Count && i < yArray.Count; i++)
                    {
                        var compare = Compare(xArray[i], yArray[i]);
                        if (compare == 0)
                            continue;
                        return compare;
                    }
                    return xArray.Count - yArray.Count;

                case JTokenType.Integer:
                    return x.Value<long>().CompareTo(y.Value<long>());
                case JTokenType.Float:
                    return (x.Value<double>()).CompareTo(y.Value<double>());
                case JTokenType.String:
                    return StringComparer.InvariantCultureIgnoreCase.Compare(x.Value<string>(), y.Value<string>());
                case JTokenType.Boolean:
                    return x.Value<bool>().CompareTo(y.Value<bool>());
                case JTokenType.Date:
                    return x.Value<DateTime>().CompareTo(y.Value<DateTime>());
                case JTokenType.Bytes:
                    var xBytes = x.Value<byte[]>();
                    byte[] yBytes = y.Type == JTokenType.String
                                        ? Convert.FromBase64String(y.Value<string>())
                                        : y.Value<byte[]>();
                    for (int i = 0; i < xBytes.Length && i < yBytes.Length; i++)
                    {
                        if (xBytes[i] != yBytes[i])
                            return xBytes[i] - yBytes[i];
                    }
                    return xBytes.Length - yBytes.Length;
            }

            return String.CompareOrdinal(x.ToString(), y.ToString());
        }

        #endregion

        #region IEqualityComparer<JToken> Members

        public bool Equals(JToken x, JToken y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(JToken obj)
        {
            switch (obj.Type)
            {
                case JTokenType.Null:
                case JTokenType.Undefined:
                case JTokenType.None:
                    return 0;

                case JTokenType.Bytes:
                    return Hash.GetCombinedHashCodeForValCollection(obj.Value<byte[]>());

                case JTokenType.Array:
                    return Hash.GetCombinedHashCodeForCollection(obj);

                case JTokenType.Object:
                    {
                        JObject j = (JObject) obj;

                        return Hash.GetCombinedHashCodeForValCollection(j.AsJEnumerable().Select(GetHashCode));
                    }


                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.Boolean:
                case JTokenType.Date:
                    {
                        var j = ((JValue) obj);
                        if (j.Value == null)
                            return 0;
                        return j.Value.GetHashCode();
                    }

                case JTokenType.String:
                    {
                        {
                            var j = ((JValue) obj);
                            if (j.Value == null)
                                return 0;
                            return StringComparer.InvariantCultureIgnoreCase.GetHashCode(j.Value<string>());
                        }
                    }
            }

            return obj.GetHashCode();
        }

        #endregion
    }
}