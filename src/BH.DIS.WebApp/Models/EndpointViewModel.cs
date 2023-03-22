using BH.DIS.Core.Endpoints;
using BH.DIS.Core.Messages;
using BH.DIS.MessageStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BH.DIS.WebApp.Models
{
    public class EndpointViewModel
    {
        public IEndpoint Endpoint { get; set; }
        public EndpointState EndpointState { get; set; }
        public Dictionary<string, MessageContent> OriginatingMessageContents { get; set; }
        public IEnumerable<MessageEntity> FailedEvents { get; set; }
    }
}
