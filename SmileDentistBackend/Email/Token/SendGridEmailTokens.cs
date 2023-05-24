using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using SendGrid;

namespace SmileDentistBackend.Email.Token
{
    public class SendGridEmailTokens : ISendGridEmailTokens
    {
        private readonly IOptions<EmailSettings> _emailSettings;

        public SendGridEmailTokens(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings;
        }
        public async Task<Response> SendAsync(string from, string to, string subject, string body, string tokenLink, string name)
        {
            var environmentVariableKey = Environment.GetEnvironmentVariable("SendGrid");
            var client = new SendGridClient(environmentVariableKey);

            var msg = new SendGridMessage()
            {
                From = new EmailAddress(from, "WebApp Registration"),
                Subject = subject,
                //PlainTextContent = body,
                HtmlContent = $"<strong>{body}</strong>"
            };

            var response = await client.SendEmailAsync(msg);

            msg.AddTo(new EmailAddress(to));
            if (response.IsSuccessStatusCode)
            {
                return response;
            }
            return response;
        }
    }
}
