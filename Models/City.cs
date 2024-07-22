using Innofactor.EfCoreJsonValueConverter;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class City
    {
        [Key]
        public long CityId { get; set; }

        [Required, MaxLength(30), Column(TypeName = "varchar")]
        public string Name { get; set; } = null!;

        [MinLength(3), MaxLength(3), Column(TypeName = "varchar")]
        public string? IataCode { get; set; }

        public Country Country { get; set; } = null!;

        [JsonField, MaxLength(50), Column(TypeName = "varchar")]
        public Dictionary<string, double> GeoCode { get; set; } = null!;
    }
}