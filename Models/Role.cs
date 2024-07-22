using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required, MaxLength(20), Column(TypeName = "varchar")]
        public string RoleName { get; set; } = null!;
    }
}
