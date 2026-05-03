using GestionClubs.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Domain.DTOs
{
    public class GetAdhesionDTO
    {
        public int Id { get; set; }
        public UserDTO? User { get; set; }
        public string Status { get; set; }="";
        public string ClubName { get; set; }="";

    }
}
