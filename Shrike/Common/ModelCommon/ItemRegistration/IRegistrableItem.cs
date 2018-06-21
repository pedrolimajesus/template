namespace Lok.Unik.ModelCommon.ItemRegistration
{
    using System;

    using Lok.Unik.ModelCommon.Interfaces;

    /// <summary>
    /// A registrable item has tags and an Id
    /// </summary>
    public interface IRegistrableItem : ITaggableEntity
    {
        //Id must be a GUID
        //Guid Id { get; set; }

        DateTime TimeRegistered { get; set; }
    }
}
