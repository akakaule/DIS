using BH.DIS.Core.Endpoints;
using BH.DIS.Events.Demo;

namespace BH.DIS.Endpoints.Demo
{
    public class Alice : Endpoint
    {
        public Alice()
        {
            Produces<AliceSaidHello>();
            Produces<AliceSaidHelloWithRetry>();
            Produces<AliceSaidBonjour>();
        }
    }
}
