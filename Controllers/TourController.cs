using GoogleApi.Entities.Search.Common;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Services.Mail;
using System.Security.Claims;
using Travel_App_Web.Data;

namespace Travel_App_Web.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("[controller]")]
    [ApiController]
    public class TourController : Controller
    {
        private readonly DBContext _context;
        private readonly EmailSenderService _emailSenderService;
        private readonly NavigationManager _navigationManager;

        public TourController(DBContext context, EmailSenderService emailSenderService, NavigationManager navigationManager)
        {
            _context = context;
            _emailSenderService = emailSenderService;
            _navigationManager = navigationManager;
        }

        [HttpGet("tours-with-prices")]
        public async Task<ActionResult<List<Tour>>> GetToursWithPrices()
        {
            string CCA2 = Request.Query.FirstOrDefault(p => p.Key == "for-country").Value;
            if (string.IsNullOrEmpty(CCA2) || CCA2.Length != 2)
            {
                return BadRequest("Incorrect country code for the agency's country");
            }

            string startTourIdStr = Request.Query.FirstOrDefault(p => p.Key == "tour-id").Value;
            long startTourId = long.TryParse(startTourIdStr, out var parsedStartTourId) ? parsedStartTourId : 0;

            string toursAmountStr = Request.Query.FirstOrDefault(p => p.Key == "tours-amount").Value;
            int toursAmount = int.TryParse(toursAmountStr, out var parsedToursAmount) ? parsedToursAmount : 0;

            string? searchQuery = Request.Query.FirstOrDefault(p => p.Key == "searchQuery").Value;
            string? visitedCCA2 = Request.Query.FirstOrDefault(p => p.Key == "visitedCCA2").Value;

            if (!string.IsNullOrEmpty(visitedCCA2) && visitedCCA2.Length != 2)
            {
                return BadRequest("Incorrect country code for the visited country");
            }

            try
            {
                var query = _context.Tours
                    .Where(t => t.Agency.Country.CCA2 == CCA2/* && t.StartDate > DateTime.Now.Date.AddDays(7)*/)    //Commented to show the demo tour
                    .AsQueryable();

                if (!string.IsNullOrEmpty(visitedCCA2))
                {
                    query = query.Where(t => t.Cities.Any(c => c.Country.CCA2 == visitedCCA2));
                }
                else if (!string.IsNullOrEmpty(searchQuery))
                {
                    var searchTerms = searchQuery.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var term in searchTerms)
                    {
                        query = query.Where(t => t.Title.ToLower().Contains(term) ||
                                                 t.Description.ToLower().Contains(term) ||
                                                 t.Cities.Any(c => c.Name.ToLower().Contains(term) ||
                                                                   c.Country.Name.ToLower().Contains(term)) ||
                                                 t.Program.Any(a => a.Title.ToLower().Contains(term)));
                    }
                }

                query = query.OrderBy(t => t.StartDate);
                if (startTourId > 0)
                {
                    query = query.SkipWhile(t => t.TourId != startTourId);
                }

                var result = await query.Take(toursAmount)
                                        .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("delete/{tourId}")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> DeleteTour(long tourId)
        {
            try
            {
                var tour = await _context.Tours.FindAsync(tourId);

                if (tour != null)
                {
                    //using HttpClient httpClient = new();
                    //var imageDeleteResponse = await httpClient.GetAsync(_navigationManager.BaseUri + $"file/delete-file/{tour.ImagePath.Replace('/', '_')}");
                    //if (!imageDeleteResponse.IsSuccessStatusCode)
                    //{
                    //    return BadRequest($"Error deleting tour image: {await imageDeleteResponse.Content.ReadAsStringAsync()}");
                    //}

                    //foreach (var taskId in tour.ScheduledTasksIds)
                    //{
                    //    BackgroundJob.Delete(taskId);
                    //}

                    //_context.Entry(tour).State = EntityState.Deleted;
                    //await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("unsub/{tourId}-{userEmail}")]
        [Authorize]
        public async Task<ActionResult> UnsubUserFromTour(long tourId, string userEmail)
        {
            try
            {
                var requestedTour = await _context.Tours.Include(t => t.Participants).FirstOrDefaultAsync(t => t.TourId == tourId);

                if (requestedTour != null)
                {
                    await _context.Entry(requestedTour)
                        .Collection(t => t.Participants)
                        .LoadAsync();

                    foreach (var participant in requestedTour.Participants)
                    {
                        await _context.Entry(participant)
                            .Reference(p => p.PrimaryUser)
                            .LoadAsync();
                        await _context.Entry(participant)
                            .Collection(p => p.OtherUsers)
                            .LoadAsync();

                        foreach (var other in participant.OtherUsers)
                        {
                            await _context.Entry(other)
                                .Reference(o => o.User)
                                .LoadAsync();
                        }
                    }

                    foreach (var participants in requestedTour.Participants)
                    {
                        if (participants.PrimaryUser.Email == userEmail)
                        {
                            _context.Entry(participants).State = EntityState.Deleted;
                            break;
                        }

                        foreach (var other in participants.OtherUsers)
                        {
                            if (other.User != null && other.User.Email == userEmail)
                            {
                                _context.Entry(other).State = EntityState.Deleted;
                                break;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("agency/register")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<TourAgency?>> RegisterAgency(TourAgency agency)
        {
            // Check if the agency object is null
            if (agency == null)
            {
                return BadRequest("Agency object is null");
            }

            // Check if an agency with the same name already exists in the specified country
            if (_context.Agencies.Any(a => a.Name == agency.Name && a.Country.CCA2 == agency.Country.CCA2))
            {
                return BadRequest($"An agency with the name \"{agency.Name}\" already exists in {agency.Country.Name}");
            }

            // Find the user with the role of manager who is registering the agency
            var manager = await _context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name && u.Role.RoleName == "Manager");
            if (manager != null)
            {
                // Add the manager to the agency's list of managers
                agency.Managers.Add(manager);
            }

            try
            {
                // Attach the country object to the context if it is not already attached
                _context.Countries.Attach(agency.Country);

                // Add the agency to the context
                _context.Agencies.Add(agency);

                // Save changes to the database
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Handle exceptions (if an error occurs)
                string message = ex.ToString();
            }

            // Find and return the registered agency
            agency = await _context.Agencies.FirstOrDefaultAsync(a => a.Name == agency.Name && a.Country.CCA2 == agency.Country.CCA2);
            return Ok(agency);
        }


        [HttpGet("agency/get-by-manager")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<TourAgency?>> GetAgencyByManager()
        {
            try
            {
                // Find the agency managed by the user with the current email
                var agency = await _context.Agencies
                    .Include(a => a.Country) // Include country information
                    .Include(a => a.Managers) // Include managers information
                    .AsSplitQuery() // Execute queries separately for performance optimization
                    .FirstOrDefaultAsync(a => a.Managers.Any(m => m.Email == User.Identity.Name)); // Check if the manager with the current email is in the managers list

                return Ok(agency); // Return the found agency
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString()); // Return an error message in case of exception
            }
        }


        [HttpGet("shedule-tour-tasks/{tourId}")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> SetScheduledTasks(long tourId)
        {
            var tour = await _context.Tours.FindAsync(tourId);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                tour.ScheduledTasksIds = new()
                    {
                        BackgroundJob.Schedule(
                            () => BookActivities(tourId),
                            tour.StartDate.Value.AddDays(-7)),
                        BackgroundJob.Schedule(
                            () => NotifyParticipants(tourId),
                            tour.StartDate.Value.AddDays(-3)),
                        BackgroundJob.Schedule(
                            () => RequestFeedback(tourId),
                            tour.EndDate.Value.AddDays(1)),
                        BackgroundJob.Schedule(
                            () => DeleteTour(tourId),
                            tour.EndDate.Value.AddDays(1))
                    };

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(ex.Message);
            }

            return Ok();
        }

        public async Task BookActivities(long tourId)
        {
            try
            {
                var reqTour = await _context.Tours
                                            .Include(t => t.Agency)
                                            .ThenInclude(a => a.Managers)
                                            .Include(t => t.Participants)
                                            .Include(t => t.Program)
                                            .AsSplitQuery()
                                            .FirstOrDefaultAsync(t => t.TourId == tourId);

                var toursOrSides = reqTour.Program.Where(a => a.ActType == Activity.ActivityType.SIDE).ToList();
                List<(string SideName, string Link)> SidesLinks = new();
                foreach (var act in toursOrSides)
                {
                    if (act is TourOrSideActivity side && side.BookingLink != null && side.PriceAmount > 0)
                    {
                        SidesLinks.Add((side.Title, side.BookingLink));
                    }
                }

                User? manager = null;
                foreach (var participant in reqTour.Participants)
                {
                    await _context.Entry(participant)
                        .Reference(p => p.PrimaryUser)
                        .LoadAsync();

                    if (reqTour.Agency.Managers.Any(m => m.Email == participant.PrimaryUser.Email))
                    {
                        manager = participant.PrimaryUser;
                        break;
                    }
                }

                if (manager != null)
                {
                    var model = new BookingRequestNotificationModel
                    {
                        Name = manager.FirstName,
                        StartDate = reqTour.StartDate.Value,
                        TourName = reqTour.Title,
                        TransferBookingLink = reqTour.Program.Any(a => a.ActType == Activity.ActivityType.TRANS) ? _navigationManager.BaseUri + $"transfers-booking/{reqTour.TourId}" : null,
                        SidesLinks = SidesLinks
                    };
                    await _emailSenderService.SendEmailAsync(manager.Email, "Make a Booking", "EmailTemplates/BookingRequestNotification.cshtml", model);
                }
            }
            catch
            {
                // do nothing
            }
        }

        public async Task NotifyParticipants(long tourId)
        {
            try
            {
                var reqTour = await _context.Tours.Include(t => t.Participants).FirstOrDefaultAsync(t => t.TourId == tourId);

                if (reqTour != null)
                {
                    foreach (var participant in reqTour.Participants)
                    {
                        await _context.Entry(participant)
                            .Reference(p => p.PrimaryUser)
                            .LoadAsync();

                        await _context.Entry(participant)
                            .Collection(p => p.OtherUsers)
                            .LoadAsync();

                        foreach (var other in participant.OtherUsers)
                        {
                            await _context.Entry(other)
                                .Reference(o => o.User)
                                .LoadAsync();
                        }
                    }

                    var participants = reqTour.Participants.SelectMany(pu =>
                    {
                        List<User> parts = new()
                        {
                        pu.PrimaryUser
                        };
                        parts.AddRange(pu.OtherUsers.Where(o => o.User != null).Select(o => o.User) ?? new List<User>());
                        return parts;
                    }).ToList();

                    foreach (var participant in participants)
                    {
                        var model = new UpcomingTourNotificationModel
                        {
                            Name = participant.FirstName,
                            StartDate = reqTour.StartDate.Value,
                            TourName = reqTour.Title
                        };
                        await _emailSenderService.SendEmailAsync(participant.Email, "Upcoming Tour", "EmailTemplates/UpcomingTourNotification.cshtml", model);
                    }
                }
            }
            catch 
            {
                // do nothing
            }
        }

        public async Task RequestFeedback(long tourId)
        {
            try
            {
                var reqTour = await _context.Tours
                                                .Include(t => t.Participants)
                                                .Include(t => t.Agency)
                                                .AsSplitQuery()
                                                .FirstOrDefaultAsync(t => t.TourId == tourId);

                if (reqTour != null)
                {
                    foreach (var participant in reqTour.Participants)
                    {
                        await _context.Entry(participant)
                            .Collection(p => p.OtherUsers)
                            .LoadAsync();

                        foreach (var other in participant.OtherUsers)
                        {
                            await _context.Entry(other)
                                .Reference(o => o.User)
                                .LoadAsync();
                        }
                    }

                    var participants = reqTour.Participants.SelectMany(pu =>
                    {
                        List<User> parts = new()
                        {
                        pu.PrimaryUser
                        };
                        parts.AddRange(pu.OtherUsers.Where(o => o.User != null).Select(o => o.User) ?? new List<User>());
                        return parts;
                    }).ToList();

                    foreach (var participant in participants)
                    {
                        var model = new FeedbackRequestNotificationModel
                        {
                            Name = participant.FirstName,
                            TourName = reqTour.Title,
                            AgencyInfo = $"{reqTour.Agency.Name} (<a href=\"{reqTour.Agency.Email}\">{reqTour.Agency.Email}</a>, {string.Join(", ", reqTour.Agency.PhoneNumbers)})"
                        };
                        await _emailSenderService.SendEmailAsync(participant.Email, "Rate the Tour", "EmailTemplates/FeedbackRequestNotification.cshtml", model);
                    }
                }
            }
            catch
            {
                // do nothing
            }
        }
    }
}
