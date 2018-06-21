namespace Lok.Unik.ModelCommon.ItemRegistration
{
    using System;
    using System.Collections.Generic;

    using Client;
    using System.ComponentModel.DataAnnotations;

    public static class ItemRegistrationType
    {
        public const string Kiosk = "Kiosk";

        public const string ProxyAgent = "Proxy Agent";

        public const string LookDevice = "LOOK Device";

        public const string MoveDevice = "MOVE Device";

        public static string[] GetProperties()
        {
            return new[]
                       {
                           Kiosk,
                           ProxyAgent,
                           LookDevice,
                           MoveDevice
                       };
        }

    }

    /// <summary>
    /// Stores the information about the registration of an IRegistrableItem
    /// </summary>
    public class ItemRegistration : IRegistrableItem
    {
        #region Implementation of ITaggableEntity

        private IList<Tag> tags;

        public virtual IList<Tag> Tags
        {
            get
            {
                return this.tags ?? (this.tags = new List<Tag>());
            }
            set
            {
                this.tags = value;
            }
        }

        #endregion

        #region Implementation of IRegistrableItem

        public virtual Guid Id { get; set; }

        public virtual DateTime TimeRegistered { get; set; }

        #endregion

        [Required]
        public virtual string PassCode { get; set; }

        [Required]
        public virtual string Name { get; set; }

        public virtual string TenancyId { get; set; }

        public string FacilityId { get; set; }

        public string Type { get; set; }

    }
}
