using Innofactor.EfCoreJsonValueConverter;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Tour
    {
        [Key]
        public long TourId { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        [Required]
        public TourAgency? Agency { get; set; }

        [Required]
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(30), Column(TypeName = "varchar")]
        public string AmountRange { get; set; } = string.Empty;

        public double ExtraAmount { get; set; } = 0;

        [Required, MaxLength(4), Column(TypeName = "varchar")]
        public string Currency { get; set; } = "USD";

        public string ImagePath { get; set; } = null!;

        public int ParticipantsMaxNumber { get; set; } = 1;

        public List<ParticipantUnit> Participants { get; set; } = new();

        public List<City> Cities { get; set; } = new();

        [JsonField, MaxLength(400)]
        public List<string> Included { get; set; } = new();

        [JsonField, MaxLength(400)]
        public List<string> NotIncluded { get; set; } = new();

        [JsonField, MaxLength(600)]
        public Dictionary<string, string> DayTitles { get; set; } = new();

        public List<Activity> Program { get; set; } = new();

        public List<Hotel> Hotels { get; set; } = new();

        public List<HotelsOffer> HotelsOffers { get; set; } = new();
        public List<TransferOrder> TransferOrders { get; set; } = new();

        [JsonField]
        public List<string> ScheduledTasksIds { get; set; } = new();
    }
}