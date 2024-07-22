using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class ParticipantUnit
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public User PrimaryUser { get; set; } = new();

        public List<Participant> OtherUsers { get; set; } = new();

        [Required]
        public CreditCard CreditCard { get; set; } = new();

        public List<HotelOrder> HotelsOrders { get; set; } = new();
    }

    public class Participant
    {
        [Key]
        public long Id { get; set; }

        public User? User { get; set; } = null;

        #region User Alternative

        [EmailAddress, MaxLength(80), Column(TypeName = "varchar")]
        public string? Email { get; set; } = null;

        [MinLength(3), MaxLength(100)]
        public string? FirstName { get; set; } = null;

        [MaxLength(100)]
        public string? MiddleName { get; set; } = null;

        [MinLength(3), MaxLength(100)]
        public string? LastName { get; set; } = null;

        [Phone, MaxLength(15), Column(TypeName = "varchar")]
        public string? Phone { get; set; } = null;

        [MaxLength(2), Column(TypeName = "varchar")]
        public Gender? Gender { get; set; } = null;

        public DateTime? DateOfBirth { get; set; } = null;

        #endregion
    }
}
