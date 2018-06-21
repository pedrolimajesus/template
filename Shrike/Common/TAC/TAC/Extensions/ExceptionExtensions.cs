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
using System.Globalization;
using System.Text;
using log4net;

namespace AppComponents.Extensions.ExceptionEx
{
    public static class ExceptionExtensions
    {
        private const string Line = "==============================================================================";


        public static void TraceException(this ILog log, Exception ex)
        {
            log.ErrorFormat("Exception traced: {0}", ex.TraceInformation());
        }

        public static string TraceInformation(this Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            var exceptionInformation = new StringBuilder();

            exceptionInformation.Append(BuildMessage(exception));

            Exception inner = exception.InnerException;

            while (inner != null)
            {
                exceptionInformation.Append(Environment.NewLine);
                exceptionInformation.Append(Environment.NewLine);
                exceptionInformation.Append(BuildMessage(inner));
                inner = inner.InnerException;
            }

            return exceptionInformation.ToString();
        }

        private static string BuildMessage(Exception exception)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}{2}:{3}{4}{5}{6}{7}",
                Line,
                Environment.NewLine,
                exception.GetType().Name,
                exception.Message,
                Environment.NewLine,
                exception.StackTrace,
                Environment.NewLine,
                Line);
        }
    }
}