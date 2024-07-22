using Innofactor.EfCoreJsonValueConverter;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace Models
{
    public class Activity
    {
        [Key]
        public long TourId { get; set; }
        [Key, MaxLength(70), Column(TypeName = "varchar")]
        public string ActivityId { get; set; } = null!;

        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [JsonField, MaxLength(5), Column(TypeName = "varchar")]
        public ActivityType ActType { get; set; }

        public enum ActivityType
        {
            [EnumMember(Value = "Transportation")]
            TRANS,
            [EnumMember(Value = "Point of Interest")]
            POI,
            [EnumMember(Value = "Tour or Side Activity")]
            SIDE
        }
    }

    public class TransferOffer : Activity
    {
        public string? OrderId { get; set; }

        [JsonField, MaxLength(20), Column(TypeName = "varchar")]
        public TransferType TranType { get; set; }

        [JsonField, MaxLength(3), Column(TypeName = "varchar")]
        public VehicleType VehType { get; set; }

        [JsonField, MaxLength(2), Column(TypeName = "varchar")]
        public VehicleCategory VehCategory { get; set; }

        public string? CarIconURL { get; set; } = null;
        [MaxLength(300), Column(TypeName = "varchar")]
        public string? Description { get; set; } = null;

        [MaxLength(50)]
        public string? ProviderName { get; set; } = null;
        public string? ProviderLogoUrl { get; set; } = null;

        public long StartLocationId { get; set; }
        public long EndLocationId { get; set; }
        public Location StartLocation { get; set; } = null!;
        public Location EndLocation { get; set; } = null!;

        public int? DistanceValue { get; set; } = null;
        [MaxLength(5), Column(TypeName = "varchar")]
        public string? DistanceUnit { get; set; } = null;

        public double? PriceAmount { get; set; } = null;
        [MaxLength(5), Column(TypeName = "varchar")]
        public string? Currency { get; set; } = null!;
        [JsonField, MaxLength(100), Column(TypeName = "varchar")]
        public List<PaymentMethod> PaymentMethods { get; set; } = new();
        [JsonField]
        public List<Dictionary<string, string>> CancellationRules { get; set; } = new();


        public TransferOffer()
        {
            ActType = ActivityType.TRANS;
        }

        public enum TransferType
        {
            [EnumMember(Value = "Private Transport")]
            PRIVATE,
            [EnumMember(Value = "Shared Transport")]
            SHARED,
            [EnumMember(Value = "Taxi")]
            TAXI,
            [EnumMember(Value = "Hourly Driver Service")]
            HOURLY,
            [EnumMember(Value = "Airport Express Train")]
            AIRPORT_EXPRESS,
            [EnumMember(Value = "Airport Express Bus")]
            AIRPORT_BUS
        }
        public enum VehicleType
        {
            [EnumMember(Value = "Motorcycle")]
            MBK,
            [EnumMember(Value = "Car")]
            CAR,
            [EnumMember(Value = "Sedan")]
            SED,
            [EnumMember(Value = "Station Wagon")]
            WGN,
            [EnumMember(Value = "Electric Car")]
            ELC,
            [EnumMember(Value = "Minibus or Minivan")]
            VAN,
            [EnumMember(Value = "SUV")]
            SUV,
            [EnumMember(Value = "Limousine")]
            LMS,
            [EnumMember(Value = "Train")]
            TRN,
            [EnumMember(Value = "Bus")]
            BUS
        }
        public enum VehicleCategory
        {
            [EnumMember(Value = "Standard")]
            ST,
            [EnumMember(Value = "Business")]
            BU,
            [EnumMember(Value = "First Class")]
            FC
        }
        public enum PaymentMethod
        {
            [EnumMember(Value = "Credit Card")]
            CREDIT_CARD,
            [EnumMember(Value = "Invoice (not supported)")]
            INVOICE,
            [EnumMember(Value = "Travel Account (not supported)")]
            TRAVEL_ACCOUNT
        }
    }

    public class PointOfInteres : Activity
    {
        [MaxLength(100)]
        public string? Name { get; set; } = null;

        public long LocationId { get; set; }
        public Location? Location { get; set; } = null;

        [JsonField, MaxLength(15), Column(TypeName = "varchar")]
        public LocationCategory Category { get; set; }

        public string? PlaceIconUrl { get; set; } = null;

        public int? Rank { get; set; } = null;

        [JsonField]
        public List<string> Tags { get; set; } = new();

        public PointOfInteres()
        {
            ActType = ActivityType.POI;
        }

        public enum LocationCategory
        {
            [EnumMember(Value = "Landmark")]
            SIGHTS,
            [EnumMember(Value = "Beach/Park")]
            BEACH_PARK,
            [EnumMember(Value = "Historical")]
            HISTORICAL,
            [EnumMember(Value = "Nightlife")]
            NIGHTLIFE,
            [EnumMember(Value = "Restaurant")]
            RESTAURANT,
            [EnumMember(Value = "Shopping")]
            SHOPPING
        }
    }

    public class TourOrSideActivity : Activity
    {
        [MaxLength(300)]
        public string? ShortDescription { get; set; } = null;
        public string? Description { get; set; } = null;

        public long LocationId { get; set; }
        public Location? Location { get; set; } = null;

        public double? Rating { get; set; } = null;

        [JsonField]
        public List<string> PicturesUrls { get; set; } = new();

        public string? BookingLink { get; set; } = null;
        public double? PriceAmount { get; set; } = null;
        [MaxLength(5), Column(TypeName = "varchar")]
        public string? Currency { get; set; } = null;

        [MaxLength(50), Column(TypeName = "varchar")]
        public string? MinimumDuration { get; set; } = null;

        public TourOrSideActivity()
        {
            ActType = ActivityType.SIDE;
        }
    }

    public class TransferOrder
    {
        [Key]
        public string OrderId { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public TransferOffer? TransferOffer { get; set; } = null;
    }
}