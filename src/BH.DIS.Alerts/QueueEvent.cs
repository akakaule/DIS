using BH.DIS.MessageStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.DIS.Alerts
{
    public class QueueEvent
    {
        public string id { get; set; }
        public string subject { get; set; }
        public string data { get; set; }
        public string eventType { get; set; }
        public string dataVersion { get; set; }
        public string metadataVersion { get; set; }
        public string eventTime { get; set; }
        public string topic { get; set; }
    }
}
