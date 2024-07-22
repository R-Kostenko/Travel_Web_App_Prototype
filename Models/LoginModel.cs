using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Enter your email address"),
            EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Enter your password"),
            MinLength(8, ErrorMessage = "Password must be at least 8 characters long"),
            RegularExpression(@"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*\W).*$",
                ErrorMessage = "Password must contain at least one digit, one lowercase letter, one uppercase letter, and one special character")]
        public string Password { get; set; }
    }
}
