using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital.Models
{
    public class Prescription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int prescription_id { get; set; }
        public int? record_id { get; set; }
        public string medication_name { get; set; }
        public string dosage { get; set; }
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }

        public MedicalRecord? MedicalRecord { get; set; }
    }
}