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
using System.Threading;

namespace AppComponents
{
    public class NestedAsyncHelper
    {
        private AsyncCallback _callback;
        private Object _extraState;
        private Object _state;

        public static NestedAsyncHelper WrapBeginParameters(AsyncCallback callback, object state, object extraState)
        {
            return new NestedAsyncHelper {_state = state, _callback = callback, _extraState = extraState};
        }


        public static void Callback(IAsyncResult asyncResult)
        {
            NestedAsyncHelper myState = (NestedAsyncHelper) asyncResult.AsyncState;

            if (myState != null && myState._callback != null)
            {
                myState._callback(new AsyncResultWrapper(asyncResult, myState._state));
            }
        }

        public IAsyncResult WrapAsyncResult(IAsyncResult asyncResult)
        {
            return new AsyncResultWrapper(asyncResult, _state);
        }

        public static object GetExtraState(IAsyncResult asyncResult)
        {
            AsyncResultWrapper asyncWrapper = (AsyncResultWrapper) asyncResult;

            NestedAsyncHelper myState = (NestedAsyncHelper) asyncWrapper.OriginalAsyncResult.AsyncState;

            return myState._extraState;
        }

        public static IAsyncResult UnwrapAsyncResult(IAsyncResult asyncResult)
        {
            AsyncResultWrapper asyncWrapper = (AsyncResultWrapper) asyncResult;
            return asyncWrapper.OriginalAsyncResult;
        }

        #region Nested type: AsyncResultWrapper

        private class AsyncResultWrapper : IAsyncResult
        {
            private readonly Object _overrideState;
            private readonly IAsyncResult _parent;

            public AsyncResultWrapper(IAsyncResult parent, object overrideState)
            {
                _parent = parent;
                _overrideState = overrideState;
            }

            public IAsyncResult OriginalAsyncResult
            {
                get { return _parent; }
            }

            #region IAsyncResult Members

            public object AsyncState
            {
                get { return _overrideState; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return _parent.AsyncWaitHandle; }
            }

            public bool CompletedSynchronously
            {
                get { return _parent.CompletedSynchronously; }
            }

            public bool IsCompleted
            {
                get { return _parent.IsCompleted; }
            }

            #endregion
        }

        #endregion
    }
}