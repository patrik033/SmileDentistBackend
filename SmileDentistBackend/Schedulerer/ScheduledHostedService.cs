using Quartz;
using Quartz.Spi;
using SmileDentistBackend.Data.Repo;

namespace SmileDentistBackend.Schedulerer
{
    public class ScheduledHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _jobFactory;
        private readonly IServiceProvider _serviceProvider;

        public ScheduledHostedService
            (
                ISchedulerFactory schedulerFactory,
                IJobFactory jobFactory,
                IServiceProvider serviceProvider
            )
        {
            _schedulerFactory = schedulerFactory;
            _jobFactory = jobFactory;
            _serviceProvider = serviceProvider;
        }

        public IScheduler Scheduler { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            Scheduler.JobFactory = _jobFactory;
            //var someTriggers = CreateJobAmounts(SetItems);

            if (DateTime.Now.Hour > 17 && DateTime.Now.Hour < 6)
                await StopAsync(cancellationToken);
            else
                await Scheduler.Start(cancellationToken);

            if (DateTime.Now.Hour > 6  && DateTime.Now.Hour < 17)
            {

                await Scheduler.AddJob(CreateJobForDailyMails(), true);
                await Scheduler.AddJob(CreateJobsForScheduledMailsHourly(), true);
                //create schedulerers for day before items in the database
                var dailyTriggers = FirstDailyTrigger(_serviceProvider);
                await Scheduler.ScheduleJob(dailyTriggers);


                //create schedulers for hourly jobs in the database
                var hourlyTrigger = await HourlyTrigger(_serviceProvider);
                await Scheduler.ScheduleJob(hourlyTrigger);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Scheduler?.Shutdown(cancellationToken);
        }

        private static IJobDetail CreateJobForDailyMails()
        {
            return JobBuilder
                .Create<DailyJobsToBeScheduled>()
                .StoreDurably()
                .WithIdentity("_job", "group")
                .Build();
        }

        private static IJobDetail CreateJobsForScheduledMailsHourly()
        {
            return JobBuilder
                .Create<DailyHourlyJobsToBeScheduled>()
                .StoreDurably()
                .WithIdentity("_job1", "group1")
                .Build();
        }

        private static ITrigger FirstDailyTrigger(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var service = scope.ServiceProvider.GetService<IRepo>();
                //var itemsToday = service.GetByDayScheduler();

                var some = TriggerBuilder.Create()
                    .WithIdentity(Guid.NewGuid().ToString(), "group")
                    //.WithCronSchedule("0 0 6 ? * MON-FRI *")
                    .StartNow()
                    .ForJob(CreateJobForDailyMails())
                    .Build();
                return some;
            }
        }

        private static async Task<ITrigger> HourlyTrigger(IServiceProvider serviceProvider)
        {

            //ITrigger some = new List<ITrigger>();
            using (var scope = serviceProvider.CreateScope())
            {
                var service = scope.ServiceProvider.GetService<IRepo>();
                var itemsToday = await service.GetByHours();
                var buff = "";

                if (itemsToday.Count > 0)
                {
                    for (int i = 0; i < itemsToday.Count; i++)
                    {
                        if (i == itemsToday.Count - 1)
                        {
                            buff += $"{itemsToday.ElementAt(i)}";
                        }
                        else
                        {
                            buff += $"{itemsToday.ElementAt(i)},";
                        }

                    }
                    var some = TriggerBuilder

                            .Create()
                            .WithIdentity(Guid.NewGuid().ToString(), Guid.NewGuid().ToString())
                            .WithCronSchedule($"0 0-59 {buff} ? * MON,TUE,WED,THU,FRI *")
                            .StartNow()
                            .ForJob(CreateJobsForScheduledMailsHourly())
                            .Build();

                    return some;
                }
                else
                {

                    return TriggerBuilder
                                .Create()
                                .WithIdentity(Guid.NewGuid().ToString(), Guid.NewGuid().ToString())
                                //.WithCronSchedule($" 0 0 6 1/1 * ? *")
                                //.StartNow()
                                .StartAt(new DateTime(2012, 2, 2))
                                .ForJob(CreateJobsForScheduledMailsHourly())
                                .Build();
                }
            }
        }


    }
}
