using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Data;
using System.Security.Claims;
using Travel_App_Web.Data;

namespace Travel_App_Web.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountController : Controller
    {
        private readonly DBContext _context;

        public AccountController(DBContext context)
        {
            _context = context;
            _context.ChangeTracker.LazyLoadingEnabled = false;
        }

        [HttpPost("update")]
        [Authorize]
        public async Task<ActionResult<User>> UpdateUser(RegisterModel model)
        {
            User user = new()
            {
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Country = model.Country,
                DateOfBirth = model.DateOfBirth,
                Gender = model.Gender,
                MiddleName = model.MiddleName,
                Phone = model.Phone
            };

            var reqUser = await _context.Users
                                    .AsNoTracking()
                                    .Include(u => u.Chats)
                                    .Include(u => u.Role)
                                    .FirstOrDefaultAsync(u => u.Email == user.Email);

            user.Chats = reqUser.Chats;
            user.Role = reqUser.Role;
            user.PasswordHash = reqUser.PasswordHash;

            if (model.Password == "Unchanged@12345")
                user.SetPassword(model.Password);

            try
            {
                _context.Attach(user);
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                user.PasswordHash = string.Empty;

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("delete/{email}")]
        [Authorize]
        public async Task<ActionResult> DeleteUser(string email)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var user = await _context.Users.Include(u => u.Chats).FirstOrDefaultAsync(u => u.Email == email);

                    if (user == null)
                        return Ok();

                    var partUnits = _context.ParticipantUnits.Where(pu => pu.PrimaryUser.Email == email).ToList();

                    foreach (var unit in partUnits)
                    {
                        _context.Entry(unit).State = EntityState.Deleted;
                    }
                    await _context.SaveChangesAsync();

                    var participants = _context.Participants.Where(p => p.User != null && p.User.Email == email);

                    foreach (var part in participants)
                    {
                        part.Email = user.Email;
                        part.Phone = user.Phone;
                        part.DateOfBirth = user.DateOfBirth;
                        part.FirstName = user.FirstName;
                        part.LastName = user.LastName;
                        part.MiddleName = user.MiddleName;
                        part.Gender = user.Gender;
                        part.User = null;
                    }
                    await _context.SaveChangesAsync();

                    _context.Entry(user.Chats).State = EntityState.Deleted;
                    await _context.SaveChangesAsync();

                    _context.Entry(user).State = EntityState.Deleted;
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(ex.Message);
                }
            }
        }

        [HttpPost("reg")]
        [AllowAnonymous]
        public async Task<ActionResult<User>> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                User? user = await _context.Users.FirstOrDefaultAsync(user => user.Email == model.Email);

                if (user == null)
                {
                    user = new User
                    {
                        Email = model.Email,
                        FirstName = model.FirstName,
                        MiddleName = model.MiddleName,
                        LastName = model.LastName,
                        DateOfBirth = model.DateOfBirth,
                        Gender = model.Gender,
                        Phone = model.Phone,
                        Country = model.Country
                    };

                    user.SetPassword(model.Password);

                    Role? role = await _context.Roles.FirstOrDefaultAsync(role => role.RoleName == "User");                    
                    if (role != null) 
                        user.Role = role;

                    try
                    {
                        if (user.Country != null)
                            _context.Countries.Attach(user.Country);
                        _context.Users.Add(user);

                        await _context.SaveChangesAsync();
                        await Authenticate(user.Email, user.Role.RoleName);

                        user = await _context.Users
                            .Include(user => user.Role)
                            .FirstOrDefaultAsync(user => user.Email == model.Email);

                        user.PasswordHash = string.Empty;
                        
                        return Ok(user);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest($"An error occurred while registering the user: {ex.Message}");
                    }
                }
                else
                    return BadRequest("This email is already in use");
            }
            return View(model);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<User>> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                // Searching for the user by email including role and country
                User? user = await _context.Users
                    .Include(user => user.Role)
                    .Include(user => user.Country)
                    .FirstOrDefaultAsync(user => user.Email == model.Email);

                if (user != null && user.VerifyPassword(model.Password))
                {
                    // Authenticating the user
                    await Authenticate(user.Email, user.Role.RoleName);

                    user.PasswordHash = string.Empty; // Clearing the password hash before returning the user

                    return Ok(user); // Returning a successful result with the user
                }
                else
                {
                    return BadRequest("Invalid login or email"); // Returning an error for invalid credentials
                }
            }
            return BadRequest("Check the form data"); // Returning an error for an invalid model
        }

        [HttpGet("auth/{email}-{roleName}")]
        [AllowAnonymous]
        public async Task<ActionResult> Auth(string email, string roleName)
        {
            try
            {
                await Authenticate(email, roleName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok();
        }

        private async Task Authenticate(string email, string roleName)
        {
            // Creating a list of user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, email), // User name claim
                new Claim(ClaimsIdentity.DefaultRoleClaimType, roleName) // User role claim
            };

            // Creating a ClaimsIdentity object based on the claims
            ClaimsIdentity id = new(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            // Authenticating the user using cookies
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }


        [HttpGet("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok();
        }


        [HttpGet("current-user")]
        [Authorize]
        public async Task<ActionResult<User>> GetUser()
        {
            string? userEmail = User.Identity?.Name;

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Chats)
                .ThenInclude(chat => chat.Messages)
                .FirstOrDefaultAsync(user => user.Email == userEmail);

            if (user != null)
            {
                return Ok(user);
            }
            return BadRequest("Error retrieving user data");
        }

        [HttpGet("current-user-chats")]
        [Authorize]
        public ActionResult<List<Chat>> GetUserChats()
        {
            string? userEmail = User.Identity?.Name;
            
            if (userEmail != null)
            {
                var chats = _context.Chats
                    .Include(c => c.Messages)
                    .Where(c => c.Emails.Contains(userEmail))
                    .ToList();

                return Ok(chats);
            }
            return BadRequest("Authorization error");
        }

        [HttpGet("get-cookies")]
        [Authorize]
        public IActionResult GetCookies() => Ok(Request.Cookies);
    }
}
