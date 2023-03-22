using System.Collections.Generic;
using System.Linq;

namespace BH.DIS.Core.Messages
{
    public class SessionState
    {
        public SessionState()
        {
            DeferredSequenceNumbers = new List<long>();
        }

        public List<long> DeferredSequenceNumbers { get; set; }

        public string BlockedByEventId { get; set; }

        public bool IsEmpty() =>
            BlockedByEventId == null &&
            !DeferredSequenceNumbers.Any();
    }
}