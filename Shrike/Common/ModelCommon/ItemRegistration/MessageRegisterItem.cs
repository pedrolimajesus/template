using System;

namespace Lok.Unik.ModelCommon.ItemRegistration
{
    /// <summary>
    /// Used on client side where the item wants to register on the server
    /// </summary>
    public class MessageRegisterItem : IDateTimeRegitration
    {
        public MessageRegisterItem()
        {
            this.RegisterDate = DateTime.UtcNow;
        }

        public string Server { get; set; }

        public ItemRegistrationResult Result { get; set; }

        #region IDateTimeRegitration Members

        public DateTime RegisterDate { get; private set; }

        #endregion
    }
}