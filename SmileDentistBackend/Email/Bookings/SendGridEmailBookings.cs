using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using SendGrid;

namespace SmileDentistBackend.Email.Bookings
{
    public class SendGridEmailBookings : ISendGridEmailBookings
    {
        private readonly IOptions<EmailSettings> _emailSettings;

        public SendGridEmailBookings(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings;
        }
        public async Task<Response> SendAsync(string from, string to, string subject, string body, DateTime? time, string name)
        {
            var environmentVariableKey = Environment.GetEnvironmentVariable("SendGrid");
            //var sendGridKey = _emailSettings.Value.Key;
            var sendGridClient = new SendGridClient(environmentVariableKey);
            var sendGridMessage = new SendGridMessage();

            sendGridMessage.SetFrom(from);
            sendGridMessage.AddTo(to);
            sendGridMessage.SetTemplateId("d-e0c5b898b60a45a6ad76aa450b26b442");
            sendGridMessage.SetTemplateData(new ExampleTemplateData
            {
                Name = name,
                Time = time,
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
