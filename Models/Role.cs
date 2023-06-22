using System.ComponentModel.DataAnnotations;

namespace UserService.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RoleName { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
