using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travel_App_Web.Data;
using Travel_App_Web.Models;

namespace Travel_App_Web.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CityController : Controller
    {
        private DBContext _context;

        public CityController(DBContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("cities")]
        public async Task<ActionResult<List<City>>> GetCities()
        {
            List<City> cities = new();

            cities = await _context.Cities.ToListAsync();

            return Ok(cities);
        }
    }
}
