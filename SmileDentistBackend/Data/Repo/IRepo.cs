using SmileDentistBackend.Models;

namespace SmileDentistBackend.Data.Repo
{
    public interface IRepo
    {
        List<Booking> GetAll();
        Task<List<int>> GetByHours();
        Task<List<Booking>> GetByHour();
        Task<List<Booking>> GetByDaySender();
        Task<Booking> UpdateBooking(Booking bookings);
    }
}
