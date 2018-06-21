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
using System.Diagnostics;
using System.Linq;
using AppComponents.Extensions.EnumerableEx;
using AppComponents.Raven;
using AppComponents.Web;
using log4net;

namespace AppComponents.Subscription
{
    public class PayPalAccountTypeBroker : AccountTypeBrokerBase, IAccountTypeBroker
    {
        protected DebugOnlyLogger _dblogger;
        protected ILog _logger;

        public PayPalAccountTypeBroker()
        {
            _logger = ClassLogger.Create(GetType());
            _dblogger = DebugOnlyLogger.Create(_logger);
        }


        protected override string BuildPaymentButton(string trxInfo, BillingPlan bp, BrokerTransactionType tt)
        {
            Debug.Assert(!string.IsNullOrEmpty(trxInfo));
            Debug.Assert(null != bp);

            _logger.InfoFormat(
                "Building payment button with transaction info {0}, billing plan {1} and transaction type {2}", trxInfo,
                bp.Name, tt);

            PayPalEncryptedButton subscribeButton = new PayPalEncryptedButton();

            IConfig config = Catalog.Factory.Resolve<IConfig>();
            var businessID = config[PayPalConfiguration.BusinessId];
            var cancelReturnUrl = config[PayPalConfiguration.CancelReturnUrl];
            var notificationUrl = config[PayPalConfiguration.NotificationUrl];

            string itemName, returnUrl, invoice;
            TemporalId payId = new TemporalId();

            if (tt == BrokerTransactionType.Upgrade)
            {
                itemName = config[PayPalConfiguration.UpgradeItem];
                returnUrl = config[PayPalConfiguration.UpgradeReturnUrl];

                invoice = string.Format("{0}:{1}:{2}", UpgradePrefix, bp.Name, payId);
            }
            else
            {
                itemName = config[PayPalConfiguration.ExpressItem];
                returnUrl = config[PayPalConfiguration.ExpressReturnUrl];

                invoice = string.Format("{0}:{1}:{2}", ExpressPrefix, bp.Name, payId);
            }
            _logger.InfoFormat("invoice: {0}", invoice);

            const int one = 1;

            subscribeButton.AddParameter(PayPal.Command, PayPal.ClickSubscription)
                .AddParameter(PayPal.Business, businessID)
                .AddParameter(PayPal.SubscribeButtonLanguage, PayPal.USEnglishLanguage)
                .AddParameter(PayPal.ItemName, itemName)
                .AddParameter(PayPal.BuyerIncludeNoteWithPayment, PayPal.HideNoteFromUser)
                .AddParameter(PayPal.ShippingAddressMode, PayPal.ShippingModeHideAddress)
                .AddParameter(PayPal.ReturnToAppMethod, PayPal.ReturnMethodGetNoVariables)
                .AddParameter(PayPal.GoToOnPayment, returnUrl)
                .AddParameter(PayPal.SubscriptionRecurrence, PayPal.SubscriptionRecurs)
                .AddParameter(PayPal.SubscriptionPrice, string.Format("{0:0.00}", bp.Rate))
                .AddParameter(PayPal.SubscriptionDuration, one.ToString())
                .AddParameter(PayPal.SubscriptionUnits, PayPal.TimeUnitMonth)
                .AddParameter(PayPal.CurrencyCode, PayPal.CurrencyUSDollar)
                .AddParameter(PayPal.TransactionNotificationGatewayURL, notificationUrl)
                .AddParameter(PayPal.GotoOnCancel, cancelReturnUrl)
                .AddParameter(PayPal.Custom, trxInfo)
                .AddParameter(PayPal.InvoiceIdentifier, invoice);


            if (bp.GracePeriodDays > 0)
            {
                subscribeButton.AddParameter(PayPal.TrialPeriodPrice, "0.00")
                    .AddParameter(PayPal.TrialPeriodDuration, bp.GracePeriodDays.ToString())
                    .AddParameter(PayPal.TrialPeriodUnits, PayPal.TimeUnitDay);
            }

            _dblogger.InfoFormat("subscription button: {0}", subscribeButton.Plain);

            return subscribeButton.Encrypted;
        }


        protected override void ProcessExpressTrx(
            string trxInfo, string trxCode, string buyerEmail, string invoice,
            string unSubRole,
            string sendAuthCodeTemplate, string subscriptionFailTemplate)
        {
            Debug.Assert(!string.IsNullOrEmpty(trxCode));
            Debug.Assert(!string.IsNullOrEmpty(trxInfo));
            Debug.Assert(!string.IsNullOrEmpty(invoice));
            Debug.Assert(!string.IsNullOrEmpty(unSubRole));
            Debug.Assert(invoice.Contains(";"));

            _dblogger.InfoFormat("Processing express transaction: trxInfo {0}, trxCode {1}, invoice {2}", trxInfo,
                                 trxCode, invoice);

            var detail = invoice.Split(';');
            BillingPlan bp = null;

            Debug.Assert(detail.Length == 3);

            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                try
                {
                    string billingName = detail[1];

                    bp = ds.Load<BillingPlan>(billingName);

                    if (null == bp)
                    {
                        var es = string.Format("no billing plan found from invoice {0}", invoice);
                        _logger.Error(es);
                        IApplicationAlert on = Catalog.Factory.Resolve<IApplicationAlert>();
                        on.RaiseAlert(ApplicationAlertKind.System, es);
                        throw new ApplicationException();
                    }
                }
                catch (Exception ex)
                {
                    IApplicationAlert on = Catalog.Factory.Resolve<IApplicationAlert>();
                    on.RaiseAlert(ApplicationAlertKind.System, ex);
                    return;
                }

                AuthorizationCode ac = ds.Load<AuthorizationCode>(trxInfo);


                switch (trxCode)
                {
                    case PayPal.SubscriptionSignUp:
                    case PayPal.SubscriptionPayment:
                        Debug.Assert(!string.IsNullOrEmpty(buyerEmail));

                        if (null == ac || ac.Principal == null)
                        {
                            // no user, no auth code. So send the user an authcode to use
                            var newAuthCode = new AuthorizationCode
                                                  {
                                                      Code = trxInfo,
                                                      ExpirationTime = DateTime.UtcNow + TimeSpan.FromDays(90.0),
                                                      Referent = bp.Name,
                                                      EmailedTo = buyerEmail
                                                  };

                            ds.Store(newAuthCode);
                            ds.SaveChanges();


                            var email = SendEmail.CreateFromTemplate(_sender,
                                                                     Enumerable.Repeat(buyerEmail, 1).ToArray(),
                                                                     sendAuthCodeTemplate,
                                                                     trxInfo,
                                                                     newAuthCode.Code);
                            email.Send();

                            _logger.InfoFormat("Sent authcode {0} to user {1}", newAuthCode.Code, buyerEmail);
                        }
                        break;

                    case PayPal.SubscriptionFailed:
                    case PayPal.SubscriptionCancel:
                    case PayPal.SubscriptionEnd:
                        {
                            Debug.Assert(null != ac);

                            var user = ds.Load<ApplicationUser>(ac.Principal);


                            if (null == user)
                            {
                                _logger.ErrorFormat("Payment notification for unknown user {0}", ac.Principal);
                            }
                            else
                            {
                                UnsignUser(user, subscriptionFailTemplate, unSubRole);
                            }
                        }
                        break;
                }
            } // doc session
        }


        protected override void ProcessUpgradeTrx(string trxInfo, string trxCode,
                                                  string subRole, string unSubRole,
                                                  string subFailTemplate, string signUpTempl, string shareTempl)
        {
            Debug.Assert(!string.IsNullOrEmpty(trxInfo));
            Debug.Assert(!string.IsNullOrEmpty(trxCode));
            Debug.Assert(!string.IsNullOrEmpty(subRole));
            Debug.Assert(!string.IsNullOrEmpty(unSubRole));
            Debug.Assert(!string.IsNullOrEmpty(subFailTemplate));
            Debug.Assert(!string.IsNullOrEmpty(signUpTempl));
            Debug.Assert(!string.IsNullOrEmpty(shareTempl));

            _dblogger.InfoFormat("Processing upgrade transaction, trxInfo {0}, trxCode {1}", trxInfo, trxCode);

            ApplicationUser user = null;
            ApplicationUserSubscription subscr = null;
            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                user = ds.Load<ApplicationUser>(trxInfo);
                subscr =
                    (ApplicationUserSubscription)
                    user.Extensions.GetOrAdd(ApplicationUserSubscription.Extension,
                                             _ => new ApplicationUserSubscription());
            }

            if (null != user)
            {
                var bp = subscr.BillingPlan;

                if (null == bp)
                {
                    var es = string.Format("billing plan {0} not found for user {1}", subscr.BillingPlan,
                                           user.PrincipalId);
                    _logger.Error(es);
                    IApplicationAlert on = Catalog.Factory.Resolve<IApplicationAlert>();
                    on.RaiseAlert(ApplicationAlertKind.System, es);
                    return;
                }

                switch (trxCode)
                {
                    case PayPal.SubscriptionSignUp:
                    case PayPal.SubscriptionPayment:
                        SignUpUser(user, signUpTempl, subRole, unSubRole, shareTempl);
                        break;

                    case PayPal.SubscriptionFailed:
                    case PayPal.SubscriptionCancel:
                    case PayPal.SubscriptionEnd:
                        UnsignUser(user, subFailTemplate, unSubRole);
                        break;
                }
            }
            else
            {
                var es = string.Format("user {0} not found for upgrade", trxInfo);
                _logger.Error(es);
                IApplicationAlert on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.System, es);
            }
        }
    }
}