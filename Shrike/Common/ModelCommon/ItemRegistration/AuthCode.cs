namespace Lok.Unik.ModelCommon.ItemRegistration
{
    using System;

    using AppComponents;

    /// <summary>
    /// This is an authorization code used for registering items
    /// </summary>
    public class AuthCode : AuthorizationCode
    {
        public AuthCode()
        {
            
        }

        public Guid AuthCodeId { get; set; }
    }
}
