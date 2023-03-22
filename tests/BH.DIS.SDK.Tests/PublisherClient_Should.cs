using System;
using System.Linq;
using BH.DIS.Core.Events;
using Xunit;

namespace BH.DIS.SDK.Tests
{
    public class PublisherClient_Should
    {
        //[Fact]
        //public void Return_Batches()
        //{
        //    var events = Enumerable.Range(0, 10000).Select(i => new VesselRequestUpdated()
        //    {
        //        RequestId = $"{i}-{Guid.NewGuid()}",
        //        RequestState = RequestState.Pending
        //    }).Cast<IEvent>();

        //    var sut = new PublisherClient(new Azure.Messaging.ServiceBus.ServiceBusClient(""), "");

        //    var batches = sut.GetBatches(events.ToList()).ToList();

        //    Assert.Equal(31, batches.Count);
        //    Assert.Equal(10000, batches.Sum(b => b.Count()));
        //}
    }
}
