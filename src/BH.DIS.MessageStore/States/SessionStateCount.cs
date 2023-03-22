using System;
using System.Collections.Generic;
using System.Text;

namespace BH.DIS.MessageStore.States
{
    public class SessionStateCount
    {
        public string SessionId { get; set; }
        public IEnumerable<string> PendingEvents { get; set; }
        public IEnumerable<string> DeferredEvents { get; set; }
    }
}
