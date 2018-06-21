using System;

namespace Lok.Unik.ModelCommon.Client
{
    /// <summary>
    /// Associates two tags together, representing
    /// a link between two sets of tagged entities
    /// </summary>
    public class GroupLink
    {
        #region Metadata
        /// <summary>
        /// Identifier
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Association / link name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Date created
        /// </summary>
        public DateTime CreateDate { get; set; }

        /// <summary>
        /// principal who created this link
        /// </summary>
        public string CreatorPrincipalId { get; set; }

        #endregion

        #region GroupAssociation
        /// <summary>
        /// First tag in the association
        /// </summary>
        public Tag GroupOne { get; set; }

        /// <summary>
        /// second tag in the association
        /// </summary>
        public Tag GroupTwo { get; set; }

        #endregion
    }
}