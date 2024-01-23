using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Travel_App_Web.Models
{
    public class Tour
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public string ImagePath { get; set; } = null!;

        public List<User>? Users { get; set; } = null!;

        public List<City> Cities { get; set; } = null!;

        public List<Apartment> Prices { get; set; } = null!;

        public List<Option>? Included { get; set; }

        public List<Option>? NotIncluded { get; set; }

        public List<DayOfTour> Days { get; set; } = null!;

        public Hotel Hotel { get; set; } = null!;

        public Bus Bus { get; set; } = null!;
    }

    public class City
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string? Country { get; set; }

        [JsonIgnore]
        public List<Tour>? Tours { get; set; }
    }

    [Owned]
    public class Apartment
    {
        public string ApartmentType { get; set; } = null!;

        public decimal PriceEUR { get; set; }
    }

    [Owned]
    public class Option
    {
        public string Content { get; set; } = null!;
    }

    
    public class DayOfTour
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Number { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        public List<Image>? Images { get; set; } = null!;
    }

    public class Image
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; }

        [Required]
        public string Path { get; set; } = null!;
    }

    public class Hotel
    {
        [Key]
        public int Id { get; set; }

        public string Description { get; set; } = null!;

        public List<Image>? Images { get; set; } = null!;

        [JsonIgnore]
        public List<Tour>? Tours { get; set; }
    }

    public class Bus
    {
        [Key]
        public int Id { get; set; }

        public string Description { get; set; } = null!;

        public List<Image>? Images { get; set; }

        [JsonIgnore]
        public List<Tour>? Tours { get; set; }
    }
}
