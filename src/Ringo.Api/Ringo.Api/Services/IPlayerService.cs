using Ringo.Api.Models;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public interface IPlayerService
    {
        Task<Player> GetPlayer(string stationId, string userId);

        Task<Player> SetPlayer(string stationId, string userId, NowPlaying np);
    }
}
