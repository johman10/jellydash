using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jellydash.Controllers
{
    /// <summary>
    /// Simple controller exposing a health-check style ping endpoint.
    /// </summary>
    [Route("Jellydash")]
    [ApiController]
    public class JellydashController : ControllerBase
    {
        /// <summary>
        /// Returns a simple pong response to verify the plugin API is reachable.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> with the string "pong".</returns>
        [HttpGet("ping")]
        [Authorize]
        public IActionResult Ping()
        {
            return Ok("pong");
        }
    }
}
