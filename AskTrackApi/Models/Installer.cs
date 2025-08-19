using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AskTrackApi.Models
{
    [Table("installers")]
    public class Installer
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("Int_name")]
        public string Int_name { get; set; }

        [Column("Int_number")]
        public string Int_number { get; set; }

        [Required]
        [Column("Int_pass")]
        public string Int_pass { get; set; }

        [Required]
        [Column("Int_code")]
        public string Int_code { get; set; }

        [Column("Int_type")]
        public string? Int_type { get; set; }

        [Column("Int_Branch")]
        public string? Int_Branch { get; set; } 

        [Column("Int_City")]
        public string? Int_City { get; set; }
    }
}