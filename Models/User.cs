using Innofactor.EfCoreJsonValueConverter;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace Models
{
    public class User
    {
        [Key, Required, EmailAddress, MaxLength(80), Column(TypeName = "varchar")]
        public string Email { get; set; } = null!;

        [Required, MinLength(3), MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required, MinLength(3), MaxLength(100)]
        public string LastName { get; set; } = null!;

        [Required, Phone, MaxLength(15), Column(TypeName = "varchar")]
        public string Phone { get; set; } = null!;

        [Required, MaxLength(2), Column(TypeName = "varchar")]
        public Gender Gender { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string PasswordHash { get; set; } = null!;

        public Country? Country { get; set; }

        public Role Role { get; set; } = null!;

        public List<Chat> Chats { get; set; } = new();

        public string IPSString { get; set; } = string.Empty;
        [NotMapped]
        public List<string> IPS
        {
            get
            {
                if (string.IsNullOrEmpty(IPSString))
                    return new List<string>();
                else
                    return IPSString.Split(';').ToList();
            }
            set
            {
                IPSString = string.Join(";", value);
            }
        }

        public void AddIP(string userIP)
        {
            var ipList = IPS;
            ipList.Add(userIP);
            IPS = ipList;
        }
        public void RemoveIP(string userIP)
        {
            var ipList = IPS;
            ipList.Remove(userIP);
            IPS = ipList;
        }

        public void SetPassword(string password)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, 32, 10000);
            byte[] salt = deriveBytes.Salt;
            byte[] hash = deriveBytes.GetBytes(32);
            PasswordHash = Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

        public bool VerifyPassword(string password)
        {
            string[] parts = PasswordHash.Split(':');
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] hash = Convert.FromBase64String(parts[1]);

            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] newHash = deriveBytes.GetBytes(32);
            return newHash.SequenceEqual(hash);
        }
    }

    public enum Gender
    {
        [EnumMember(Value = "Male")]
        ML,
        [EnumMember(Value = "Female")]
        FL,
        [EnumMember(Value = "Other")]
        OT
    }

    public static class ChatListExtensions
    {
        public static void AddChat(this List<Chat> chats, Chat chat, Role userRole)
        {
            if (userRole.RoleName == "Admin" || chats.Count < 1)
                chats.Add(chat);
        }
    }
}
