using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using AppComponents.Extensions.StringEx;

namespace AppComponents.ControlFlow
{
    public class AuthCodePrincipalContextProvider : IContextProvider
    {
        #region IContextProvider Members

        public IEnumerable<Uri> ProvideContexts()
        {
            var ctx = HttpContext.Current;
            if (null == ctx)
            {
                yield break;
            }

            var pr = (AuthorizationCode)HttpContext.Current.Items["AuthorizationCode"];

            if (null == pr)
            {
                yield break;
            }

            yield return new Uri(string.Format("context://AuthorizationCode/{0}", pr.Code.ToAscBase64()));

            if (!string.IsNullOrEmpty(pr.Tenant))
            {
                yield return new Uri(string.Format("context://Tenancy/{0}", pr.Tenant));
            }

            if (!string.IsNullOrEmpty(pr.Principal))
            {
                yield return new Uri(string.Format("context://Principal/{0}", pr.Principal));
            }
            else if (!string.IsNullOrEmpty(pr.Referent))
            {
                yield return new Uri(string.Format("context://Principal/{0}", pr.Referent));
            }
        }

        #endregion
    }

}
