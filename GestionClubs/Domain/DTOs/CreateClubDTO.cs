using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.DTOs
{
    public class CreateClubDTO
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public required string Name { get; set; }
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public required string Description { get; set; }
        [Required]
        [EmailAddress]
        public required string Email { get; set; }
        public Collection<string> Documents { get; set; } = new Collection<string>();
    }

}
