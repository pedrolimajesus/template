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
using System.Linq;
using System.Reflection;

namespace AppComponents.Extensions.EnumEx
{
    public static class EnumExtensions
    {
        public static string EnumName<T>(this T that)
        {
            CheckIsEnum<T>(false);
            string name = Enum.GetName(that.GetType(), that);
            return name;
        }

        public static int EnumValue<T>(this T that)
        {
            CheckIsEnum<T>(false);
            return Convert.ToInt32(that);
        }

        public static IEnumerable<string> RenderTokens(IEnumerable<Enum> tokens)
        {
            return tokens.OrderBy(t => t.EnumName()).Select(t => t.EnumName());
        }

        public static string CombinedToken(IEnumerable<Enum> tokens)
        {
            return string.Join("&&", RenderTokens(tokens));
        }

        public static void CheckIsEnum<T>(bool withFlags)
        {
            if (!typeof (T).IsEnum && typeof (T) != typeof (Enum))
                throw new ArgumentException(string.Format("Type '{0}' is not an enum", typeof (T).FullName));

            if (withFlags && !Attribute.IsDefined(typeof (T), typeof (FlagsAttribute)))
                throw new ArgumentException(string.Format("Type '{0}' doesn't have the 'Flags' attribute",
                                                          typeof (T).FullName));
        }

        public static bool IsFlagSet<T>(this T value, T flag) where T : struct
        {
            CheckIsEnum<T>(true);
            long lValue = Convert.ToInt64(value);
            long lFlag = Convert.ToInt64(flag);
            return (lValue & lFlag) != 0;
        }

        public static IEnumerable<T> GetFlags<T>(this T value) where T : struct
        {
            CheckIsEnum<T>(true);
            foreach (T flag in Enum.GetValues(typeof (T)).Cast<T>())
            {
                if (value.IsFlagSet(flag))
                    yield return flag;
            }
        }

        public static T SetFlags<T>(this T value, T flags, bool on) where T : struct
        {
            CheckIsEnum<T>(true);
            long lValue = Convert.ToInt64(value);
            long lFlag = Convert.ToInt64(flags);
            if (on)
            {
                lValue |= lFlag;
            }
            else
            {
                lValue &= (~lFlag);
            }
            return (T) Enum.ToObject(typeof (T), lValue);
        }

        public static T SetFlags<T>(this T value, T flags) where T : struct
        {
            return value.SetFlags(flags, true);
        }

        public static T ClearFlags<T>(this T value, T flags) where T : struct
        {
            return value.SetFlags(flags, false);
        }

        public static T CombineFlags<T>(this IEnumerable<T> flags) where T : struct
        {
            CheckIsEnum<T>(true);
            long lValue = 0;
            foreach (T flag in flags)
            {
                long lFlag = Convert.ToInt64(flag);
                lValue |= lFlag;
            }
            return (T) Enum.ToObject(typeof (T), lValue);
        }

        public static string GetDescription<T>(this T value) where T : struct
        {
            CheckIsEnum<T>(false);
            string name = Enum.GetName(typeof (T), value);
            if (name != null)
            {
                FieldInfo field = typeof (T).GetField(name);
                if (field != null)
                {
                    DescriptionAttribute attr =
                        Attribute.GetCustomAttribute(field, typeof (DescriptionAttribute)) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return null;
        }
    }
}