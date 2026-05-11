using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.Entities
{
    public class Event : BaseEntity
    {
        public int ClubId { get; set; }
        public Club? Club { get; set; }
        public required string Title { get; set; } 
        public required string Description { get; set; }
        public bool IsPublic { get; set; }
        public string? Location { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<User> Participent { get; set; } = [];
    }
}
