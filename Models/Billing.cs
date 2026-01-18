using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital.Models
{
    public class Billing
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int bill_id { get; set; }
        
        public int? appointment_id { get; set; }  // ✅ ADD THIS LINE
        public decimal total_amount { get; set; }
        public string payment_status { get; set; }
        public DateTime? payment_date { get; set; }

        public Patient? Patient { get; set; }
        public Appointment? Appointment { get; set; }
       
    }
}