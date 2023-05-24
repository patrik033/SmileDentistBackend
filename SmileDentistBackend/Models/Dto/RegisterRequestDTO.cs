using System.ComponentModel.DataAnnotations;

namespace SmileDentistBackend.Models.Dto
{
    public class RegisterRequestDTO
    {
        public string? Name { get; set; }
        [Required(ErrorMessage = "Email is Required!")]
        public string? UserName { get; set; }
        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; }
        [Compare("Password",ErrorMessage = "The password and confirmation password do not match!")]
        public string? ConfirmPassword { get; set; }
        public string? Role { get; set; }
    }
}
