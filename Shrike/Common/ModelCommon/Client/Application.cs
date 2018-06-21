///////////////////////////////////////////////////////////
//  BaseApp.cs
//  Implementation of the Class Application
//  Generated by Enterprise Architect
//  Created on:      14-Sep-2012 14:41:49
///////////////////////////////////////////////////////////

namespace Lok.Unik.ModelCommon.Client
{
    using System;
    using System.Collections.Generic;

    using Lok.Unik.ModelCommon.Interfaces;

    public class Application : IApplication
    {
        public Application()
        {
            Id = Guid.NewGuid();
            Tags = new List<Tag>();
            TransferInfo = new BitTorrentInfo();
        }

        #region Application Comparison

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Application other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            return ReferenceEquals(this, other) || Equals(other.Id, this.Id);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> Tag is equal to the current <see cref="T:System.Object"/> Tag.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> tag is equal to the current <see cref="T:System.Object"/> tag; otherwise, false.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> tag to compare with the current <see cref="T:System.Object"/> tag. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return obj.GetType() == typeof(Application) && this.Equals(obj);
        }

        /// <summary>
        /// Serves as a hash function for Tag type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/> tag.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked
            {
                var result = this.Id.GetHashCode();
                result = (result * 397) ^ (this.Name != null ? this.Name.GetHashCode() : 0);
                return result;
            }
        }

        public static bool operator ==(Application left, Application right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Application left, Application right)
        {
            return !Equals(left, right);
        }

        #endregion

        #region Implementation of IApplication

        public Guid Id { get; set; }

        public string Name { get; set; }

        

        #endregion

        #region Implementation of ITaggableEntity

        public IList<Tag> Tags { get; set; }

        #endregion


        public BitTorrentInfo TransferInfo { get; set; }

        public string IconPath { get; set; }

        public string Version { get; set; }

        public string ManagedAppController { get; set; }

        public string RelativeExePath { get; set; }

        public string RelativeConfigPath { get; set; }

        public string InstallationPath { get; set; }

        public double DeploymentFileSize { get; set; }

        public string AdditionalArgs { get; set; }

        public string ControllerArgs { get; set; }

        public string ContentIndex { get; set; }
    }

    //end BaseApp
}

//end namespace System