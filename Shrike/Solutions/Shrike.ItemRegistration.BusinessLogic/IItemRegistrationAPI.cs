namespace Shrike.ItemRegistration.BusinessLogic
{
    using Lok.Unik.ModelCommon.ItemRegistration;

    public interface IItemRegistrationAPI
    {
        ItemRegistrationResult RegisterItem<TItem> (string registrationCode, string description = null);
    }
}
