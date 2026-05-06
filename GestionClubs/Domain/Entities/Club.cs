using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.Entities
{
    public class Club:BaseEntity
    {
        [Required]
        public required string Name { get; set; }
        [Required]
        public required string Description { get; set; }
        public Collection<string> Documents { get; set; } = [];
        
        public Collection<Member> Members { get; set; } = [];
        public Collection<Adhesion> Adhesions { get; set; } = [];
        public Collection<Annoucement> Annoucements { get; set; } = [];

    }
}
