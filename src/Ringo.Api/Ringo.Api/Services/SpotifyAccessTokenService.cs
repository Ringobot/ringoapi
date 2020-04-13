using Microsoft.Extensions.Logging;
using Ringo.Api.Data;
using Ringo.Api.Models;
using Scale;
using SpotifyApi.NetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public class SpotifyAccessTokenService : IAccessTokenService
    {
        private readonly ICache _cache;
        private readonly ICosmosData<UserAccessToken> _data;
        private readonly IUserAccountsService _userAccounts;
        private readonly ILogger<SpotifyAccessTokenService> _logger;

        public SpotifyAccessTokenService(
            ICache cache,
            ICosmosData<UserAccessToken> data,
            IUserAccountsService userAccounts,
            ILogger<SpotifyAccessTokenService> logger)
        {
            _cache = cache;
            _data = data;
            _userAccounts = userAccounts;
            _logger = logger;
        }

        public async Task<string> GetAccessToken(string userId)
        {
            return await RetryHelper.RetryAsync(async () =>
            {
                // get User Access Tokens from Cache
                var cachedToken = await _cache.Get<UserAccessToken>(Key(userId));

                // Return cached value if hit and not expired
                if (cachedToken != null && !cachedToken.AccessTokenHasExpired) return cachedToken.Tokens.AccessToken;

                // Get token from storage
                var storedToken = await _data.Get(UserAccessToken.CanonicalId(userId), userId);
                if (storedToken.AccessTokenHasExpired)
                {
                    // If token has expired, refresh the token
                    var newToken = await _userAccounts.RefreshUserAccessToken(storedToken.Tokens.RefreshToken);
                    storedToken.ResetAccessToken(newToken);

                    // Save User Access Tokens to storage
                    await _data.Replace(storedToken, storedToken.ETag);
                }

                // Store User Access Tokens in Cache. Set Cache item expiry for when the token is due to expire
                await _cache.Set(Key(userId), storedToken, storedToken.AccessTokenExpiresBefore.Subtract(DateTimeOffset.UtcNow));
                return storedToken.Tokens.AccessToken;
            }, waitMs: 10, logger: _logger);
        }

        public async Task<bool> HasAccessToken(string userId) =>
            await _data.GetOrDefault(UserAccessToken.CanonicalId(userId), userId) != null;

        public async Task SetAccessToken(string userId, BearerAccessRefreshToken token)
        {
            await RetryHelper.RetryAsync(async () =>
            {
                var storedToken = await _data.GetOrDefault(UserAccessToken.CanonicalId(userId), userId);

                if (storedToken == null)
                {
                    var uat = new UserAccessToken(userId, token);
                    uat.EnforceInvariants();
                    await _data.Create(uat);
                }
                else
                {
                    storedToken.Tokens = token;
                    storedToken.EnforceInvariants();
                    await _data.Replace(storedToken, storedToken.ETag);
                }
            }, waitMs: 10, logger: _logger);
        }

        private string Key(string userId) => $"{nameof(UserAccessToken)}/{userId}";
    }
}
