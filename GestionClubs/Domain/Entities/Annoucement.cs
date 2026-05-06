using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.Entities
{
    public class Annoucement : BaseEntity
    {
        public required int ClubId { get; set; }
        public Club? Club { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public bool IsPublic { get; set; }
    }
}
