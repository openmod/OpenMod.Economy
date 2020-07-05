using System;
using System.Text.Json.Serialization;
using LiteDB;
using OpenMod.Economy.API;

namespace OpenMod.Economy.Core
{
    public class AccountId : IAccountId
    {
        private string m_OwnerType;
        private string m_OwnerId;


        [BsonId]
        public string UniqueId
        {
            get
            {
                return $"{m_OwnerType}_{m_OwnerId}";
            }
            set
            {
                var values = value.Split('_');
                OwnerType = values[0];
                OwnerId = values[1];
            }
        }

        [BsonIgnore, JsonIgnore]
        public string OwnerType
        {
            get { return m_OwnerType; }
            set { m_OwnerType = value?.ToLower(); }
        }

        [BsonIgnore, JsonIgnore]
        public string OwnerId
        {
            get { return m_OwnerId; }
            set { m_OwnerId = value?.ToLower(); }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is IAccountId accountId))
                return false;


            var flag1 = m_OwnerId.Equals(accountId.OwnerId, StringComparison.OrdinalIgnoreCase);
            var flag2 = m_OwnerType.Equals(accountId.OwnerType, StringComparison.OrdinalIgnoreCase);
            return flag1 && flag2;
        }

        public override int GetHashCode()
        {
            return UniqueId.GetHashCode();
        }
    }
}
