using System.Threading.Tasks;

namespace BH.DIS.WebApp.Services
{
    public interface IRenderService
    {
        Task<string> RenderAsync(string url, object props);
    }
}