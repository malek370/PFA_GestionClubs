using GestionClubs.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.DTOs
{
    public class CreateAdhesionDTO
    {
        [Required]
        public required int ClubId { get; set; }
        
    }
}
