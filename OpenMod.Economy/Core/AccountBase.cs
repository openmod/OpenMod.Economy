using System;
using LiteDB;

namespace OpenMod.Economy.Core
{
    public class AccountBase
    {
        [BsonId]
        public string UniqueId
        {
            get => $"{m_OwnerType}_{m_OwnerId}";
            set
            {
                var values = value.Split('_');
                if (values.Length != 2)
                    throw new Exception($"Invalid UniqueId: {UniqueId}");

                m_OwnerType = values[0].ToLower();
                m_OwnerId = values[1].ToLower();
            }
        }

        [BsonIgnore] 
        private string m_OwnerType;

        [BsonIgnore]
        private string m_OwnerId;

        public decimal Balance { get; set; }
    }
}