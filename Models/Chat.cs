using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Travel_App_Web.Models
{
    public class Chat
    {
        [Key]
        public int Id { get; set; }

        public List<EmailInfo> InterlocutorsEmails { get; set; } = new List<EmailInfo>();

        public List<Message> Messages { get; set; } = new List<Message>();
    }

    public class EmailInfo
    {
        [Key, MaxLength(254), Column(TypeName = "varchar")]
        public string Email { get; set; } = string.Empty;
    }

    public class Message
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(254), Column(TypeName = "varchar")]
        public string SenderEmail { get; set; } = string.Empty;

        [MaxLength(100)]
        public string SenderName { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string Content { get; set; } = string.Empty;
        public DateTime DispatchTime { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
    }
}
