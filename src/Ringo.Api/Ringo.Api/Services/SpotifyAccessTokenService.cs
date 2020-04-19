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
                string cachedToken = await _cache.Get<string>(Key(userId));

                // Return cached token if hit
                if (cachedToken != null) return cachedToken;

                // Get token from storage
                var storedToken = await _data.GetOrDefault(UserAccessToken.CanonicalId(userId), userId);

                if (storedToken == null) return null;

                if (storedToken.AccessTokenExpired)
                {
                    // If token has expired, refresh the token
                    var now = DateTimeOffset.UtcNow;
                    var newToken = await _userAccounts.RefreshUserAccessToken(storedToken.RefreshToken);
                    storedToken.ResetAccessToken(newToken, now);

                    // Save User Access Tokens to storage
                    await _data.Replace(storedToken, storedToken.ETag);
                }

                // Store AccessToken (only) in Cache. Set Cache item expiry for when the token is due to expire.
                // DO NOT cache Refresh Tokens
                await _cache.Set(
                    Key(userId),
                    storedToken.AccessToken,
                    storedToken.Expires.Subtract(DateTimeOffset.UtcNow));

                return storedToken.AccessToken;

            }, waitMs: 10, logger: _logger);
        }

        public async Task<bool> HasAccessToken(string userId) => await GetAccessToken(userId) != null;

        public async Task SetAccessToken(string userId, BearerAccessRefreshToken token)
        {
            await RetryHelper.RetryAsync(async () =>
            {
                var storedToken = await _data.GetOrDefault(UserAccessToken.CanonicalId(userId), userId);
                var uat = new UserAccessToken(userId, token);
                uat.EnforceInvariants();

                if (storedToken == null)
                {
                    await _data.Create(uat);
                }
                else
                {
                    await _data.Replace(uat, storedToken.ETag);
                }
            }, waitMs: 10, logger: _logger);
        }

        private string Key(string userId) => $"{nameof(UserAccessToken)}/{userId}";
    }
}
