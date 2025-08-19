using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskTrackApi.Models.GPS
{
    [Keyless]
    [Table("UserInfo", Schema = "dbo")] // Optional, but good practice
    public class UserInfo
    {
        [Column("Device ID")]
      
        public string DeviceId { get; set; }

        [Column("GroupAccount")]
        public string GroupAccount { get; set; }

        [Column("isinstalled")]
        public bool? isinstalled { get; set; }

        [Column("PhoneNumber")]
        public string PhoneNumber { get; set; }

       
    }
}
