using System;

namespace AlloyTicketClient.Models
{
    public class RuleKey
    {
        public Guid RuleId { get; set; }
        public string ObjectId { get; set; } = string.Empty;

        public RuleKey() { }
        public RuleKey(Guid ruleId, string objectId)
        {
            RuleId = ruleId;
            ObjectId = objectId;
        }
        public override bool Equals(object? obj)
        {
            return obj is RuleKey key &&
                   RuleId.Equals(key.RuleId) &&
                   ObjectId == key.ObjectId;
        }
        public override int GetHashCode() => HashCode.Combine(RuleId, ObjectId);
    }
}
