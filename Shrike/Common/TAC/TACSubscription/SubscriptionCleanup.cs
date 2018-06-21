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
using System.Threading;
using AppComponents.Extensions.EnumerableEx;
using AppComponents.Raven;
using AppComponents.Web;

namespace AppComponents.Subscription
{
    public enum TrialCleanUpConfiguration
    {
        DuringTrialRoleType,
        PostTrialRoleType,
        TrialEndDiscountCoupon,
        TrialEndEmailTemplateName
    }

    public static class SubscriptionCleanUp
    {
        public static void Register()
        {
            Catalog.Services.RegisterInstance
                (new StochasticRecurringWorker.RecurringWork
                     {
                         Work = CleanCoupons,
                         StochasticBoundaryMin = TimeSpan.FromHours(12.0),
                         StochasticBoundaryMax = TimeSpan.FromHours(48.0),
                         Schedule = DateTime.UtcNow
                     });

            Catalog.Services.RegisterInstance
                (new StochasticRecurringWorker.RecurringWork
                     {
                         Work = CleanTrials,
                         StochasticBoundaryMin = TimeSpan.FromHours(8.0),
                         StochasticBoundaryMax = TimeSpan.FromHours(24.0),
                         Schedule = DateTime.UtcNow
                     });
        }

        public static void CleanCoupons(CancellationToken ct)
        {
            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var expired = (from c in ds.Query<Coupon>() where c.Expiration < DateTime.UtcNow select c).Take(512);

                if (ct.IsCancellationRequested)
                    return;

                expired.ForEach(ex => ds.Delete(ex));


                if (ct.IsCancellationRequested)
                    return;

                var expiredAC =
                    (from a in ds.Query<AuthorizationCode>() where a.ExpirationTime < DateTime.UtcNow select a).Take(512);

                if (ct.IsCancellationRequested)
                    return;

                expiredAC.ForEach(ex => ds.Delete(ex));


                ds.SaveChanges();
            }
        }

        public static void CleanTrials(CancellationToken ct)
        {
            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var config = Catalog.Factory.Resolve<IConfig>();
                string duringTrialRole = config.Get(TrialCleanUpConfiguration.DuringTrialRoleType, string.Empty);
                string postTrialRole = config.Get(TrialCleanUpConfiguration.PostTrialRoleType, string.Empty);
                string discountCoupon = config.Get(TrialCleanUpConfiguration.TrialEndDiscountCoupon, string.Empty);


                string emailTemplate = config.Get(TrialCleanUpConfiguration.TrialEndEmailTemplateName, string.Empty);
                string sender = config[SendEmailSettings.EmailReplyAddress];


                if (!string.IsNullOrEmpty(duringTrialRole) && !string.IsNullOrEmpty(postTrialRole))
                {
                    var expiredTrials = (from u in ds.Query<ApplicationUser>()
                                         where u.AccountRoles.Contains(duringTrialRole) &&
                                               u.Extensions.ContainsKey(ApplicationUserSubscription.Extension)
                                         let subscr =
                                             (ApplicationUserSubscription)
                                             u.Extensions[ApplicationUserSubscription.Extension]
                                         where !string.IsNullOrEmpty(subscr.HadTrialAccount) &&
                                               subscr.BillingStatus == BillingStatus.LimitedTrial &&
                                               DateTime.UtcNow > subscr.SubscriptionEnd
                                         select u).Take(512);


                    if (expiredTrials.Any())
                    {
                        if (ct.IsCancellationRequested)
                            return;

                        foreach (var exp in expiredTrials)
                        {
                            if (ct.IsCancellationRequested)
                                return;

                            if (!string.IsNullOrEmpty(discountCoupon) && !string.IsNullOrEmpty(emailTemplate) &&
                                !string.IsNullOrEmpty(exp.ContactEmail))
                            {
                                var matchingCoupon = ds.Load<Coupon>(discountCoupon);


                                if (null != matchingCoupon)
                                {
                                    SendEmail.CreateFromTemplate(sender, new[] {exp.ContactEmail}, emailTemplate,
                                                                 matchingCoupon.PassString).Send();
                                }
                            }


                            exp.AccountRoles.Clear();
                            exp.AccountRoles.Add(postTrialRole);
                            var subscr =
                                (ApplicationUserSubscription) exp.Extensions[ApplicationUserSubscription.Extension];
                            subscr.BillingStatus = BillingStatus.NotEnrolled;
                        }

                        ds.SaveChanges();
                    }
                }
            }
        }
    }
}