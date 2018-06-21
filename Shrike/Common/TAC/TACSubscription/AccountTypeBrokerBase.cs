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
using System.Diagnostics;
using System.Linq;
using AppComponents.Extensions.EnumerableEx;
using AppComponents.Raven;
using AppComponents.Web;
using log4net;

namespace AppComponents.Subscription
{
    public abstract class AccountTypeBrokerBase : IAccountTypeBroker
    {
        public const string DefaultBillingPlanName = "Default";


        protected static readonly string UpgradePrefix = "UPGP@";
        protected static readonly string ExpressPrefix = "EXPP@";
        protected string _authCode;
        protected IConfig _config;
        protected DebugOnlyLogger _dblog;
        protected ILog _log;
        protected string _sender;

        public AccountTypeBrokerBase()
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);
            _config = Catalog.Factory.Resolve<IConfig>();

            _sender = _config[SendEmailSettings.EmailReplyAddress];
            _authCode = string.Empty;
        }

        #region IAccountTypeBroker Members

        public string AuthCode
        {
            get { return _authCode; }
        }


        public virtual void DemoteAccount(string toRole, ApplicationUser user = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(toRole));

            if (null == user)
            {
                IUserContext _uc = Catalog.Factory.Resolve<IUserContext>();
                user = _uc.GetCurrentUser();
            }

            var billingExt =
                (ApplicationUserSubscription) user.Extensions.GetOrAdd(ApplicationUserSubscription.Extension,
                                                                       _ => new ApplicationUserSubscription());

            billingExt.BillingPlan = null;
            billingExt.SubscriptionEnd = DateTime.UtcNow;
            billingExt.BillingStatus = BillingStatus.NotEnrolled;

            user.AccountRoles = new List<string>(new[] {toRole});

            _dblog.InfoFormat("User {0} demoted to role {1}", user.PrincipalId, toRole);

            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                ds.Store(user);
                ds.SaveChanges();
            }
        }


        public virtual bool GetBillingPlanFromCoupon(string couponPassKey, out BillingPlan billingPlan,
                                                     out Coupon coupon)
        {
            Debug.Assert(!string.IsNullOrEmpty(couponPassKey));

            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                billingPlan = null;
                coupon = ds.Load<Coupon>(couponPassKey);
            }

            if (null == coupon)
            {
                _dblog.InfoFormat("No coupon found for passkey {0}", couponPassKey);
                return false;
            }

            IUserContext _uc = Catalog.Factory.Resolve<IUserContext>();
            var user = _uc.GetCurrentUser();

            if (!string.IsNullOrEmpty(coupon.Target))
            {
                if (user.PrincipalId != coupon.Target)
                {
                    _dblog.InfoFormat("User {0} is not the target of coupon {1}", user.PrincipalId, coupon.PassString);
                    return false;
                }
            }

            if (coupon.TotalAllowed > 0 && (coupon.CurrentCount >= coupon.TotalAllowed))
            {
                _dblog.InfoFormat("Coupon {0} exceeded allowed counts of {1}", coupon.PassString, coupon.TotalAllowed);
                return false;
            }

            billingPlan = coupon.BillingPlan;

            if (null == billingPlan)
            {
                var es = string.Format("Coupon does not have a proper billing plan: coupon {0}", coupon.PassString);
                _log.Error(es);
                IApplicationAlert on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es);

                return false;
            }

            _dblog.InfoFormat("Coupon passkey {0} linked to billing plan {1}", couponPassKey, billingPlan.Name);

            return true;
        }


        public virtual BillingPlan GetDefaultBillingPlan()
        {
            BillingPlan dbp = null;
            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                dbp = ds.Load<BillingPlan>(DefaultBillingPlanName);
            }

            Debug.Assert(null != dbp);
            if (null == dbp)
            {
                var es = "No default billing plan found!";
                _log.FatalFormat(es);
                IApplicationAlert on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es);
            }

            return dbp;
        }

        public virtual string PromoteAccount(string couponPassKey, BrokerTransactionType tt, string newRole,
                                             bool resetRole = true)
        {
            Debug.Assert(!string.IsNullOrEmpty(couponPassKey));
            Debug.Assert(!string.IsNullOrEmpty(newRole));

            string retval = null;

            BillingPlan bp = null;
            Coupon coupon = null;

            if (!string.IsNullOrEmpty(couponPassKey))
            {
                if (GetBillingPlanFromCoupon(couponPassKey, out bp, out coupon))
                {
                    coupon.CurrentCount++;
                    _dblog.InfoFormat("Coupon {0} now used {1} times of {2}", coupon.PassString, coupon.CurrentCount,
                                      coupon.TotalAllowed);
                }
            }

            if (null == bp)
            {
                bp = GetDefaultBillingPlan();
                if (null == bp)
                {
                    throw new AccountPromotionException("No default billing plan is available.");
                }
            }

            bool requiresPayment = bp.RecurrenceType > RecurrenceTypes._Paid;

            IUserContext uc = null;
            ApplicationUser user = null;


            if (requiresPayment)
            {
                string trxInfo = null;
                if (tt == BrokerTransactionType.Express)
                {
                    _authCode = AuthorizationCode.GenerateCode();
                    trxInfo = AuthCode;
                    _dblog.InfoFormat("Generated authcode {0} for express transaction button", trxInfo);
                }
                else if (tt == BrokerTransactionType.Upgrade)
                {
                    uc = Catalog.Factory.Resolve<IUserContext>();
                    user = uc.GetCurrentUser();
                    trxInfo = user.PrincipalId;

                    var subscr =
                        (ApplicationUserSubscription) user.Extensions.GetOrAdd(ApplicationUserSubscription.Extension,
                                                                               _ => new ApplicationUserSubscription());

                    subscr.BillingPlan = bp;
                    _dblog.InfoFormat("Generating upgrade payment button for user {0} to billing plan {1}",
                                      user.PrincipalId, bp.Name);
                }

                retval = BuildPaymentButton(trxInfo, bp, tt);
            }
            else
            {
                retval = null;
                uc = Catalog.Factory.Resolve<IUserContext>();
                user = uc.GetCurrentUser();

                var subscr =
                    (ApplicationUserSubscription) user.Extensions.GetOrAdd(ApplicationUserSubscription.Extension,
                                                                           _ => new ApplicationUserSubscription());

                Debug.Assert(null != user);
                if (null == user)
                {
                    throw new AccountPromotionException("User not signed in.");
                }

                subscr.BillingPlan = bp;

                switch (bp.RecurrenceType)
                {
                    case RecurrenceTypes.FreeForLife:
                        if (resetRole)
                            user.AccountRoles.Clear();
                        user.AccountRoles.Add(newRole);
                        subscr.BillingPlan = bp;
                        subscr.SubscriptionStart = DateTime.UtcNow;
                        subscr.SubscriptionEnd = DateTime.MaxValue;
                        subscr.BillingStatus = BillingStatus.NeverBilled;
                        _log.InfoFormat("User {0} promoted to free for life", user.PrincipalId);
                        break;

                    case RecurrenceTypes.LimitedTrial:
                        if (subscr.HadTrialAccount == couponPassKey)
                        {
                            _log.WarnFormat("User {0} already used trial account for {1}", user.PrincipalId,
                                            couponPassKey);
                            throw new AccountPromotionException("Cannot use a trial account on this user.");
                        }

                        if (resetRole)
                            user.AccountRoles.Clear();
                        user.AccountRoles.Add(newRole);
                        subscr.SubscriptionStart = DateTime.UtcNow;
                        subscr.SubscriptionEnd = DateTime.UtcNow + TimeSpan.FromDays(bp.GracePeriodDays);
                        subscr.HadTrialAccount = couponPassKey;
                        subscr.BillingStatus = BillingStatus.LimitedTrial;
                        _log.InfoFormat("user {0} promoted to limited trial until {1} using coupon {2}",
                                        user.PrincipalId, subscr.SubscriptionEnd, couponPassKey);
                        break;

                    case RecurrenceTypes.Owner:
                        user.AccountRoles.Add(newRole);
                        subscr.BillingStatus = BillingStatus.NeverBilled;
                        _log.WarnFormat("user {0} promoted to owner", user.PrincipalId);
                        break;

                    default:
                        var bad = string.Format("Billing plan {0} has invalid recurrence type {1}", bp.Name,
                                                bp.RecurrenceType.ToString());
                        _log.ErrorFormat(bad);
                        throw new AccessViolationException(bad);
                        //break;
                }
            }

            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                // user
                if (null != user)
                {
                    ds.Store(user);
                }

                // coupon
                if (null != coupon)
                {
                    ds.Store(coupon);
                }

                ds.SaveChanges();
            }


            return retval;
        }


        public virtual void ProcessPaymentNotification(
            string trxInfo,
            string invoice,
            string trxCode,
            string buyerEmail,
            string subRole,
            string unSubRole,
            string subscriptionAuthCodeTemplate,
            string subscriptionSignUpTemplate,
            string subscriptionFailTemplate,
            string shareTemplate)
        {
            Debug.Assert(invoice.StartsWith(UpgradePrefix) || invoice.StartsWith(ExpressPrefix));
            Debug.Assert(!string.IsNullOrEmpty(trxInfo));
            Debug.Assert(!string.IsNullOrEmpty(invoice));
            Debug.Assert(!string.IsNullOrEmpty(trxCode));
            Debug.Assert(!string.IsNullOrEmpty(subRole));
            Debug.Assert(!string.IsNullOrEmpty(unSubRole));


            if (string.IsNullOrEmpty(_sender))
                _sender = _config[SendEmailSettings.EmailReplyAddress];

            if (invoice.StartsWith(UpgradePrefix))
                ProcessUpgradeTrx(trxInfo, trxCode,
                                  subRole, unSubRole,
                                  subscriptionFailTemplate, subscriptionSignUpTemplate, shareTemplate);

            if (invoice.StartsWith(ExpressPrefix))
                ProcessExpressTrx(trxInfo, trxCode, buyerEmail, invoice, unSubRole, subscriptionAuthCodeTemplate,
                                  subscriptionFailTemplate);
        }

        public void SignUpUser(ApplicationUser user, string emailTemplate, string subRole, string unSubRole,
                               string shareTemplate, string authCode = null)
        {
            Debug.Assert(null != user);
            Debug.Assert(!string.IsNullOrEmpty(emailTemplate));
            Debug.Assert(!string.IsNullOrEmpty(subRole));
            Debug.Assert(!string.IsNullOrEmpty(unSubRole));
            Debug.Assert(!string.IsNullOrEmpty(shareTemplate));

            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                var subscr =
                    (ApplicationUserSubscription)
                    user.Extensions.GetOrAdd(ApplicationUserSubscription.Extension,
                                             _ => new ApplicationUserSubscription());
                bool updateUser = false;

                if (!string.IsNullOrEmpty(authCode))
                {
                    _dblog.InfoFormat("Attempting to sign up against authcode {0}", authCode);


                    var auth = ds.Include<AuthorizationCode>(ac => ac.Referent).Load(authCode);
                    if (null == auth)
                    {
                        throw new AuthCodeException(
                            "Your authorization code is good, but payment has not been received yet. Please try again in a little while.");
                    }

                    var bp = ds.Load<BillingPlan>(auth.Referent);

                    if (null == bp)
                    {
                        throw new AuthCodeException(
                            "There was a problem with your authorization code. Please email us about this so we can get your account up immediately.");
                    }

                    subscr.BillingPlan = bp;
                    updateUser = true;
                }

                // user already subscribed or not?
                if (!user.AccountRoles.Contains(subRole))
                {
                    SendEmail email;

                    if (user.AccountRoles.Contains(unSubRole))
                        user.AccountRoles.Remove(unSubRole);

                    user.AccountRoles.Add(subRole);


                    if (subscr.SubscriptionStart == DateTime.MinValue)
                        subscr.SubscriptionStart = DateTime.UtcNow;

                    subscr.BillingStatus = BillingStatus.Billing;
                    updateUser = true;


                    _log.InfoFormat("Signed user {0} to role {1}", user.PrincipalId, subRole);

                    var bp = subscr.BillingPlan;
                    if (null != bp && bp.AutoShare)
                    {
                        _log.InfoFormat("Distributing shared coupon for billing plan {0} to user {1}", bp.Name,
                                        user.PrincipalId);
                        Coupon sharedCoupon = new Coupon
                                                  {
                                                      BillingPlan = bp,
                                                      CurrentCount = 0,
                                                      Expiration = DateTime.UtcNow + TimeSpan.FromDays(7.0),
                                                      PassString = GuidEncoder.Encode(Guid.NewGuid()),
                                                      TotalAllowed = 1
                                                  };

                        ds.Store(sharedCoupon);


                        email = SendEmail.CreateFromTemplate(_sender,
                                                             Enumerable.Repeat(user.ContactEmail, 1).ToArray(),
                                                             shareTemplate,
                                                             user.ContactFirstName,
                                                             sharedCoupon.PassString);
                    }
                    else
                    {
                        email = SendEmail.CreateFromTemplate(_sender,
                                                             Enumerable.Repeat(user.ContactEmail, 1).ToArray(),
                                                             emailTemplate,
                                                             user.ContactFirstName);
                    }


                    email.Send();
                }

                if (updateUser)
                {
                    ds.Store(user);
                }

                ds.SaveChanges();
            } // document session
        }

        #endregion

        protected abstract string BuildPaymentButton(string trxInfo, BillingPlan bp, BrokerTransactionType tt);


        protected abstract void ProcessExpressTrx(
            string trxInfo, string trxCode, string buyerEmail, string invoice,
            string unSubRole,
            string sendAuthCodeTemplate, string subscriptionFailTemplate);


        protected void UnsignUser(ApplicationUser user, string emailTemplate, string unsubRole)
        {
            Debug.Assert(null != user);
            Debug.Assert(!string.IsNullOrEmpty(emailTemplate));
            Debug.Assert(!string.IsNullOrEmpty(unsubRole));

            _dblog.InfoFormat("Unsigning user {0} to role {1}", user.PrincipalId, unsubRole);

            user.AccountRoles.Clear();
            user.AccountRoles.Add(unsubRole);

            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                ds.Store(user);
                ds.SaveChanges();
            }


            var email = SendEmail.CreateFromTemplate(_sender, new[] {user.ContactEmail}, emailTemplate,
                                                     user.ContactFirstName);
            email.Send();
        }

        protected abstract void ProcessUpgradeTrx(string trxInfo, string trxCode,
                                                  string subRole, string unSubRole,
                                                  string subFailTemplate, string signUpTempl, string shareTempl);
    }
}