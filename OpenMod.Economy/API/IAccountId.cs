namespace OpenMod.Economy.API
{
    public interface IAccountId
    {
        string UniqueId { get; }
        string OwnerType { get; }
        string OwnerId { get; }
    }
}
