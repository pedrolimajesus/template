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
using System.Linq;
using AppComponents.Raven;
using Raven.Client.Indexes;

namespace AppComponents.Subscription
{
    public enum BillingStatus
    {
        NotEnrolled,
        LimitedTrial,
        Billing,
        NeverBilled
    }

    public enum RecurrenceTypes
    {
        _Unpaid = 0,

        Owner,
        LimitedTrial,
        FreeForLife,

        _Paid = 100,
        MonthlySubscription,
        // OneTimePayment, // not supported

        _Last
    }

    public class SubscriptionEntitesIndexTag : AbstractIndexCreationGroupAssemblyTag
    {
    }

    public class BillingPlan
    {
        [DocumentIdentifier]
        public string Name { get; set; }

        public RecurrenceTypes RecurrenceType { get; set; }

        public Decimal Rate { get; set; }
        public int GracePeriodDays { get; set; }

        public int CouponCode { get; set; }

        public bool AutoShare { get; set; }
    }

    public class BillingPlan_ByCode : AbstractIndexCreationTask<BillingPlan>
    {
        public BillingPlan_ByCode()
        {
            Map = plans => from pl in plans select new {pl.CouponCode};
        }
    }

    public class PaymentTransaction
    {
        public string Id { get; set; }
        public string ThirdPartyRef { get; set; }
        public string Response { get; set; }
        public string Item { get; set; }
        public Decimal Amount { get; set; }
        public string Currency { get; set; }
        public string ReceiverEmail { get; set; }
        public string PayerEmail { get; set; }
        public string Principal { get; set; }
        public string TransactionCode { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime TransactionTime { get; set; }
    }

    public class PaymentTransaction_ByPrincipal : AbstractIndexCreationTask<PaymentTransaction>
    {
        public PaymentTransaction_ByPrincipal()
        {
            Map = transactions => from trx in transactions select new {trx.Principal};
        }
    }

    public class PaymentTransaction_ByPaymentStatus : AbstractIndexCreationTask<PaymentTransaction>
    {
        public PaymentTransaction_ByPaymentStatus()
        {
            Map = transactions => from trx in transactions select new {trx.PaymentStatus};
        }
    }


    public class Coupon
    {
        [DocumentIdentifier]
        public string PassString { get; set; }

        public int CurrentCount { get; set; }
        public int TotalAllowed { get; set; }
        public string Target { get; set; }
        public DateTime Expiration { get; set; }
        public BillingPlan BillingPlan { get; set; }
    }

    public class Coupon_ByTarget : AbstractIndexCreationTask<Coupon>
    {
        public Coupon_ByTarget()
        {
            Map = coupons => from c in coupons select new {c.Target};
        }
    }

    public class OwnerNotifications
    {
        public string Id { get; set; }
        public DateTime NotificationTime { get; set; }
        public string Notification { get; set; }
    }
}