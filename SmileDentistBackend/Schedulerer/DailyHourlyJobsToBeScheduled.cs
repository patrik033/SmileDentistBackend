using Quartz;
using SmileDentistBackend.Data.Repo;
using SmileDentistBackend.Email.Bookings;

namespace SmileDentistBackend.Schedulerer
{
    [DisallowConcurrentExecution]
    public class DailyHourlyJobsToBeScheduled : IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISendGridEmailBookings _sender;
        private readonly ILogger<DailyHourlyJobsToBeScheduled> _logger;
        private readonly IConfiguration _configuration;

        public DailyHourlyJobsToBeScheduled
            (
            ILogger<DailyHourlyJobsToBeScheduled> logger,
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
                var allItems = await service.GetByHour();
                var emailSettings = Environment.GetEnvironmentVariable("EmailSettings");
                

                foreach(var item in allItems)
                {
                    var mailSent = await _sender.SendAsync(emailSettings, emailSettings, "Scheduled Time", "", item.ScheduledTime, item.Name);
                    if (mailSent.IsSuccessStatusCode)
                    {
                        item.MailHasBeenSent = true;
                        await service.UpdateBooking(item);
                        _logger.LogInformation($"Bookings id{item.Id} messageStatus has been set");
                    }
                }
            }
            return; /*Task.CompletedTask;*/
        }
    }
}
