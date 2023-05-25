using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using SendGrid;
using SmileDentistBackend.Email.Bookings;

namespace SmileDentistBackend.Email.Token
{
    public class SendGridEmailTokens : ISendGridEmailTokens
    {
        private readonly IOptions<EmailSettings> _emailSettings;

        public SendGridEmailTokens(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings;
        }
        public async Task<Response> SendAsync(string from, string to, string subject, string tokenLink, string name)
        {
            var environmentVariableKey = Environment.GetEnvironmentVariable("SendGrid");
            var sendGridClient = new SendGridClient(environmentVariableKey);
            var sendGridMessage = new SendGridMessage();
            
            sendGridMessage.SetFrom(from);
            sendGridMessage.AddTo(to);
            sendGridMessage.SetSubject(subject);
            sendGridMessage.SetTemplateId("d-aa8a242d99ef457c9a6d4c0319c83b0e");
            sendGridMessage.SetTemplateData(new TokenTemplateData
            {
                Name = name,
                Token = tokenLink
            });
            
            var resp = await sendGridClient.SendEmailAsync(sendGridMessage);
            
            //var client = new SendGridClient(environmentVariableKey);

            //var msg = new SendGridMessage()
            //{
            //    From = new EmailAddress(from, "WebApp Registration"),
            //    Subject = subject,
            //    //PlainTextContent = body,
            //    HtmlContent = $"<strong>{body}</strong>"
            //};

            //var response = await client.SendEmailAsync(msg);

            //msg.AddTo(new EmailAddress(to));
            if (resp.IsSuccessStatusCode)
            {
                return resp;
            }
            return resp;
        }
    }
}
