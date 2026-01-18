using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital.Models
{
    public class TestResult
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int test_id { get; set; }

        // Foreign key
        public int? record_id { get; set; }

        public string test_name { get; set; }

        public string result { get; set; }

        public DateTime? test_date { get; set; }
        public MedicalRecord? MedicalRecord { get; set; }


    }
}
