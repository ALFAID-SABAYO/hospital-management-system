using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital.Models
{
    public class MedicalRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int record_id { get; set; }
        public int? appointment_id { get; set; }
        public DateTime visit_date { get; set; }
        public string diagnosis { get; set; } = string.Empty;
        public string note { get; set; } = string.Empty;

        public Appointment? Appointment { get; set; }
        public ICollection<Prescription>? Prescriptions { get; set; }
        public ICollection<TestResult>? TestResults { get; set; }
    }
}