using GestionClubs.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.DTOs
{
    public class UpdateMemberPostDTO
    {
        [Required]
        public required int MemberId { get; set; }
        [Required]
        [EnumDataType(typeof(ClubPost))]
        public required ClubPost NewPost { get; set; }
    }
}
