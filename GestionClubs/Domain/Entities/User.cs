using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.Entities
{
    public class User:BaseEntity
    {
        [Required]
        [EmailAddress] 
        public required string Email { get; set; }
        [Required]
        public required string FirstName { get; set; }
        [Required]
        public required string LastName { get; set; }
        public List<Event> Events { get; set; } = [];
    }
}
