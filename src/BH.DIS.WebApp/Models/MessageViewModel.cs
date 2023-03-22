using BH.DIS.MessageStore;
using BH.DIS.WebApp.Services.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BH.DIS.WebApp.Models
{
    public class MessageViewModel
    {
        public MessageEntity FailedMessage { get; set; }
        public MessageEntity OriginatingMessage { get; set; }
        public IEnumerable<LogEntry> LogEntries { get; set; }
        public IEnumerable<MessageEntity> Actions { get; set; }
    }
}
