using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using SendGrid;
using SmileDentistBackend.Email.Token;

namespace SmileDentistBackend.Email.Registering
{
    public class SendGridEmailRegister : ISendGridEmailRegister
    {
        private readonly IOptions<EmailSettings> _emailSettings;

        public SendGridEmailRegister(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings;
        }

        public async Task<Response> SendAsync(string from, string to, string subject, string token,string name)
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
                Token = token
            });

            var resp = await sendGridClient.SendEmailAsync(sendGridMessage);

            if (resp.IsSuccessStatusCode)
            {
                return resp;
            }
            return resp;
        }
    }
}
