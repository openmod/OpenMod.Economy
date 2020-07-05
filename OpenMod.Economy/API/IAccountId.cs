namespace OpenMod.Economy.API
{
    public interface IAccountId
    {
        string OwnerType { get; }
        string OwnerId { get; }
    }
}