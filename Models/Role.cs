using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Travel_App_Web.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RoleName { get; set; }

        public List<User>? Users { get; set; }
    }
}
