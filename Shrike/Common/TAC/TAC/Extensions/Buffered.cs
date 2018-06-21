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
using System.Collections;
using System.Collections.Generic;

namespace AppComponents.Extensions.EnumerableEx
{
    public static partial class EnumerableExtensions
    {
        /// <summary>
        ///   Asynchronously begins buffering an enumeration, even before it is lazy loaded.
        /// </summary>
        public static IEnumerable<T> Buffered<T>(this IEnumerable<T> enumeration)
        {
            return new BufferedEnumerable<T>(enumeration);
        }

        #region Nested type: BufferedEnumerable

        private class BufferedEnumerable<T> : IEnumerable<T>
        {
            private IAsyncResult asyncResult;
            private List<T> buffer;
            private Action bufferAction;
            private IEnumerator<T> enumerator;
            private bool stillBuffering;

            public BufferedEnumerable(IEnumerable<T> enumeration)
            {
                enumerator = enumeration.GetEnumerator();
                stillBuffering = true;
                buffer = new List<T>();
                bufferAction = Buffer;

                asyncResult = bufferAction.BeginInvoke(null, null);
            }

            #region IEnumerable<T> Members

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                IEnumerable<T> bufferedValues = TryGetBufferedValues();

                if (bufferedValues != null)
                {
                    foreach (var value in bufferedValues)
                    {
                        yield return value;
                    }
                }

                while (stillBuffering)
                {
                    bufferedValues = TryGetBufferedValues();


                    if (bufferedValues != null)
                    {
                        foreach (var value in bufferedValues)
                        {
                            yield return value;
                        }
                    }
                }


                bufferAction.EndInvoke(asyncResult);

                foreach (var value in buffer)
                {
                    yield return value;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<T>) this).GetEnumerator();
            }

            #endregion

            private void Buffer()
            {
                try
                {
                    bool more;

                    do
                    {
                        more = false;

                        lock (enumerator)
                        {
                            if (enumerator.MoveNext())
                            {
                                buffer.Add(enumerator.Current);
                                more = true;
                            }
                        }
                    } while (more);
                }
                finally
                {
                    stillBuffering = false;
                }
            }

            private IEnumerable<T> TryGetBufferedValues()
            {
                IEnumerable<T> bufferedValues = null;


                lock (enumerator)
                {
                    if (buffer.Count > 0)
                    {
                        bufferedValues = buffer.ToArray();
                        buffer.Clear();
                    }
                }

                return bufferedValues;
            }
        }

        #endregion
    }
}