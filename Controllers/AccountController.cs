using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Security.Claims;
using Travel_App_Web.Data;
using Travel_App_Web.Models;

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

        [HttpPost("reg")]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
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
                        Phone = model.Phone
                    };

                    user.SetPassword(model.Password);

                    Role? role = await _context.Roles.FirstOrDefaultAsync(role => role.RoleName == "User");
                    
                    if (role != null) 
                        user.Role = role;

                    _context.Users.Add(user);

                    await _context.SaveChangesAsync();
                    await Authenticate(user);

                    user = await _context.Users
                        .Include(user => user.Role)
                        .FirstOrDefaultAsync(user => user.Email == model.Email);

                    return Ok(user);
                    //return RedirectToAction("Index", "Home");
                }
                else
                    //ModelState.AddModelError(string.Empty, "This Email is already being used!");
                    return BadRequest("This Email is already being used!");
            }
            return View(model);
        }

        [HttpPost("auth")]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult<User>> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                User? user = await _context.Users.
                    Include(user => user.Role).
                    FirstOrDefaultAsync(user =>  user.Email == model.Email);

                if (user != null && user.VerifyPassword(model.Password))
                {
                    await Authenticate(user);
                    return Ok(user);
                }
                else
                    return BadRequest("Invalid login or email.");
            }
            return View(model);
        }

        private async Task Authenticate(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role.RoleName)
            };

            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", 
                ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }

        [HttpGet("current-user")]
        [Authorize]
        public async Task<ActionResult<User>> GetUser()
        {
            string? userEmail = User.Identity?.Name;

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Chats)
                .ThenInclude(chat => chat.InterlocutorsEmails)
                .Include(u => u.Chats)
                .ThenInclude(chat => chat.Messages)
                .FirstOrDefaultAsync(user => user.Email == userEmail);

            if (user != null)
            {
                return Ok(user);
            }
            return BadRequest("Error retrieving user data.");
        }

        [HttpGet("current-user-chats")]
        [Authorize]
        public ActionResult<List<Chat>> GetUserChats()
        {
            string? userEmail = User.Identity?.Name;
            
            if (userEmail != null)
            {
                var chats = _context.Chats
                    .Include(c => c.InterlocutorsEmails)
                    .Include(c => c.Messages)
                    .Where(c => c.InterlocutorsEmails.Any(e => e.Email == userEmail));


                if (!chats.IsNullOrEmpty())
                {
                    return Ok(chats);
                }
                return BadRequest("Didn't find the user");
            }
            return BadRequest("Authorization error");
        }

        [HttpGet("get-cookies")]
        public IActionResult GetCookies()
        {
            var cookies = Request.Cookies;
            return Ok(cookies);
        }
    }
}
