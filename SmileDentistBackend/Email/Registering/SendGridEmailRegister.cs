using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using SendGrid;

namespace SmileDentistBackend.Email.Registering
{
    public class SendGridEmailRegister : ISendGridEmailRegister
    {
        private readonly IOptions<EmailSettings> _emailSettings;

        public SendGridEmailRegister(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings;
        }

        public async Task<Response> SendAsync(string from, string to, string subject, string body)
        {
            var environmentVariableKey = Environment.GetEnvironmentVariable("SendGrid");
            //var sendGridKey = _emailSettings.Value.Key;
            var client = new SendGridClient(environmentVariableKey);

            var msg = new SendGridMessage()
            {
                From = new EmailAddress(from, "WebApp Registration"),
                Subject = subject,
                //PlainTextContent = body,
                HtmlContent = $"<strong>{body}</strong>"
            };
            msg.AddTo(new EmailAddress(to));
            var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }
            return response;
        }
    }
}
