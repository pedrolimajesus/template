namespace Lok.Unik.ModelCommon.ItemRegistration
{
    using System;

    using Lok.Unik.ModelCommon.Client;

    public class ItemRegistrationResult 
    {
        public ResultCode Result { get; set; }
        public string AuthCode { get; set; }
        public Guid ItemId { get; set; }
        public Guid PassCodeId { get; set; }
        public string PassCodeName { get; set; }
        public string Description { get; set; }
    }
}
