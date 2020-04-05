using Ringo.Api.Models;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public interface IStationService
    {
        Task<StationServiceResult> Start(User user, string stationId);
        Task<StationServiceResult> Join(User user, string stationId);
    }
}