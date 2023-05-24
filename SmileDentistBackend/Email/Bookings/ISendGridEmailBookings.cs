using SendGrid;

namespace SmileDentistBackend.Email.Bookings
{
    public interface ISendGridEmailBookings
    {
        Task<Response> SendAsync(string from, string to, string subject, string body, DateTime? time, string name);
    }
}
