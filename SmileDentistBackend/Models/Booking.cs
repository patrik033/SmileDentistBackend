using System.ComponentModel.DataAnnotations;

namespace SmileDentistBackend.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public string? Message { get; set; }
        public string? Name { get; set; }
        [Required]
        public string? Email { get; set; }
        [Required]
        public Guid UserId { get; set; }
        public DateTime? ScheduledTimeHourlyBefore { get; set; }
        public DateTime? ScheduledDateBefore { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public bool? MailHasBeenSent { get; set; } = false;
    }
}
