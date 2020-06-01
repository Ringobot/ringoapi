using Microsoft.Extensions.Logging;
using Ringo.Api.Data;
using Ringo.Api.Models;
using Scale;
using SpotifyApi.NetCore.Authorization;
using System;
using System.Security;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public class AccessTokenService : IAccessTokenService
    {
        private readonly ICache _cache;
        private readonly ICosmosData<UserAccessRefreshToken> _data;
        private readonly IUserAccountsService _userAccounts;
        private readonly ILogger<AccessTokenService> _logger;

        public AccessTokenService(
            ICache cache,
            ICosmosData<UserAccessRefreshToken> data,
            IUserAccountsService userAccounts,
            ILogger<AccessTokenService> logger)
        {
            _cache = cache;
            _data = data;
            _userAccounts = userAccounts;
            _logger = logger;
        }

        public async Task<RingoUserAccessToken> GetRingoAccessToken(string userId)
        {
            var spotifyToken = await GetSpotifyAccessToken(userId);
            if (spotifyToken == null) return null;
            return new RingoUserAccessToken(spotifyToken);
        }

        public async Task<UserAccessToken> GetSpotifyAccessToken(string userId)
        {
            return await RetryHelper.RetryAsync(async () =>
            {
                // get User Access Tokens from Cache
                var cachedToken = await _cache.Get<UserAccessToken>(Key(userId));

                // Return cached token if hit
                if (cachedToken != null) return cachedToken;

                // Get token from storage
                var storedToken = await _data.GetOrDefault(UserAccessToken.CanonicalId(userId), userId);

                if (storedToken == null) return null;

                // Store AccessToken (only) in Cache. 
                // Only store tokens that have not yet expired
                // Set Cache item expiry for when the token is due to expire.
                // DO NOT cache Refresh Tokens
                var expiry = storedToken.Expires.Subtract(DateTimeOffset.UtcNow);

                if (expiry.TotalMilliseconds > 0)
                {
                    await _cache.Set(
                        Key(userId),
                        new UserAccessToken(storedToken),
                        storedToken.Expires.Subtract(DateTimeOffset.UtcNow));
                }

                return storedToken;

            }, waitMs: 10, logger: _logger);
        }

        public async Task<UserAccessToken> GetSpotifyAccessToken(string userId, bool refreshIfExpired)
        {
            var token = await GetSpotifyAccessToken(userId);
            if (!refreshIfExpired || !token.AccessTokenExpired) return token;

            return await RefreshSpotifyAccessToken(userId);
        }

        public async Task<UserAccessToken> RefreshSpotifyAccessToken(string userId)
        {
            var refreshToken = await _data.GetOrDefault(UserAccessToken.CanonicalId(userId), userId);

            var now = DateTimeOffset.UtcNow;
            var newToken = await _userAccounts.RefreshUserAccessToken(refreshToken.RefreshToken);
            refreshToken.ResetAccessToken(newToken, now);

            // Save User Access Tokens to storage
            await _data.Replace(refreshToken, refreshToken.ETag);

            // Store AccessToken (only) in Cache. Set Cache item expiry for when the token is due to expire.
            // DO NOT cache Refresh Tokens
            var userAccessToken = new UserAccessToken(refreshToken);

            await _cache.Set(
                Key(userId),
                userAccessToken,
                userAccessToken.Expires.Subtract(DateTimeOffset.UtcNow));

            return userAccessToken;
        }

        public async Task<RingoUserAccessToken> RefreshRingoAccessToken(string userId, string bearerToken)
        {
            var refreshToken = await _data.GetOrDefault(UserAccessToken.CanonicalId(userId), userId);
            if (bearerToken != CryptoHelper.Sha256(refreshToken.AccessToken)) throw new SecurityException("Not authorized");

            return new RingoUserAccessToken(await RefreshSpotifyAccessToken(userId));
        }

        public async Task<bool> HasRingoAccessToken(string userId) => await GetRingoAccessToken(userId) != null;

        public async Task SetSpotifyAccessToken(string userId, BearerAccessRefreshToken token)
        {
            await RetryHelper.RetryAsync(async () =>
            {
                var storedToken = await _data.GetOrDefault(UserAccessToken.CanonicalId(userId), userId);
                var uat = new UserAccessRefreshToken(userId, token);
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
