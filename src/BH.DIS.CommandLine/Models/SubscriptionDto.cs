﻿namespace BH.DIS.CommandLine.Models;

public class SubscriptionDto
{
    public string Name { get; set; }
    public string TopicName { get; set; }
    public List<RuleDto> Rules { get; set; }
    public bool IsDeprecated { get; set; }
        
    public class SubscriptionDtoComparer : IEqualityComparer<SubscriptionDto>
    {
        public int GetHashCode(SubscriptionDto co)
        {
            if (co == null)
            {
                return 0;
            }
            return co.Name.GetHashCode();
        }

        public bool Equals(SubscriptionDto x1, SubscriptionDto x2)
        {
            if (object.ReferenceEquals(x1, x2))
            {
                return true;
            }
            if (object.ReferenceEquals(x1, null) ||
                object.ReferenceEquals(x2, null))
            {
                return false;
            }
            return x1.Name == x2.Name && x1.TopicName == x2.TopicName;
        }
    }
}
