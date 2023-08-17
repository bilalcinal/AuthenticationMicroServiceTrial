using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Notifacition.API.Data;
using Notifacition.API.Model;

namespace Notifacition.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class NotifacitionController : ControllerBase
    {
        private readonly IConfiguration Configuration;

        public NotifacitionController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail([FromQuery] EmailModel email)
        {
               var _notifacitionController = new NotifacitionController(Configuration);

                await _notifacitionController.SendEmail(email);

                return Ok("Email sent successfully!");
           
        }
    }
}
