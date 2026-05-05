using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.DTOs
{
    public class GetAnnoucementDTO
    {
        public int Id {  get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required string ClubName { get; set; }

    }
}
