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

namespace AppComponents
{
    public enum MessageBoxLocalConfig
    {
        Name,
        OptionalCapacity
    }

    public interface IMessageOutbox : IDisposable
    {
        string Name { get; set; }
        long Capacity { get; }
        bool Pending { get; }
        void Enqueue<T>(T item);
        void Send();
        void AutomaticSend();
        void AutomaticSend(TimeSpan sendDuration);
    }

    public interface IMessageInbox : IDisposable
    {
        string Name { get; set; }
        IEnumerable<object> WaitForMessages(TimeSpan duration);
        IEnumerable<T> WaitForMessages<T>(TimeSpan duration);
    }
}