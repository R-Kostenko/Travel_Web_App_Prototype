using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travel_App_Web.Data;
using Travel_App_Web.Models;

namespace Travel_App_Web.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TourController : Controller
    {
        private DBContext _context;

        public TourController(DBContext context)
        {
            _context = context;
        }

        [HttpGet("tours-with-prices")]
        public async Task<ActionResult<List<Tour>>> GetTours()
        {
            List<Tour> tours = await _context.Tours.
                Include(tour => tour.Prices).
                ToListAsync();
            return Ok(tours);
        }

        [HttpGet("tour-by-id/{Id}")]
        public async Task<ActionResult<Tour>> GetTourById(int Id)
        {
            Tour? tour = await _context.Tours.
                //IgnoreAutoIncludes().
                Where(tour => tour.Id == Id).
                Include(tour => tour.Cities).
                Include(tour => tour.Prices).
                Include(tour => tour.Included).
                Include(tour => tour.NotIncluded).
                Include(tour => tour.Days).
                ThenInclude(day => day.Images).
                Include(tour => tour.Hotel.Images).
                Include(tour => tour.Bus.Images).
                AsSplitQuery().
                FirstOrDefaultAsync();
            if (tour == null)
            {
                return NotFound();
            }
            return Ok(tour);
        }

        [HttpPost("create-tour")]
        public async Task<ActionResult> CreateTour(Tour tour)
        {
            if (tour == null)
            {
                return BadRequest("Tour object is null");
            }

            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            return Ok("Tour was created successfully");
        }
    }
}
