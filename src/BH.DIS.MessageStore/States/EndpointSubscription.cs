﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BH.DIS.MessageStore.States
{
    public class EndpointSubscription
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "notificationSeverity")]
        public string NotificationSeverity { get; set; }
        [JsonProperty(PropertyName = "mail")]
        public string Mail { get; set; }
        [JsonProperty(PropertyName = "endpointId")]
        public string EndpointId { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "authorId")]
        public string AuthorId { get; set; }
        [JsonProperty(PropertyName = "notifiedAt")]
        public string NotifiedAt { get; set; }
        [JsonProperty(PropertyName = "errorList")]
        public string ErrorList { get; set; }
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
        [JsonProperty(PropertyName = "eventTypes")]
        public List<string> EventTypes { get; set; }
        [JsonProperty(PropertyName = "payload")]
        public string Payload { get; set; }
        [JsonProperty(PropertyName = "frequency")]
        public int Frequency { get; set; }

    }
}
