using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.DTOs
{
    public class GetMemberDTO
    {
        public int Id { get; set; }
        public string ClubName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PostInClub { get; set; } = "";
    }
}
