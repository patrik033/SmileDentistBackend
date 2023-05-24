using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmileDentistBackend.Data;
using SmileDentistBackend.Models;

namespace SmileDentistBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SchedulererController : ControllerBase
    {
        private readonly QuartzContext _context;
        private readonly ILogger<SchedulererController> _logger;

        public SchedulererController(QuartzContext context, ILogger<SchedulererController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PostBooking([FromBody] Booking booking)
        {
            if (booking != null)
            {

                var localTime = booking.ScheduledTime;
                var convertedTime = localTime.Value.ToLocalTime();

                DateTime? hourlyBefore = booking.ScheduledTimeHourlyBefore;
                DateTime? convertedHourlyBefore = hourlyBefore.Value.ToLocalTime();

                DateTime? dayBefore = booking.ScheduledDateBefore;
                DateTime? convertedDayBefore = dayBefore.Value.ToLocalTime();


                var doublePosts = await _context.Bookings.FirstOrDefaultAsync(x => x.ScheduledTime == convertedTime);


                if (doublePosts != null)
                {
                    return BadRequest("Sorry the booked time aldready exists, please choose another time");
                }
                else
                {
                    return NoDoublePosts(booking, convertedTime, hourlyBefore, ref convertedHourlyBefore, dayBefore, ref convertedDayBefore);
                }
            }
            return BadRequest();
        }

        private IActionResult NoDoublePosts(Booking booking, DateTime convertedTime, DateTime? hourlyBefore, ref DateTime? convertedHourlyBefore, DateTime? dayBefore, ref DateTime? convertedDayBefore)
        {
            if (hourlyBefore.Value.Year < DateTime.Now.Year)
            {
                convertedHourlyBefore = null;
            }

            if (dayBefore.Value.Year < DateTime.Now.Year)
            {
                convertedDayBefore = null;
            }

            var sendToDatabase = new Booking
            {
                Id = booking.Id,
                Message = "",
                Name = booking.Name,
                Email = booking.Email,
                UserId = booking.UserId,
                ScheduledTime = convertedTime,
                ScheduledTimeHourlyBefore = convertedHourlyBefore,
                ScheduledDateBefore = convertedDayBefore,
                MailHasBeenSent = false,
            };

            try
            {
                _context.Bookings.Add(sendToDatabase);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occured: {ex.Message}", ex);
            }
            return Ok();
        }


        [HttpGet("/Month")]
        public async Task<IActionResult> GetAllMonth()
        {
            var datesAhead = DateTime.Now.AddMonths(2);
            var dates = _context.Bookings.Where(x => x.ScheduledTime.Value.Month >= DateTime.Now.Month && x.ScheduledTime.Value.Month <= datesAhead.Month);
            var datesFiltered = dates.Where(x => x.ScheduledTime.Value.Date >= DateTime.Now.Date);
            return Ok(datesFiltered);
        }


    }
}
