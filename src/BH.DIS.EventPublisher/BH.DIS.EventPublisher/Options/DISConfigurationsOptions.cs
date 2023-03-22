using System;
using System.Collections.Generic;
using System.Text;

namespace BH.DIS.EventPublisher.Options
{
    class DISConfigurationsOptions
    {
        public string Key { get; set; }
        public string ResourceId { get; set; }
        public string TopicName { get; set; }
        public string ServiceBusConnection{ get; set;}   
    }
}
