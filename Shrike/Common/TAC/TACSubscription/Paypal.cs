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
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using AppComponents.Extensions.ExceptionEx;
using AppComponents.Raven;
using log4net;
using X509 = System.Security.Cryptography.X509Certificates;

namespace AppComponents.Subscription
{
    public static class PayPal
    {
        public const string Command = "cmd";
        public const string ClickSubscription = "_xclick-subscriptions";
        public const string Business = "business";
        public const string SubscribeButtonLanguage = "lc";
        public const string ItemName = "item_name";
        public const string BuyerIncludeNoteWithPayment = "no_note";
        public const string ShippingAddressMode = "no_shipping";
        public const string ReturnToAppMethod = "rm";
        public const string GoToOnPayment = "return";
        public const string SubscriptionRecurrence = "src";
        public const string SubscriptionPrice = "a3";
        public const string SubscriptionDuration = "p3";
        public const string SubscriptionUnits = "t3";
        public const string CurrencyCode = "currency_code";
        public const string TransactionNotificationGatewayURL = "notify_url";
        public const string GotoOnCancel = "cancel_return";
        public const string Custom = "custom";
        public const string InvoiceIdentifier = "invoice";
        public const string TrialPeriodPrice = "a1";
        public const string TrialPeriodDuration = "p1";
        public const string TrialPeriodUnits = "t1";


        public const string USEnglishLanguage = "US";
        public const string HideNoteFromUser = "1";
        public const string ShippingModeRequireAddress = "2";
        public const string ShippingModeHideAddress = "1";
        public const string ReturnMethodGetNoVariables = "1";
        public const string TimeUnitMonth = "M";
        public const string TimeUnitDay = "D";
        public const string CurrencyUSDollar = "USD";
        public const string SubscriptionRecurs = "1";


        public const string SubscriptionSignUp = "subscr_signup";
        public const string SubscriptionCancel = "subscr_cancel";
        public const string SubscriptionFailed = "subscr_failed";
        public const string SubscriptionPayment = "subscr_payment";
        public const string SubscriptionEnd = "subscr_eot";

        public const string ReceiverEmail = "receiver_email";
        public const string PayerEmail = "payer_email";
        public const string TransactionCode = "txn_type";
        public const string TransactionId = "txn_id";


        public const string Verified = "VERIFIED";
        public const string Invalid = "INVALID";
    }

    public class PayPalEncryption
    {
        private Encoding _encoding = Encoding.Default;
        private X509Certificate2 _recipientCert;
        private string _recipientPublicCertPath;
        private X509Certificate2 _signerCert;

        #region Properties

        /// <summary>
        ///   Character encoding, e.g. UTF-8, Windows-1252
        /// </summary>
        public string Charset
        {
            get { return _encoding.WebName; }
            set
            {
                if (value != null && value != "")
                {
                    _encoding = Encoding.GetEncoding(value);
                }
            }
        }

        /// <summary>
        ///   Path to the recipient's public certificate in PEM format
        /// </summary>
        public string RecipientPublicCertPath
        {
            get { return _recipientPublicCertPath; }
            set
            {
                _recipientPublicCertPath = value;
                _recipientCert =
                    new X509Certificate2(_recipientPublicCertPath);
            }
        }

        #endregion

        /// <summary>
        ///   Loads the PKCS12 file which contains the public certificate and private key of the signer
        /// </summary>
        /// <param name="signerPfxCertPath"> File path to the signer's public certificate plus private key in PKCS#12 format </param>
        /// <param name="signerPfxCertPassword"> Password for signer's private key </param>
        public void LoadSignerCredential(string signerPfxCertPath,
                                         string signerPfxCertPassword)
        {
            _signerCert =
                new X509Certificate2(signerPfxCertPath, signerPfxCertPassword);
        }


        /// <summary>
        ///   Sign a message and encrypt it for the recipient.
        /// </summary>
        /// <param name="clearText"> _name value pairs must be separated by \n (vbLf or Chr(10)), for example "cmd=_xclick\nbusiness=..." </param>
        /// <returns> </returns>
        public string SignAndEncrypt(string clearText)
        {
            string result = null;
            byte[] messageBytes = _encoding.GetBytes(clearText);
            byte[] signedBytes = Sign(messageBytes);
            byte[] encryptedBytes = Envelope(signedBytes);
            result = Base64Encode(encryptedBytes);
            return result;
        }


        private byte[] Sign(byte[] messageBytes)
        {
            ContentInfo content = new ContentInfo(messageBytes);
            SignedCms signed = new SignedCms(content);
            CmsSigner signer = new CmsSigner(_signerCert);
            signed.ComputeSignature(signer);
            byte[] signedBytes = signed.Encode();
            return signedBytes;
        }


        private byte[] Envelope(byte[] contentBytes)
        {
            ContentInfo content = new ContentInfo(contentBytes);
            EnvelopedCms envMsg = new EnvelopedCms(content);
            CmsRecipient recipient =
                new CmsRecipient(
                    SubjectIdentifierType.IssuerAndSerialNumber, _recipientCert);
            envMsg.Encrypt(recipient);
            byte[] encryptedBytes = envMsg.Encode();
            return encryptedBytes;
        }


        private string Base64Encode(byte[] encoded)
        {
            const string PKCS7_HEADER = "-----BEGIN PKCS7-----";
            const string PKCS7_FOOTER = "-----END PKCS7-----";
            string base64 = Convert.ToBase64String(encoded);
            StringBuilder formatted = new StringBuilder();
            formatted.Append(PKCS7_HEADER);
            formatted.Append(base64);
            formatted.Append(PKCS7_FOOTER);
            return formatted.ToString();
        }
    }


    public enum PayPalConfiguration
    {
        PayPalCert,
        SignerCert,
        SignerPass,
        PayPalSignerId,
        PostUrl,
        BusinessId,
        UpgradeItem,
        ExpressItem,
        UpgradeReturnUrl,
        ExpressReturnUrl,
        CancelReturnUrl,
        NotificationUrl,
        ReceiveEmail
    }

    public class PayPalEncryptedButton
    {
        private IConfig _config;
        private ILocalFileMirror _mirror;
        private string _plain = string.Empty;

        public PayPalEncryptedButton()
        {
            _config = Catalog.Factory.Resolve<IConfig>();
            _mirror = Catalog.Factory.Resolve<ILocalFileMirror>();
        }


        public string Plain
        {
            get { return _plain; }
        }

        public string Encrypted
        {
            get
            {
                string encrypted = string.Empty;
                string fileFolder = _mirror.TargetPath;

                // load paypal cert path
                string paypalCertPath = Path.Combine(fileFolder, _config[PayPalConfiguration.PayPalCert]);

                // load signer pfx path
                string signerPfxPath = Path.Combine(fileFolder, _config[PayPalConfiguration.SignerCert]);

                // load password
                string signerPfxPassword = _config[PayPalConfiguration.SignerPass];

                // add cert id to cleartext
                string certId = _config[PayPalConfiguration.PayPalSignerId];

                AddParameter("cert_id", certId);

                PayPalEncryption ewp = new PayPalEncryption();
                ewp.LoadSignerCredential(signerPfxPath, signerPfxPassword);
                ewp.RecipientPublicCertPath = paypalCertPath;

                encrypted = ewp.SignAndEncrypt(_plain);

                // get encrypted data
                return encrypted;
            }
        }

        public PayPalEncryptedButton AddParameter(string name, string value)
        {
            _plain += string.Format("{0}={1}\n", name, value);
            return this;
        }

        public static void Click(HttpResponse wr, string transaction)
        {
            IConfig config = Catalog.Factory.Resolve<IConfig>();

            string postBackUrl = config[PayPalConfiguration.PostUrl];
            string certId = config[PayPalConfiguration.PayPalSignerId];
            string transactionPost = string.Format("{0}?cmd=_s-xclick&encrypted={1}", postBackUrl,
                                                   HttpUtility.UrlEncode(transaction));

            wr.Clear();
            wr.Write("<html><head>");
            wr.Write("</head><body onload='document.form1.submit()'>Going to PayPal ... one second");
            wr.Write(string.Format("<form action='{0}' name='form1' method='post' visible='false'>", postBackUrl));
            wr.Write("<input type='hidden' name='cmd' value='_s-xclick'>");
            wr.Write(string.Format("<input type='hidden' name='encrypted' value='{0}'>", transaction));
            wr.Write("<input type='submit' value='' >");
            wr.Write("<img alt='' border='0' " +
                     "src='https://www.paypal.com/en_US/i/scr/pixel.gif' " +
                     "width='1' height='1'>");
            wr.Write("</form>");
            wr.Write("</body></html>");
            wr.End();
        }
    }


    public class IPNGatewayException : ApplicationException
    {
        public IPNGatewayException()
        {
        }

        public IPNGatewayException(string msg)
            : base(msg)
        {
        }
    }

    public class PayPalIPNGateway
    {
        private IAccountTypeBroker _accountBroker;
        private IConfig _config;
        private DebugOnlyLogger _dblog;
        private ILog _log;

        public PayPalIPNGateway()
        {
            _accountBroker = Catalog.Factory.Resolve<IAccountTypeBroker>();
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);
            _config = Catalog.Factory.Resolve<IConfig>();
        }


        public string SubscriptionRole { get; set; }

        public string UnsubscribedRole { get; set; }

        public string EmailTemplateSubscriptionAuthCode { get; set; }
        public string EmailTemplateSubscriptionSignUp { get; set; }
        public string EmailTemplateSubscriptionFail { get; set; }
        public string EmailTemplateShare { get; set; }


        public void ProcessIPNRequest(HttpRequest ipnRequest)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(EmailTemplateShare));
            Debug.Assert(!string.IsNullOrWhiteSpace(EmailTemplateSubscriptionAuthCode));
            Debug.Assert(!string.IsNullOrWhiteSpace(EmailTemplateSubscriptionFail));
            Debug.Assert(!string.IsNullOrWhiteSpace(EmailTemplateSubscriptionSignUp));
            Debug.Assert(!string.IsNullOrWhiteSpace(SubscriptionRole));
            Debug.Assert(!string.IsNullOrWhiteSpace(UnsubscribedRole));


            PaymentTransaction trx = null;

            try
            {
                string itemName = ipnRequest.Form[PayPal.ItemName];
                string receiverEmail = ipnRequest.Form[PayPal.ReceiverEmail];
                string payerEmail = ipnRequest.Form[PayPal.PayerEmail];
                string trxInfo = ipnRequest.Form[PayPal.Custom];
                string transactionCode = ipnRequest.Form[PayPal.TransactionCode];
                string invoice = ipnRequest.Form[PayPal.InvoiceIdentifier];
                string txnId = ipnRequest.Form[PayPal.TransactionId];
                if (string.IsNullOrEmpty(txnId))
                    txnId = invoice;
                _dblog.InfoFormat("Payment notification: {0}", ipnRequest.Form);

                string[] checkNames = {
                                          PayPal.ItemName, PayPal.ReceiverEmail, PayPal.PayerEmail, PayPal.Custom,
                                          PayPal.TransactionCode, PayPal.InvoiceIdentifier, PayPal.TransactionId
                                      };
                var bad = from c in checkNames where string.IsNullOrWhiteSpace(ipnRequest.Form[c]) select c;
                if (bad.Any())
                {
                    _log.FatalFormat("Bad payment notification, missing arguments: {0}", string.Join(",", bad));
                    return;
                }


                trx = new PaymentTransaction
                          {
                              Id = txnId,
                              Item = itemName,
                              Currency = "USD",
                              PaymentStatus = "x",
                              Principal = trxInfo,
                              ReceiverEmail = receiverEmail,
                              PayerEmail = payerEmail,
                              Response = "VERIFIED",
                              TransactionTime = DateTime.UtcNow,
                              TransactionCode = transactionCode
                          };

                //Post back to either sandbox or live
                //string strSandbox = "https://www.sandbox.paypal.com/cgi-bin/webscr";
                //string strLive = "https://www.paypal.com/cgi-bin/webscr";
                string postback = _config[PayPalConfiguration.PostUrl];

                HttpWebRequest req = (HttpWebRequest) WebRequest.Create(postback);

                //Set values for the request back
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                byte[] param = ipnRequest.BinaryRead(ipnRequest.ContentLength);
                string strRequest = Encoding.ASCII.GetString(param);
                strRequest += "&cmd=_notify-validate";
                req.ContentLength = strRequest.Length;

                //Send the request to PayPal and get the response
                StreamWriter streamOut = new StreamWriter(req.GetRequestStream(), Encoding.ASCII);
                streamOut.Write(strRequest);
                streamOut.Close();
                StreamReader streamIn = new StreamReader(req.GetResponse().GetResponseStream());
                string strResponse = streamIn.ReadToEnd();
                streamIn.Close();

                if (strResponse == PayPal.Verified)
                {
                    //check that receiver_email is your Primary PayPal email
                    string receiverEmailVerify = _config[PayPalConfiguration.ReceiveEmail];
                    if (string.IsNullOrEmpty(receiverEmail) || receiverEmail != receiverEmailVerify)
                        throw new IPNGatewayException(string.Format("Bad IPN receiver.", receiverEmail));

                    // check that txn_id has not been previously processed
                    using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                    {
                        var prev = ds.Load<PaymentTransaction>(txnId);
                        if (prev != null)
                        {
                            trx.Id = string.Format("dup: {0} @{1}", txnId, Guid.NewGuid());
                            throw new IPNGatewayException("Duplicate completed transaction received.");
                        }
                    }

                    // process payment
                    _accountBroker.ProcessPaymentNotification(trxInfo, invoice, transactionCode, payerEmail,
                                                              SubscriptionRole, UnsubscribedRole,
                                                              EmailTemplateSubscriptionAuthCode,
                                                              EmailTemplateSubscriptionSignUp,
                                                              EmailTemplateSubscriptionFail, EmailTemplateShare);
                }
                else if (strResponse == PayPal.Invalid)
                {
                    trx.Response = PayPal.Invalid;

                    throw new IPNGatewayException("Invalid IPN notification");
                }
            }
            catch (Exception ex)
            {
                _log.Error("IPN notification not verified!");
                _log.Error(ex.Message);
                _log.Error(ex.TraceInformation());
            }
            finally
            {
                if (trx != null && !string.IsNullOrEmpty(trx.PaymentStatus) && trx.PaymentStatus == "Completed")
                {
                    try
                    {
                        using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                        {
                            ds.Store(trx);
                            ds.SaveChanges();
                        }
                    }
                    catch
                    {
                    }
                }
            } // finally
        }
    }
}