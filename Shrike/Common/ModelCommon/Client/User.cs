namespace Lok.Unik.ModelCommon.Client
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    using AppComponents.Web;

    using Interfaces;

    using Raven.Imports.Newtonsoft.Json;

    public class User : ITaggableEntity
    {
        public Guid Id { get; set; }

        #region Implementation of ITaggableEntity

        private IList<Tag> _tags;

        public IList<Tag> Tags
        {
            get
            {
                return _tags ?? (_tags = new List<Tag>());
            }
            set
            {
                _tags = value;
            }
        }

        #endregion

        #region ApplicationUser Association

        public string AppUserId { get; set; }

        [JsonIgnore]
        [XmlIgnore]
        private ApplicationUser _appUser;

        [JsonIgnore]
        [XmlIgnore]
        public ApplicationUser AppUser
        {
            get
            {
                return _appUser;
            }
            set
            {
                _appUser = value;
                if (_appUser != null)
                {
                    AppUserId = _appUser.PrincipalId; // string.Format("ApplicationUsers/{0}", appUser.Id);
                }
            }
        }

        #endregion

        #region Invitation Association

        [JsonIgnore]
        [XmlIgnore]
        public Invitation Invitation { get; set; }

        public string InvitationId { get; set; }

        #endregion

        #region Filters

        //Filters are collection of tags that represent groups of devices, apps, content, schedules that the user can see
        public IList<Tag> DeviceFilter { get; set; }

        public IList<Tag> ApplicationFilter { get; set; }

        public IList<Tag> ContentFilter { get; set; }

        public IList<Tag> ScheduleFilter { get; set; }

        #endregion

        #region Assignments

        //should look over the GroupLink documents on raven db
        public IList<Tag> AdminOverDevices
        {
            get
            {
                return null;
            }
        }

        #endregion


    }
}