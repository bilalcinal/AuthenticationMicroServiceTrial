using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Notification.API.Model;

namespace Notification.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class NotificationController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public NotificationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        [HttpPost]
        public async Task<IActionResult> SendEmail(EmailModel emailModel)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("BilalCinal", "hbilalcinal@gmail.com"));
                message.To.Add(new MailboxAddress("", emailModel.ToEmail));
                message.Subject = emailModel.Subject;

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = emailModel.Body;
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync("smtp.gmail.com", 587, false);
                await client.AuthenticateAsync("hbilalcinal@gmail.com", "tqktaustbvneybed");
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return Ok("E-mail sent successfully!");
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred: {ex.Message}");
            }
        }

    }
}