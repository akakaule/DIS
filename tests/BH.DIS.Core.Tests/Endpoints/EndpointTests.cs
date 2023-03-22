using BH.DIS.Core.Events;
using System;
using Xunit;

namespace BH.DIS.Core.Endpoints
{
    public class EndpointTests
    {
        class EmptyEndpoint : Endpoint { }

        [Fact]
        public void Id_Always_ReturnsNameOfType()
        {
            var endpoint = new EmptyEndpoint();
            Assert.Equal(typeof(EmptyEndpoint).Name, endpoint.Id);
        }

        [Fact]
        public void Name_Always_ReturnsNameOfType()
        {
            var endpoint = new EmptyEndpoint();
            Assert.Equal(typeof(EmptyEndpoint).Name, endpoint.Name);
        }

        [Fact]
        public void Consumes_WhenCalledMultipleTimesWithSameEventType_Throws()
        {
            Assert.ThrowsAny<Exception>(() => new MyConsumingEndpoint());
        }

        class EmptyEvent : Event { }

        class MyConsumingEndpoint : Endpoint
        {
            public MyConsumingEndpoint()
            {
                Consumes<EmptyEvent>();
                Consumes<EmptyEvent>();
            }
        }

        [Fact]
        public void Produces_WhenCalledMultipleTimesWithSameEventType_Throws()
        {
            Assert.ThrowsAny<Exception>(() => new MyProducingEndpoint());
        }

        private class MyProducingEndpoint : Endpoint
        {
            public MyProducingEndpoint()
            {
                Produces<EmptyEvent>();
                Produces<EmptyEvent>();
            }
        }
    }
}
