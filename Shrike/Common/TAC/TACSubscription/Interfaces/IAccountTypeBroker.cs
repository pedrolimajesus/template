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
using AppComponents.Web;

namespace AppComponents.Subscription
{
    public enum BrokerTransactionType
    {
        Express,
        Upgrade
    }

    public interface IAccountTypeBroker
    {
        string AuthCode { get; }
        void DemoteAccount(string toRole, ApplicationUser user = null);
        bool GetBillingPlanFromCoupon(string couponPassKey, out BillingPlan billingPlan, out Coupon coupon);

        BillingPlan GetDefaultBillingPlan();

        void ProcessPaymentNotification(string trxInfo, string invoice, string trxCode, string buyerEmail,
                                        string subRole,
                                        string unSubRole, string subscriptionAuthCodeTemplate,
                                        string subscriptionSignUpTemplate, string subscriptionFailTemplate,
                                        string shareTemplate);

        string PromoteAccount(string couponPassKey, BrokerTransactionType tt, string newRole, bool resetRole = true);

        void SignUpUser(ApplicationUser user, string emailTemplate, string subRole, string unSubRole,
                        string shareTemplate, string authCode = null);
    }


    public sealed class AccountPromotionException : ApplicationException
    {
        public AccountPromotionException()
        {
        }

        public AccountPromotionException(string msg)
            : base(msg)
        {
        }
    }
}