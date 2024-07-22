using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Services;
using Travel_App_Web.Data;

namespace Travel_App_Web.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LocationController : Controller
    {
        private readonly DBContext _dbcontext;
        private AmadeusService? amadeus;

        public LocationController(DBContext context, AmadeusService amadeus)
        {
            _dbcontext = context;
            this.amadeus = amadeus;
        }

        [HttpGet("countries-keys-names")]
        public async Task<ActionResult<string>> GetCountriesKeysNames()
        {
            try
            {
                var countries = await _dbcontext.Countries
                    .Select(c => new { c.CCA2, c.Name })
                    .ToListAsync();

                var values = countries.Select(obj => (obj.CCA2, obj.Name)).ToList();

                string valuesJson = JsonConvert.SerializeObject(values);
                return Ok(valuesJson);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        [HttpGet("countries-list")]
        public async Task<ActionResult<List<Country>>> GetCountriesList()
        {
            try
            {
                var countries = await _dbcontext.Countries.ToListAsync();
                return Ok(countries);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        [HttpGet("country-by-cca2/{cca2}")]
        public async Task<ActionResult<Country>> GetCountryByKey(string cca2)
        {
            try
            {
                var country = await _dbcontext.Countries.
                    Include(c => c.IDD)
                    .FirstOrDefaultAsync(c => c.CCA2 == cca2);
                if (country != null)
                {
                    return Ok(country);
                }
                return BadRequest("Such country does not exist in the database");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        [HttpGet("add-country/{countryName}")]
        public async Task<ActionResult<string>> AddCountry(string countryName)
        {
            try
            {
                using HttpClient client = new();
                string url = $"https://restcountries.com/v3.1/name/{countryName.ToLower().Replace(" ", "%20")}";
                var response = await client.GetAsync(url);

                string jsonCountriesResponse;
                if (response.IsSuccessStatusCode)
                {
                    jsonCountriesResponse = await response.Content.ReadAsStringAsync();
                    JArray countriesJson = JArray.Parse(jsonCountriesResponse);

                    List<Country>? countries = new();

                    foreach (var countryJson in countriesJson)
                    {
                        try
                        {
                            var name = countryJson["name"]?["common"]?.ToString();
                            var cca2 = countryJson["cca2"]?.ToString();

                            if (_dbcontext.Countries.Any(c => c.CCA2 == cca2))
                                continue;

                            var region = countryJson["region"]?.ToString();
                            var subRegion = countryJson["subregion"]?.ToString();

                            var currenciesJson = countryJson["currencies"]?.ToString();
                            var currencies = currenciesJson != null ? JsonConvert.DeserializeObject<Dictionary<string, Currency>>(currenciesJson) : new();

                            var languagesJson = countryJson["languages"]?.ToString();
                            var languages = languagesJson != null ? JsonConvert.DeserializeObject<Dictionary<string, string>>(languagesJson) : new();

                            var timezonesJson = countryJson["timezones"]?.ToString();
                            var timezones = timezonesJson != null ? JsonConvert.DeserializeObject<List<string>>(timezonesJson) : new();

                            var flagURL = countryJson["flags"]?["svg"]?.ToString();

                            var iddJson = countryJson["idd"]?.ToString();
                            var idd = iddJson != null ? JsonConvert.DeserializeObject<Idd>(iddJson) : new();

                            Country? country = new()
                            {
                                CCA2 = cca2 ?? string.Empty,
                                Name = name ?? string.Empty,
                                Region = region ?? string.Empty,
                                Subregion = subRegion ?? string.Empty,
                                Currencies = currencies,
                                Languages = languages,
                                Timezones = timezones,
                                FlagURL = flagURL,
                                IDD = idd
                            };
                            
                            _dbcontext.Add(country);
                            await _dbcontext.SaveChangesAsync();

                            country = await _dbcontext.Countries.
                                Include(c => c.IDD).
                                FirstOrDefaultAsync(c => c.CCA2 == cca2);
                            if (country != null)
                                countries.Add(country);
                        }
                        catch (DbUpdateException ex)
                        {
                            string message = ex.ToString();
                            continue;
                        }
                    }

                    string resultJson = JsonConvert.SerializeObject(countries);
                    return Ok(resultJson);
                }

                return BadRequest("This country does not exist");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        [HttpGet("cities/{countryCode}")]
        public async Task<ActionResult<List<City>>> GetCities(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode) || countryCode.Any(c => !char.IsAsciiLetter(c)))
                return BadRequest("Incorrect format of country code");

            List<City> cities = await _dbcontext.Cities.Where(c => c.Country.CCA2 == countryCode).ToListAsync();

            return Ok(cities);
        }

        [HttpGet("add-city/{countryCode}-{cityName}")]
        public async Task<ActionResult<string>> AddCity(string countryCode, string cityName)
        {
            try
            {
                Country country = await _dbcontext.Countries.FindAsync(countryCode);
                List<City> cities = await amadeus.GetCitiesByCountryCode(countryCode, cityName);
                cities.ForEach(c => c.Country = country);

                await _dbcontext.Cities.AddRangeAsync(cities);
                await _dbcontext.SaveChangesAsync();

                cities = await _dbcontext.Cities.Where(c => c.Name.ToLower().Contains(cityName) && c.Country.CCA2 == countryCode).ToListAsync();
                return Ok(JsonConvert.SerializeObject(cities));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return BadRequest(ex.Message);
            }
            finally
            {
                amadeus = null;
            }
        }
    }
}
