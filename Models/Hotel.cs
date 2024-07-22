using Innofactor.EfCoreJsonValueConverter;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace Models
{
    public class Hotel : GooglePlace
    {
        [MaxLength(30), Column(TypeName = "varchar")]
        public string HotelId { get; set; } = null!;

        public City City { get; set; } = null!;

        [JsonField, MaxLength(400), Column(TypeName = "varchar")]
        public Dictionary<string, int>? Sentiments { get; set; } = null;
    }

    public class HotelOrder
    {
        [Key, MaxLength(100)]
        public string OrderId { get; set; } = null!;
        [MaxLength(100), Column(TypeName = "varchar")]
        public string ProviderConfirmationId { get; set; } = string.Empty;

        public HotelsOffer? HotelsOffer { get; set; }
    }

    public class HotelsOffer
    {
        [Key, MaxLength(100), Column(TypeName = "varchar")]
        public string OfferId { get; set; } = null!;
        [MaxLength(30), Column(TypeName = "varchar")]
        public string HotelId { get; set; } = null!;

        public string? OrderId { get; set; }

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }

        public int AdultsQuantity { get; set; }
        public int? RoomQuantity { get; set; } = null;

        [JsonField, MaxLength(3), Column(TypeName = "varchar")]
        public SpecialRate? RateCode { get; set; } = null;

        [JsonField, MaxLength(300), Column(TypeName = "varchar")]
        public List<BoardType>? Boards { get; set; } = null;

        public Room? Room { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; } = null;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceTotal { get; set; }

        [MaxLength(3), Column(TypeName = "varchar")]
        public string Currency { get; set; } = null!;

        [MaxLength(10), Column(TypeName = "varchar")]
        public string? PaymentType { get; set; } = null;

        public PolicyDetails? PolicyDetails { get; set; }


        public enum SpecialRate
        {
            [EnumMember(Value = "Rack")]
            RAC,
            [EnumMember(Value = "Best Available Rate")]
            BAR,
            [EnumMember(Value = "Promotion")]
            PRO,
            [EnumMember(Value = "Corporate")]
            COR,
            [EnumMember(Value = "Government (Qualified)")]
            GOV,
            [EnumMember(Value = "AAA (Qualified)")]
            AAA,
            [EnumMember(Value = "Bed and Breakfast")]
            BNB,
            [EnumMember(Value = "Package")]
            PKG,
            [EnumMember(Value = "Travel Industry")]
            TVL,
            [EnumMember(Value = "Special Promotional Rate")]
            SPC,
            [EnumMember(Value = "Weekend")]
            WKD,
            [EnumMember(Value = "Contract")]
            CON,
            [EnumMember(Value = "Senior (Europe) (Qualified)")]
            SNR,
            [EnumMember(Value = "AARP - American Association of Retired Persons (50+) (Qualified)")]
            ARP,
            [EnumMember(Value = "Senior (Qualified)")]
            SRS,
            [EnumMember(Value = "Room Only (No Breakfast)")]
            ROR,
            [EnumMember(Value = "Family")]
            FAM,
            [EnumMember(Value = "Day Rate")]
            DAY
        }
        public enum BoardType
        {
            [EnumMember(Value = "Room Only")]
            ROOM_ONLY,
            [EnumMember(Value = "Breakfast")]
            BREAKFAST,
            [EnumMember(Value = "Half Board")]
            HALF_BOARD,
            [EnumMember(Value = "Full Board")]
            FULL_BOARD,
            [EnumMember(Value = "All Inclusive")]
            ALL_INCLUSIVE,
            [EnumMember(Value = "Buffet Breakfast")]
            BUFFET_BREAKFAST,
            [EnumMember(Value = "Caribbean Breakfast")]
            CARIBBEAN_BREAKFAST,
            [EnumMember(Value = "Continental Breakfast")]
            CONTINENTAL_BREAKFAST,
            [EnumMember(Value = "English Breakfast")]
            ENGLISH_BREAKFAST,
            [EnumMember(Value = "Full Breakfast")]
            FULL_BREAKFAST,
            [EnumMember(Value = "Dinner Bed and Breakfast")]
            DINNER_BED_AND_BREAKFAST,
            [EnumMember(Value = "Lunch")]
            LUNCH,
            [EnumMember(Value = "Dinner")]
            DINNER,
            [EnumMember(Value = "Family Plan")]
            FAMILY_PLAN,
            [EnumMember(Value = "As Brochured")]
            AS_BROCHURED,
            [EnumMember(Value = "Self Catering")]
            SELF_CATERING,
            [EnumMember(Value = "Bermuda")]
            BERMUDA,
            [EnumMember(Value = "American")]
            AMERICAN,
            [EnumMember(Value = "Family American")]
            FAMILY_AMERICAN,
            [EnumMember(Value = "Modified")]
            MODIFIED
        }
    }

    public class Room
    {
        [Key]
        public long RoomId { get; set; }
        public string OfferId { get; set; } = null!;

        [MaxLength(3), Column(TypeName = "varchar")]
        public string Type { get; set; } = null!;

        [MaxLength(30), Column(TypeName = "varchar")]
        public string? Category { get; set; } = null;

        public int? Beds { get; set; } = null;

        [MaxLength(30), Column(TypeName = "varchar")]
        public string? BedType { get; set; } = null;

        [MaxLength(800)]
        public string? Description { get; set; } = null;
    }

    public class PolicyDetails
    {
        [Key]
        public long PolicyDetailsId { get; set; }
        [MaxLength(100), Column(TypeName = "varchar")]
        public string OfferId { get; set; } = null!;
        public Policy? Guarantee { get; set; } = null;
        public Policy? Deposit { get; set; } = null;
        public Policy? Prepay { get; set; } = null;
        public Policy? HoldTime { get; set; } = null;
        [JsonField]
        public List<Cancellation>? Cancellations { get; set; } = null;
        public CheckInOutPolicy? CheckInOut { get; set; } = null;
    }

    public class Policy
    {
        [Key]
        public long PolicyId { get; set; }
        public long PolicyDetailsId { get; set; }

        public double? Amount { get; set; } = null;

        public DateTime? Deadline { get; set; } = null;

        [MaxLength(500)]
        public string? Description { get; set; } = null;

        [JsonField, MaxLength(150), Column(TypeName = "varchar")]
        public List<VendorCodes>? CreditCards { get; set; } = null;

        [JsonField, MaxLength(150), Column(TypeName = "varchar")]
        public List<string>? Methods { get; set; } = null;
    }

    public class Cancellation
    {
        public string? Type { get; set; } = null;
        public string? Description { get; set; } = null;

        public double? Amount { get; set; } = null;

        public int? NumberOfNights { get; set; } = null;

        public double? Percentage { get; set; } = null;

        public DateTime? Deadline { get; set; } = null;
    }

    [Owned]
    public class CheckInOutPolicy
    {
        [MaxLength(10), Column(TypeName = "varchar")]
        public string CheckIn { get; set; } = null!;

        [MaxLength(150)]
        public string CheckInDescription { get; set; } = null!;

        [MaxLength(10), Column(TypeName = "varchar")]
        public string CheckOut { get; set; } = null!;

        [MaxLength(150)]
        public string CheckOutDescription { get; set; } = null!;
    }
}