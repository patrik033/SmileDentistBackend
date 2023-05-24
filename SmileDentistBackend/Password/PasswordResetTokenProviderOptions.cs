using Microsoft.AspNetCore.Identity;

namespace SmileDentistBackend.Password
{
    public class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public PasswordResetTokenProviderOptions()
        {
            Name = "PasswordResetTokenProvider";
            TokenLifespan = TimeSpan.FromMinutes(5);
        }
    }
}
