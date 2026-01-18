namespace Hospital.Models
{
    public class DepartmentWithDoctorCountVM
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public int DoctorCount { get; set; }
    }
}
