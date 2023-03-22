using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace BH.DIS.EventPublisher.Models
{
    public class EventPublisherModel
    {
        [JsonProperty(PropertyName = "TopicName")]
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string TopicName { get; set; }

        [JsonProperty(PropertyName = "EventType")]
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string EventType { get; set; }

        [JsonProperty(PropertyName = "EventMessage")]
        [Required]
        public dynamic EventMessage { get; set; }

        [JsonProperty(PropertyName = "SessionId")]
        public string SessionId { get; set; }

        [JsonProperty(PropertyName = "CorrelationId")]
        public string CorrelationId { get; set; }

        public Type GetEventType()
        {
            return Type.GetType(EventType);
        }
    }
}
