using Innofactor.EfCoreJsonValueConverter;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class TourAgency
    {
        [Key]
        public long AgencyId { get; set; }

        [MaxLength(80), Column(TypeName = "varchar")]
        public string Name { get; set; } = string.Empty;

        public Country? Country { get; set; }

        [JsonField, MaxLength(300)]
        public List<string> PhoneNumbers { get; set; } = new();

        [MaxLength(80), EmailAddress]
        public string Email { get; set; } = string.Empty;

        public List<User> Managers { get; set; } = new();

        public CreditCard CreditCard { get; set; } = null!;
    }
}
