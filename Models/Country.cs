using Innofactor.EfCoreJsonValueConverter;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Country
    {
        [Key, MinLength(2), MaxLength(2), Column(TypeName = "varchar")]
        public string CCA2 { get; set; } = null!;

        [Required, JsonField, MaxLength(80), Column(TypeName = "varchar")]
        public string Name { get; set; } = null!;

        [MaxLength(30), Column(TypeName = "varchar")]
        public string Region { get; set; } = string.Empty;

        [MaxLength(80), Column(TypeName = "varchar")]
        public string Subregion { get; set; } = string.Empty;

        [JsonField, MaxLength(100), Column(TypeName = "varchar")]
        public Dictionary<string, string>? Languages { get; set; } = new();

        [JsonField, MaxLength(300)]
        public Dictionary<string, Currency>? Currencies { get; set; } = new();

        [JsonField, MaxLength(200), Column(TypeName = "varchar")]
        public List<string>? Timezones { get; set; } = new();

        public string? FlagURL { get; set; }

        public Idd? IDD { get; set; } = null!;
    }

    public class Currency
    {
        public string Name { get; set; } = null!;
    }

    public class Idd
    {
        [Key]
        public long Id { get; set; }

        public string? CountryCCA2 { get; set; }

        [MaxLength(5), Column(TypeName = "varchar")]
        public string Root { get; set; } = string.Empty;

        [JsonField, Column(TypeName = "varchar(max)")]
        public List<string> Suffixes { get; set; } = new();
    }
}