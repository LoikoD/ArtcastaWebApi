using ArtcastaWebApi.Models;

namespace ArtcastaWebApi.Services
{
    public interface IAccessPointsService
    {
        List<AccessPoint> GetAllAccessPoints();
        int GetAccessPointIdByName(string name);
    }
}
