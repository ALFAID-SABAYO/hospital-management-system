using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital.Models
{
    public class Appointment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int appointment_id { get; set; }
        public int? patient_id { get; set; }
        public int? doctor_id { get; set; }
        public DateTime appointment_date { get; set; }
        public string status { get; set; }

        // public Billing Billing { get; set; }  // ❌ COMMENT YA DELETE KARO
        public MedicalRecord? MedicalRecord { get; set; }
        public Billing? Billing { get; set; }

        public Patient? Patient { get; set; }
        public Doctor? Doctor { get; set; }

    }
}
