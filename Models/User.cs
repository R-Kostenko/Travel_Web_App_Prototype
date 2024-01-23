using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace Travel_App_Web.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MinLength(3), MaxLength(100)]
        public string FirstName { get; set; } = null!;


        [MaxLength(100)]
        public string? MiddleName { get; set; }


        [Required, MinLength(3),MaxLength(100)]
        public string LastName { get; set; } = null!;


        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required, Phone]
        public string Phone { get; set; } = null!;

        [Required, MinLength(8)]
        public string PasswordHash { get; set; } = null!;

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = new Role();

        public List<Tour>? Tours { get; set; }


        public void SetPassword(string password)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, 32, 10000))
            {
                byte[] salt = deriveBytes.Salt;
                byte[] hash = deriveBytes.GetBytes(32);
                PasswordHash = Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
            }
        }

        public bool VerifyPassword(string password)
        {
            string[] parts = PasswordHash.Split(':');
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] hash = Convert.FromBase64String(parts[1]);

            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                byte[] newHash = deriveBytes.GetBytes(32);
                return newHash.SequenceEqual(hash);
            }
        }
    }
}
