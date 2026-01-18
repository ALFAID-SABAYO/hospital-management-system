using Hospital.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class MedicalRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int record_id { get; set; }
    public int? appointment_id { get; set; }  // ✅ Keep this
    public DateTime visit_date { get; set; }
    public string diagnosis { get; set; }
    public string note { get; set; }

    public Appointment? Appointment { get; set; }  // ✅ Keep this
    public ICollection<Prescription>? Prescriptions { get; set; }

  
    public ICollection<TestResult>? TestResults { get; set; }
}