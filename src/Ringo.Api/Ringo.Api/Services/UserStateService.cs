using Ringo.Api.Data;
using Ringo.Api.Models;
using System;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public class UserStateService : IUserStateService
    {
        private readonly ICosmosData<UserState> _data;

        public UserStateService(ICosmosData<UserState> data)
        {
            _data = data;
        }

        public async Task<string> NewState(string userId)
        {
            // Store the state
            string state = Guid.NewGuid().ToString("N");
            await _data.Create(new UserState(userId, state));
            return state;
        }

        public async Task ValidateState(string state, string userId)
        {
            var userState = await _data.GetOrDefault(state, state);
            if (userState == null) throw new InvalidOperationException("Invalid State value");
            if (userState.UserId != userId) throw new InvalidOperationException("Invalid State value");
        }
    }
}
