using BH.DIS.Core.Endpoints;
using BH.DIS.Events.Demo;

namespace BH.DIS.Endpoints.Demo
{
    public class Charlie : Endpoint
    {
        public Charlie()
        {
            Consumes<AliceSaidHello>();
            Consumes<AliceSaidHelloWithRetry>();
        }
    }
}
