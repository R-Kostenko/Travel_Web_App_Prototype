using Innofactor.EfCoreJsonValueConverter;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Models
{
    public class Chat
    {
        [Key]
        public long ChatId { get; set; }

        [MaxLength(200), Column(TypeName = "varchar")]
        public string Emails { get; set; } = string.Empty;

        [NotMapped]
        public List<string> InterlocutorsEmails
        {
            get
            {
                if (string.IsNullOrEmpty(Emails))
                    return new List<string>();
                else
                    return Emails.Split(',').ToList();
            }
            set
            {
                Emails = string.Join(",", value);
            }
        }

        public List<Message> Messages { get; set; } = new List<Message>();
    }

    [Owned]
    public class Message
    {
        [Key]
        public long MessageId { get; set; }

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
