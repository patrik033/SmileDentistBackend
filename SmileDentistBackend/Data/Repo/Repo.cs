using Microsoft.EntityFrameworkCore;
using SmileDentistBackend.Models;

namespace SmileDentistBackend.Data.Repo
{
    public class Repo : IRepo
    {

        private readonly QuartzContext _context;

        public Repo(QuartzContext context)
        {
            _context = context;
        }

        public List<Booking> GetAll()
        {
            return _context.Bookings.ToList();
        }


        public async Task<List<Booking>> GetByDaySender()
        {
            var onlyByDay = await _context.Bookings.Where(x => x.ScheduledDateBefore.Value.Date == DateTime.Now.Date && x.MailHasBeenSent != true).ToListAsync();
            return onlyByDay;
        }

        public async Task<List<int>> GetByHours()
        {
            var onlyByHour = await _context.Bookings.OrderBy(x => x.ScheduledTimeHourlyBefore).Where(x => x.ScheduledTimeHourlyBefore.Value.Date == DateTime.Now.Date && x.MailHasBeenSent != true).Select(x => x.ScheduledTimeHourlyBefore.Value.Hour).ToListAsync();
            return onlyByHour;
        }

        public async Task<List<Booking>> GetByHour()
        {
            var currentHour = await _context.Bookings.Where(x => x.ScheduledTimeHourlyBefore.Value.Hour == DateTime.Now.Hour && x.MailHasBeenSent != true).ToListAsync();
            return currentHour;
        }

        public async Task<Booking> UpdateBooking(Booking bookings)
        {
            var checkBooking = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == bookings.Id);
            if (checkBooking != null)
            {
                checkBooking.MailHasBeenSent = true;
                _context.Bookings.Update(bookings);
                await _context.SaveChangesAsync();
            }
            return bookings;
        }
    }
}
