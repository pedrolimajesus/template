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

namespace AppComponents.Subscription
{
    using System.Xml.Serialization;

    using global::Raven.Imports.Newtonsoft.Json;

    public class ApplicationUserSubscription
    {
        [Newtonsoft.Json.JsonIgnore]
        [JsonIgnore]
        [XmlIgnore]
        public const string Extension = "Subscription";

        public ApplicationUserSubscription()
        {
            BillingStatus = BillingStatus.NeverBilled;
        }

        public BillingPlan BillingPlan { get; set; }
        public DateTime SubscriptionStart { get; set; }
        public BillingStatus BillingStatus { get; set; }
        public string HadTrialAccount { get; set; }
        public DateTime SubscriptionEnd { get; set; }
    }
}