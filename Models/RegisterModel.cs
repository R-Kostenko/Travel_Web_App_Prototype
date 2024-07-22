using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Enter your first name"),
         MinLength(3, ErrorMessage = "Please use a first name longer than 3 characters"),
         MaxLength(100, ErrorMessage = "Please use a first name shorter than 100 characters")]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Enter your last name"),
         MinLength(3, ErrorMessage = "Please use a last name longer than 3 characters"),
         MaxLength(100, ErrorMessage = "Please use a last name shorter than 100 characters")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Enter your email address"),
         EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Enter your phone number"),
         Phone(ErrorMessage = "Please enter a valid phone number")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Select your gender")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Enter your date of birth")]
        public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-25);

        [Required(ErrorMessage = "Select your country")]
        public Country? Country { get; set; }

        [Required(ErrorMessage = "Enter your password"),
         MinLength(8, ErrorMessage = "Password must be at least 8 characters long"),
         RegularExpression(@"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*\W).*$",
             ErrorMessage = "Password must contain at least one digit, one lowercase letter, one uppercase letter, and one special character")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirm your password"),
         Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
