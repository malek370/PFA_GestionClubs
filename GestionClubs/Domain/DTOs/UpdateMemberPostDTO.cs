using GestionClubs.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.DTOs
{
    public class UpdateMemberPostDTO
    {
        public int MemberId { get; set; }
        public ClubPost NewPost { get; set; }
    }
}
