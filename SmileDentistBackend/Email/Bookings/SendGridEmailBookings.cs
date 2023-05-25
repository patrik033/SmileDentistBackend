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
            DateTime data = (DateTime)time;
            var niceTime = $"{data.ToLongDateString()} klockan {data.ToShortTimeString()}";
            sendGridMessage.SetFrom(from);
            sendGridMessage.AddTo(to);
            sendGridMessage.SetSubject(subject);
            sendGridMessage.SetTemplateId("d-e0c5b898b60a45a6ad76aa450b26b442");
            sendGridMessage.SetTemplateData(new ExampleTemplateData
            {
                Name = name,
                Time = niceTime,
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
