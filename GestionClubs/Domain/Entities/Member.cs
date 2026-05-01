using GestionClubs.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.Entities
{
    public class Member : BaseEntity
    {
        public int ClubId { get; set; }
        public Club? Club { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required ClubPost PostInClub {  get; set; }
    }
}
