using Innofactor.EfCoreJsonValueConverter;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace Models
{
    public class GooglePlace
    {
        [Key, MaxLength(50), Column(TypeName = "varchar")]
        public string PlaceId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(300)]
        public string? Description { get; set; } = null;

        public Location Location { get; set; } = new();

        [MaxLength(100), Column(TypeName = "varchar")]
        public string? IconUri { get; set; } = null;

        public Contact? Contact { get; set; } = null;

        [JsonField]
        public List<string> WeekdayDescriptions { get; set; } = new();

        public double? Rating { get; set; } = null;
        public int? UserRatingCount { get; set; } = null;
        public List<Review> Reviews { get; set; } = new();
    }

    public class Location
    {
        [Key]
        public long LocationId { get; set; }
        public string? PlaceId { get; set; }

        public string? ShortAddress { get; set; } = null;

        [MaxLength(200)]
        public string FormattedAddress { get; set; } = string.Empty;
        [JsonField]
        public List<AddressComponent> AddressComponents { get; set; } = new();

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int? UtcOffsetMinutes { get; set; } = null;

        [JsonField]
        public List<string> ImagesPaths { get; set; } = new();

        [MaxLength(150)]
        public string GoogleMapsUri { get; set; } = string.Empty;
    }

    public class AddressComponent
    {
        public virtual string ShortName { get; set; }

        public virtual string LongName { get; set; }

        public virtual IEnumerable<AddressComponentType> Types { get; set; }
    }

    [Owned]
    public class Contact
    {
        [MaxLength(500)]
        public string? WebsiteUri { get; set; } = null;
        [MaxLength(20), Column(TypeName = "varchar")]
        public string? PhoneNumber { get; set; } = null;
    }

    [Owned]
    public class Review
    {
        [Key, MaxLength(200), Column(TypeName = "varchar")]
        public string ReviewId { get; set; } = null!;

        [MaxLength(50)]
        public string PublishTimeDescription { get; set; } = null!;

        public double? Rating { get; set; }

        [MaxLength(1000)]
        public string Text { get; set; } = null!;

        [MaxLength(50)]
        public string AuthorDisplayName { get; set; } = null!;

        [MaxLength(150)]
        public string PhotoUri { get; set; } = null!;
    }

    public enum AddressComponentType
    {
        [EnumMember(Value = "accounting")]
        Accounting,
        [EnumMember(Value = "airport")]
        Airport,
        [EnumMember(Value = "amusement_park")]
        Amusement_Park,
        [EnumMember(Value = "aquarium")]
        Aquarium,
        [EnumMember(Value = "art_gallery")]
        Art_Gallery,
        [EnumMember(Value = "atm")]
        Atm,
        [EnumMember(Value = "bakery")]
        Bakery,
        [EnumMember(Value = "bank")]
        Bank,
        [EnumMember(Value = "bar")]
        Bar,
        [EnumMember(Value = "beauty_salon")]
        Beauty_Salon,
        [EnumMember(Value = "bicycle_store")]
        Bicycle_Store,
        [EnumMember(Value = "book_store")]
        Book_Store,
        [EnumMember(Value = "bowling_alley")]
        Bowling_Alley,
        [EnumMember(Value = "bus_station")]
        Bus_Station,
        [EnumMember(Value = "cafe")]
        Cafe,
        [EnumMember(Value = "campground")]
        Campground,
        [EnumMember(Value = "car_dealer")]
        Car_Dealer,
        [EnumMember(Value = "car_rental")]
        Car_Rental,
        [EnumMember(Value = "car_repair")]
        Car_Repair,
        [EnumMember(Value = "car_wash")]
        Car_Wash,
        [EnumMember(Value = "casino")]
        Casino,
        [EnumMember(Value = "cemetery")]
        Cemetery,
        [EnumMember(Value = "church")]
        Church,
        [EnumMember(Value = "city_hall")]
        City_Hall,
        [EnumMember(Value = "clothing_store")]
        Clothing_Store,
        [EnumMember(Value = "convenience_store")]
        Convenience_Store,
        [EnumMember(Value = "courthouse")]
        Courthouse,
        [EnumMember(Value = "dentist")]
        Dentist,
        [EnumMember(Value = "department_store")]
        Department_Store,
        [EnumMember(Value = "doctor")]
        Doctor,
        [EnumMember(Value = "drugstore")]
        DrugStore,
        [EnumMember(Value = "electrician")]
        Electrician,
        [EnumMember(Value = "electronics_store")]
        Electronics_Store,
        [EnumMember(Value = "embassy")]
        Embassy,
        [EnumMember(Value = "fire_station")]
        Fire_Station,
        [EnumMember(Value = "florist")]
        Florist,
        [EnumMember(Value = "funeral_home")]
        Funeral_Home,
        [EnumMember(Value = "furniture_store")]
        Furniture_Store,
        [EnumMember(Value = "gas_station")]
        Gas_Station,
        [EnumMember(Value = "gym")]
        Gym,
        [EnumMember(Value = "hair_care")]
        Hair_Care,
        [EnumMember(Value = "hardware_store")]
        Hardware_Store,
        [EnumMember(Value = "hindu_temple")]
        Hindu_Temple,
        [EnumMember(Value = "home_goods_store")]
        Home_Goods_Store,
        [EnumMember(Value = "hospital")]
        Hospital,
        [EnumMember(Value = "insurance_agency")]
        Insurance_Agency,
        [EnumMember(Value = "jewelry_store")]
        Jewelry_Store,
        [EnumMember(Value = "laundry")]
        Laundry,
        [EnumMember(Value = "lawyer")]
        Lawyer,
        [EnumMember(Value = "library")]
        Library,
        [EnumMember(Value = "light_rail_station")]
        Light_Rail_Station,
        [EnumMember(Value = "liquor_store")]
        Liquor_Store,
        [EnumMember(Value = "local_government_office")]
        Local_Government_Office,
        [EnumMember(Value = "locksmith")]
        Locksmith,
        [EnumMember(Value = "lodging")]
        Lodging,
        [EnumMember(Value = "meal_delivery")]
        Meal_Delivery,
        [EnumMember(Value = "meal_takeaway")]
        Meal_Takeaway,
        [EnumMember(Value = "mosque")]
        Mosque,
        [EnumMember(Value = "movie_rental")]
        Movie_Rental,
        [EnumMember(Value = "movie_theater")]
        Movie_Theater,
        [EnumMember(Value = "moving_company")]
        Moving_Company,
        [EnumMember(Value = "museum")]
        Museum,
        [EnumMember(Value = "night_club")]
        Night_Club,
        [EnumMember(Value = "painter")]
        Painter,
        [EnumMember(Value = "park")]
        Park,
        [EnumMember(Value = "parking")]
        Parking,
        [EnumMember(Value = "pet_store")]
        Pet_Store,
        [EnumMember(Value = "pharmacy")]
        Pharmacy,
        [EnumMember(Value = "physiotherapist")]
        Physiotherapist,
        [EnumMember(Value = "plumber")]
        Plumber,
        [EnumMember(Value = "police")]
        Police,
        [EnumMember(Value = "post_office")]
        PostOffice,
        [EnumMember(Value = "primary_school")]
        Primary_School,
        [EnumMember(Value = "secondary_school")]
        Secondary_School,
        [EnumMember(Value = "real_estate_agency")]
        Real_Estate_Agency,
        [EnumMember(Value = "restaurant")]
        Restaurant,
        [EnumMember(Value = "roofing_contractor")]
        Roofing_Contractor,
        [EnumMember(Value = "rv_park")]
        Rv_Park,
        [EnumMember(Value = "school")]
        School,
        [EnumMember(Value = "shoe_store")]
        Shoe_Store,
        [EnumMember(Value = "shopping_mall")]
        Shopping_Mall,
        [EnumMember(Value = "spa")]
        Spa,
        [EnumMember(Value = "stadium")]
        Stadium,
        [EnumMember(Value = "storage")]
        Storage,
        [EnumMember(Value = "store")]
        Store,
        [EnumMember(Value = "subway_station")]
        Subway_Station,
        [EnumMember(Value = "supermarket")]
        Supermarket,
        [EnumMember(Value = "synagogue")]
        Synagogue,
        [EnumMember(Value = "taxi_stand")]
        Taxi_Stand,
        [EnumMember(Value = "tourist_attraction")]
        Tourist_Attraction,
        [EnumMember(Value = "train_station")]
        Train_Station,
        [EnumMember(Value = "transit_station")]
        Transit_Station,
        [EnumMember(Value = "travel_agency")]
        Travel_Agency,
        [EnumMember(Value = "university")]
        University,
        [EnumMember(Value = "veterinary_care")]
        Veterinary_Care,
        [EnumMember(Value = "zoo")]
        Zoo,
        [EnumMember(Value = "administrative_area_level_1")]
        Administrative_Area_Level_1,
        [EnumMember(Value = "administrative_area_level_2")]
        Administrative_Area_Level_2,
        [EnumMember(Value = "administrative_area_level_3")]
        Administrative_Area_Level_3,
        [EnumMember(Value = "administrative_area_level_4")]
        Administrative_Area_Level_4,
        [EnumMember(Value = "administrative_area_level_5")]
        Administrative_Area_Level_5,
        [EnumMember(Value = "administrative_area_level_6")]
        Administrative_Area_Level_6,
        [EnumMember(Value = "administrative_area_level_7")]
        Administrative_Area_Level_7,
        [EnumMember(Value = "archipelago")]
        Archipelago,
        [EnumMember(Value = "colloquial_area")]
        Colloquial_Area,
        [EnumMember(Value = "continent")]
        Continent,
        [EnumMember(Value = "country")]
        Country,
        [EnumMember(Value = "establishment")]
        Establishment,
        [EnumMember(Value = "finance")]
        Finance,
        [EnumMember(Value = "floor")]
        Floor,
        [EnumMember(Value = "food")]
        Food,
        [EnumMember(Value = "general_contractor")]
        General_Contractor,
        [EnumMember(Value = "geocode")]
        Geocode,
        [EnumMember(Value = "health")]
        Health,
        [EnumMember(Value = "intersection")]
        Intersection,
        [EnumMember(Value = "landmark")]
        Landmark,
        [EnumMember(Value = "locality")]
        Locality,
        [EnumMember(Value = "natural_feature")]
        Natural_Feature,
        [EnumMember(Value = "neighborhood")]
        Neighborhood,
        [EnumMember(Value = "place_of_worship")]
        Place_Of_Worship,
        [EnumMember(Value = "plus_code")]
        Plus_Code,
        [EnumMember(Value = "point_of_interest")]
        Point_Of_Interest,
        [EnumMember(Value = "political")]
        Political,
        [EnumMember(Value = "post_box")]
        Post_Box,
        [EnumMember(Value = "postal_code")]
        Postal_Code,
        [EnumMember(Value = "postal_code_prefix")]
        Postal_Code_Prefix,
        [EnumMember(Value = "postal_code_suffix")]
        Postal_Code_Suffix,
        [EnumMember(Value = "postal_town")]
        Postal_Town,
        [EnumMember(Value = "premise")]
        Premise,
        [EnumMember(Value = "room")]
        Room,
        [EnumMember(Value = "route")]
        Route,
        [EnumMember(Value = "street_address")]
        Street_Address,
        [EnumMember(Value = "street_number")]
        Street_Number,
        [EnumMember(Value = "sublocality")]
        Sublocality,
        [EnumMember(Value = "sublocality_level_1")]
        Sublocality_Level_1,
        [EnumMember(Value = "sublocality_level_2")]
        Sublocality_Level_2,
        [EnumMember(Value = "sublocality_level_3")]
        Sublocality_Level_3,
        [EnumMember(Value = "sublocality_level_4")]
        Sublocality_Level_4,
        [EnumMember(Value = "sublocality_level_5")]
        Sublocality_Level_5,
        [EnumMember(Value = "subpremise")]
        Subpremise,
        [EnumMember(Value = "town_square")]
        Town_Square,
        [EnumMember(Value = "address")]
        Address,
        [EnumMember(Value = "ward")]
        Ward,
        [EnumMember(Value = "unknown")]
        Unknown
    }
}