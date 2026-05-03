using GestionClubs.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.Entities
{
    public class Member : BaseEntity
    {
        public int ClubId { get; set; }
        public Club? Club { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public required ClubPost PostInClub {  get; set; }
    }
}
