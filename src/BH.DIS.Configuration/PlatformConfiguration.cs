using BH.DIS.Core;
using BH.DIS.Endpoints.Demo;


namespace BH.DIS
{
    public class PlatformConfiguration : Platform
    {
        public PlatformConfiguration()
        {
            AddEndpoint(new Alice());
            AddEndpoint(new Bob());
            AddEndpoint(new Charlie());
        }
    }
}
