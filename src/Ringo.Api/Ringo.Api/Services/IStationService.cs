using Ringo.Api.Models;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public interface IStationService
    {
        Task<StationServiceResult> Start(string userId, string stationId);
        Task<StationServiceResult> Join(string userId, string stationId);
        Task<StationServiceResult> ChangeOwner(string userId, string stationId);
        Task<StationServiceResult> CreateStation(CreateStation station);
    }
}