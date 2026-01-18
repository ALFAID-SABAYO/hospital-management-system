using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital.Models
{
    public class Doctor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int doctor_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string specialization { get; set; }
        public string contact_number { get; set; }
        public string email { get; set; }
        public string status { get; set; }
        public int? department_id { get; set; }
        [NotMapped]
        public string FullName => $"{first_name} {last_name}";


        public Department? Department { get; set; }
        public ICollection<DoctorSchedule>? DoctorSchedules { get; set; }
        public ICollection<Appointment>? Appointments { get; set; }
    }
}