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
    public interface IContextProvider
    {
        IEnumerable<Uri> ProvideContexts();
    }


    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    public sealed class NamedContextAttribute : Attribute
    {
        public NamedContextAttribute(string uri)
        {
            Context = new Uri(uri);
        }

        public Uri Context { get; set; }
    }

    public sealed class ContextResolutionException : ApplicationException
    {
        public ContextResolutionException()
        {
        }

        public ContextResolutionException(string msg) : base(msg)
        {
        }
    }
}