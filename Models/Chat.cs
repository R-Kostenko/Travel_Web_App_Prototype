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
        [Key]
        public string Email { get; set; } = string.Empty;
    }

    public class Message
    {
        [Key]
        public int Id { get; set; }
        public string SenderEmail { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
