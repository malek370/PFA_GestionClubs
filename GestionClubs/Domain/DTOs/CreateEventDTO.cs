using GestionClubs.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.DTOs
{
    public class CreateEventDTO
    {
        public int ClubId { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public bool IsPublic { get; set; } = false;
        public string? Location { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}
