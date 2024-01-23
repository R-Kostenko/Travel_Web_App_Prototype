using System.ComponentModel.DataAnnotations;

namespace Travel_App_Web.Models
{
    public class LoginModel
    {
        [Required, EmailAddress(ErrorMessage = "Please enter your valid Email")]
        public string Email { get; set; }
                
        [Required,
            MinLength(8, ErrorMessage = "Password must be at least 8 characters."),
            RegularExpression(@"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*\W).*$", ErrorMessage = "Password must contain at least one digit, " +
            "one lowercase letter, one uppercase letter, and one special character.")]
        public string Password { get; set; }
    }
}
