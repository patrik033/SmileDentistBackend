using Quartz;
using SmileDentistBackend.Data.Repo;
using SmileDentistBackend.Email.Bookings;

namespace SmileDentistBackend.Schedulerer
{
    [DisallowConcurrentExecution]
    public class DailyJobsToBeScheduled : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISendGridEmailBookings _sender;
        private readonly ILogger<DailyJobsToBeScheduled> _logger;
        private readonly IConfiguration _configuration;

        public DailyJobsToBeScheduled
            (
            ILogger<DailyJobsToBeScheduled> logger,
            IServiceProvider serviceProvider,
            ISendGridEmailBookings sender,
            IConfiguration configuration
            )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _sender = sender;
            _configuration = configuration;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var service = scope.ServiceProvider.GetService<IRepo>();
                var allItems = await service.GetByDaySender();
                var emailSettings = Environment.GetEnvironmentVariable("EmailSettings");

                foreach (var item in allItems)
                {
                    var some = await _sender.SendAsync(emailSettings, emailSettings, "Smile Dentist Booking", $"Hello {item.Name} you have a reservation for dental care at: {item.ScheduledTime}", item.ScheduledTime, item.Name);

                    if (some.IsSuccessStatusCode)
                    {
                        item.MailHasBeenSent = true;
                        await service.UpdateBooking(item);
                        _logger.LogInformation($"Bookings id{item.Id} messageStatus has been set");
                    }
                }
            }
            return;
        }
    }
}
