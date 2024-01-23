using System.ComponentModel.DataAnnotations;

namespace Travel_App_Web.Models
{
    public class RegisterModel
    {
        [Required, MinLength(3, ErrorMessage = "Please use a First Name bigger than 3 letters."),
            MaxLength(100, ErrorMessage = "Please use a First Name less than 100 letters.")]
        public string FirstName { get; set; } = null!;


        [MaxLength(100)]
        public string? MiddleName { get; set; }


        [Required, MinLength(3, ErrorMessage = "Please use a Last Name bigger than 3 letters."),
            MaxLength(100, ErrorMessage = "Please use a Last Name less than 100 letters.")]
        public string LastName { get; set; } = null!;

        [Required, EmailAddress(ErrorMessage = "Please enter your valid Email")]
        public string Email { get; set; } = null!;

        [Required, Phone(ErrorMessage = "Please enter your valid Phone.")]
        public string Phone { get; set; } = null!;

        [Required, 
            MinLength(8, ErrorMessage = "Password must be at least 8 characters."),
            RegularExpression(@"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*\W).*$", ErrorMessage = "Password must contain at least one digit, " +
            "one lowercase letter, one uppercase letter, and one special character.")]
        public string Password { get; set; } = null!;

        [Required, 
            Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
