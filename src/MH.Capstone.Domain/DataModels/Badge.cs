using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataModels
{

    [Table("Sighting")]
    public class Badge
    {
        // Turn into GUID?
        int BadgeID { get; set; }

        // Add in a userID field as a foreign key?

        int PointValue { get; set; }

        public byte[]? badgeIcon { get; set; }
        

    }

}