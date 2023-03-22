using System;
using Xunit;

namespace BH.DIS.Core.Endpoints
{
    public class PlatformTests
    {
        class EmptyEndpoint : Endpoint { }

        class MyPlatform : Platform
        {
            public MyPlatform()
            {
                AddEndpoint(new EmptyEndpoint());
            }
        }

        [Fact]
        public void Endpoints_WhenEndpointAdded_ContainsIt()
        {
            IPlatform platform = new MyPlatform();
            string endpointId = new EmptyEndpoint().Id;

            Assert.Contains(platform.Endpoints, ep => ep.Id == endpointId);
        }

        [Fact]
        public void Add_WhenSameEndpointAddedMultipleTimes_Throws()
        {
            Assert.ThrowsAny<Exception>(() => new MyBadPlatform());
        }

        private class MyBadPlatform : Platform
        {
            public MyBadPlatform()
            {
                var endpoint = new EmptyEndpoint();
                AddEndpoint(endpoint);
                AddEndpoint(endpoint);
            }
        }
    }
}
