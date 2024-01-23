using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Travel_App_Web.Data;
using Travel_App_Web.Models;

namespace Travel_App_Web.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountController : Controller
    {
        private DBContext _context;

        public AccountController(DBContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("reg")]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
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

                    return Ok("You have successfully registered!");
                    //return RedirectToAction("Index", "Home");
                }
                else
                    //ModelState.AddModelError(string.Empty, "This Email is already being used!");
                    return BadRequest("This Email is already being used!");
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("auth")]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                User? user = await _context.Users.
                    Include(user => user.Role).
                    FirstOrDefaultAsync(user =>  user.Email == model.Email);

                if (user != null)
                {
                    if (user.VerifyPassword(model.Password))
                    {
                        await Authenticate(user);

                        return Ok("You have logged in successfully");
                    }
                    else
                        return BadRequest("Invalid login or email.");
                }
                else
                    //ModelState.AddModelError(string.Empty, "Invalid login or email.");
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
        public ActionResult Index()
        {
            var user = User.Identity.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok($"Current User: {user}, Role: {role}");
        }
    }
}
