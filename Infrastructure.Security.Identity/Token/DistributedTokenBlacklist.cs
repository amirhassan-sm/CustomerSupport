using Application.Contracts.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Security.Identity.Token
{
    public class DistributedTokenBlacklist : ITokenBlacklist
    {
        private const string KeyPrefix = "auth:revoked:";

        private readonly IDistributedCache cache;
        private readonly ILogger<DistributedTokenBlacklist> logger;

        public DistributedTokenBlacklist(
            IDistributedCache cache,
            ILogger<DistributedTokenBlacklist> logger)
        {
            this.cache = cache;
            this.logger = logger;
        }

        public async Task RevokeAsync(string jti, TimeSpan timeToLive)
        {
            if (string.IsNullOrWhiteSpace(jti) || timeToLive <= TimeSpan.Zero)
                return;

            try
            {
                await cache.SetStringAsync(
                    KeyPrefix + jti,
                    "1",
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = timeToLive
                    });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to revoke token {Jti}.", jti);
            }
        }

        public async Task<bool> IsRevokedAsync(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti))
                return false;

            try
            {
                var value = await cache.GetStringAsync(KeyPrefix + jti);
                return value is not null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to check token {Jti} against the blacklist.", jti);
                return false;
            }
        }
    }
}
