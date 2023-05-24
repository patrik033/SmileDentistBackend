using Microsoft.AspNetCore.Identity;

namespace SmileDentistBackend.Email
{
    public class EmailConfirmationTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public EmailConfirmationTokenProviderOptions()
        {
            Name = "EmailDataProtectorTokenProvider";
            TokenLifespan = TimeSpan.FromMinutes(5);
        }
    }
}
