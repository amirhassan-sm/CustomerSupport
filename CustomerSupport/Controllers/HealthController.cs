using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace CustomerSupport.Controllers
{
    [Route("health")]
    [ApiController]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromServices] IDistributedCache cache,
            [FromServices] IConfiguration config)
        {
            const string key = "health:ping";
            var redis = config.GetConnectionString("Redis");
            var provider = string.IsNullOrWhiteSpace(redis) ? "memory" : "redis";

            await cache.SetStringAsync(
                key,
                "ok",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                });

            var value = await cache.GetStringAsync(key);

            return Ok(new
            {
                status = "ok",
                cache = provider,
                cacheWriteRead = value == "ok"
            });
        }
    }
}
