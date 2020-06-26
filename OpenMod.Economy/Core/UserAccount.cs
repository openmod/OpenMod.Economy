#region

using LiteDB;

#endregion

namespace OpenMod.Economy.Core
{
    public class UserAccount
    {
        [BsonId] public string UniqueId;

        public decimal Balance { get; set; }
    }
}