using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppComponents.Web.Authentication
{
    using System.Web;

    using log4net;

    public class InvitationAuthCode : AuthorizationCode
    {
        private const string IdKey = "id";

        private const string InvitingTenancyKey = "it";

        private string code;

        private static readonly ILog _log = ClassLogger.Create(typeof(InvitationAuthCode));

        public override string Code
        {
            get
            {
                return code ?? (code = GenerateInvitationCode());
            }
            set
            {
                code = value;
            }
        }

        public string InvitingTenancy { get; set; }

        private string GenerateInvitationCode()
        {
            var plainText = string.Format(
                "{0}={1}&{2}={3}", IdKey, GuidEncoder.Encode(Guid.NewGuid()), InvitingTenancyKey, InvitingTenancy);
            return GenerateCode(plainText);
        }

        public static string GetInvitingTenancyFromCode(string code)
        {
            try
            {
                var plain = DecryptCode(code);
                var dico = HttpUtility.ParseQueryString(plain);

                var goodGuid = GuidEncoder.Decode(dico[IdKey]);
                return Guid.Empty == goodGuid ? null : dico[InvitingTenancyKey];
            }
            catch (Exception exception)
            {
                _log.Error("GetInvitingTenancyFromCode", exception);
                _log.ErrorFormat("Inviting Authcode authorization failed for {0}", code);
                return null;
            }
        }
    }
}