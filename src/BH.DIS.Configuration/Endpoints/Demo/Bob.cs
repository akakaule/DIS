using BH.DIS.Core.Endpoints;
using BH.DIS.Events.Demo;

namespace BH.DIS.Endpoints.Demo
{
    public class Bob : Endpoint
    {
        public Bob()
        {
            Consumes<AliceSaidHello>();
            Consumes<AliceSaidHelloWithRetry>();
            Consumes<AliceSaidBonjour>();            
        }
    }
}
