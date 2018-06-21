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

namespace AppComponents
{
    public static class Disposable
    {
        public static IDisposable Create(Action a)
        {
            return new AnonymousDisposable(a);
        }

        public static IDisposable Enclose(object o)
        {
            if (o is IDisposable)
                return (IDisposable) o;

            return Create(() => { });
        }
    }

    public class AnonymousDisposable : IDisposable
    {
        private readonly Action _onDispose;

        public AnonymousDisposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        #region IDisposable Members

        public void Dispose()
        {
            _onDispose();
        }

        #endregion
    }
}