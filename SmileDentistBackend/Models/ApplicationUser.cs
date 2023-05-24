using Microsoft.AspNetCore.Identity;

namespace SmileDentistBackend.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
    }
}
