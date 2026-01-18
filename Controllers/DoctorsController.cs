using Hospital.Data;
using Hospital.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hospital.Controllers
{
    [Authorize(Roles = "Admin,Doctor")] // Only Admin and Doctor can access this controller
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== DOCTORS LIST ====================
        // GET: Doctors
        [Authorize(Roles = "Admin")] // Only Admin can see the full doctors list
        public async Task<IActionResult> Index()
        {
            var doctors = await _context.Doctors
                .Include(d => d.Department)
                .ToListAsync();
            return View(doctors);
        }

        // ==================== DOCTOR DETAILS ====================
        // GET: Doctors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .Include(d => d.DoctorSchedules)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Patient)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                        .ThenInclude(mr => mr.Prescriptions)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                        .ThenInclude(mr => mr.TestResults)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Billing)
                .FirstOrDefaultAsync(m => m.doctor_id == id);

            if (doctor == null) return NotFound();

            // Calculate statistics
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var todayAppointments = doctor.Appointments?.Count(a => a.appointment_date.Date == today) ?? 0;
            var monthAppointments = doctor.Appointments?.Count(a => a.appointment_date >= monthStart && a.appointment_date <= monthEnd) ?? 0;
            var completedAppointments = doctor.Appointments?.Count(a => a.status == "Completed") ?? 0;
            var totalPatients = doctor.Appointments?.Select(a => a.patient_id).Distinct().Count() ?? 0;

            ViewBag.TodayAppointments = todayAppointments;
            ViewBag.MonthAppointments = monthAppointments;
            ViewBag.CompletedAppointments = completedAppointments;
            ViewBag.TotalPatients = totalPatients;
            ViewBag.Today = today.ToString("MMMM dd, yyyy");

            // Upcoming appointments
            var upcomingAppointments = doctor.Appointments?
                .Where(a => a.appointment_date > DateTime.Now && a.status != "Cancelled")
                .OrderBy(a => a.appointment_date)
                .Take(5)
                .ToList() ?? new List<Appointment>();

            ViewBag.UpcomingAppointments = upcomingAppointments;

            return View(doctor);
        }

        // ==================== CREATE DOCTOR ====================
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["department_id"] = new SelectList(_context.Departments, "department_id", "name");
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("doctor_id,first_name,last_name,specialization,contact_number,email,status,department_id")] Doctor doctor)
        {
            if (await _context.Doctors.AnyAsync(d => d.email == doctor.email))
            {
                ModelState.AddModelError("email", "Email already exists in the system.");
            }

            if (ModelState.IsValid)
            {
                doctor.status = string.IsNullOrEmpty(doctor.status) ? "Active" : doctor.status;
                doctor.first_name = CapitalizeFirstLetter(doctor.first_name?.Trim());
                doctor.last_name = CapitalizeFirstLetter(doctor.last_name?.Trim());
                doctor.specialization = CapitalizeFirstLetter(doctor.specialization?.Trim());

                _context.Add(doctor);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Doctor added successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["department_id"] = new SelectList(_context.Departments, "department_id", "name", doctor.department_id);
            return View(doctor);
        }

        // ==================== EDIT DOCTOR ====================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();

            ViewData["department_id"] = new SelectList(_context.Departments, "department_id", "name", doctor.department_id);
            return View(doctor);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("doctor_id,first_name,last_name,specialization,contact_number,email,status,department_id")] Doctor doctor)
        {
            if (id != doctor.doctor_id) return NotFound();

            if (await _context.Doctors.AnyAsync(d => d.email == doctor.email && d.doctor_id != id))
            {
                ModelState.AddModelError("email", "Email already exists in the system.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Doctor information updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.doctor_id)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["department_id"] = new SelectList(_context.Departments, "department_id", "name", doctor.department_id);
            return View(doctor);
        }

        // ==================== DELETE DOCTOR ====================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(m => m.doctor_id == id);

            if (doctor == null) return NotFound();
            return View(doctor);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Doctor deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // ==================== DOCTOR DASHBOARD ====================
        public async Task<IActionResult> Dashboard(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Patient)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Billing)
                .FirstOrDefaultAsync(m => m.doctor_id == id);

            if (doctor == null) return NotFound();

            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var allAppointments = doctor.Appointments?.ToList() ?? new List<Appointment>();
            var todayAppointments = allAppointments.Where(a => a.appointment_date.Date == today).ToList();
            var upcomingAppointments = allAppointments.Where(a => a.appointment_date > today && a.status != "Cancelled" && a.status != "Completed").OrderBy(a => a.appointment_date).Take(10).ToList();
            var pastAppointments = allAppointments.Where(a => a.appointment_date < today && (a.status == "Scheduled" || a.status == "Confirmed")).OrderByDescending(a => a.appointment_date).Take(10).ToList();

            ViewBag.TodayAppointments = todayAppointments.Count;
            ViewBag.MonthAppointments = allAppointments.Count(a => a.appointment_date >= monthStart && a.appointment_date <= monthEnd);
            ViewBag.CompletedAppointments = allAppointments.Count(a => a.status == "Completed");
            ViewBag.TotalPatients = allAppointments.Select(a => a.patient_id).Distinct().Count();
            ViewBag.TotalRevenue = allAppointments.Where(a => a.Billing != null).Sum(a => a.Billing.total_amount);
            ViewBag.Today = today.ToString("MMMM dd, yyyy");
            ViewBag.UpcomingAppointments = upcomingAppointments;
            ViewBag.PastAppointments = pastAppointments;
            ViewBag.TodayAppointmentsList = todayAppointments.OrderBy(a => a.appointment_date).ToList();

            return View(doctor);
        }

        // ==================== HELPER METHODS ====================
        private DateTime GetStartOfWeek(DateTime date)
        {
            int diff = date.DayOfWeek - DayOfWeek.Monday;
            if (diff < 0) diff += 7;
            return date.AddDays(-diff).Date;
        }

        private string CapitalizeFirstLetter(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.doctor_id == id);
        }

        // ==================== DOCTOR PERFORMANCE ====================
        public async Task<IActionResult> Performance(int? id, DateTime? fromDate, DateTime? toDate)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Patient)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Billing)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                        .ThenInclude(mr => mr.Prescriptions)
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                        .ThenInclude(mr => mr.TestResults)
                .FirstOrDefaultAsync(m => m.doctor_id == id);

            if (doctor == null) return NotFound();

            var startDate = fromDate ?? DateTime.Today.AddMonths(-3);
            var endDate = toDate ?? DateTime.Today;

            var appointments = doctor.Appointments?
                .Where(a => a.appointment_date >= startDate && a.appointment_date <= endDate)
                .ToList() ?? new List<Appointment>();

            ViewBag.Doctor = doctor;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.TotalAppointments = appointments.Count;
            ViewBag.CompletedAppointments = appointments.Count(a => a.status == "Completed");
            ViewBag.CancelledAppointments = appointments.Count(a => a.status == "Cancelled");
            ViewBag.NoShowAppointments = appointments.Count(a => a.status == "No Show");

            var completionRate = ViewBag.TotalAppointments > 0 
                ? Math.Round((double)ViewBag.CompletedAppointments / (double)ViewBag.TotalAppointments * 100, 1) 
                : 0;
            var cancellationRate = ViewBag.TotalAppointments > 0 
                ? Math.Round((double)ViewBag.CancelledAppointments / (double)ViewBag.TotalAppointments * 100, 1) 
                : 0;

            ViewBag.CompletionRate = completionRate;
            ViewBag.CancellationRate = cancellationRate;
            ViewBag.TotalRevenue = appointments
                .Where(a => a.Billing != null)
                .Sum(a => a.Billing.total_amount);

            ViewBag.PrescriptionsCount = appointments
                .Where(a => a.MedicalRecord != null && a.MedicalRecord.Prescriptions != null)
                .SelectMany(a => a.MedicalRecord.Prescriptions)
                .Count();

            ViewBag.TestsCount = appointments
                .Where(a => a.MedicalRecord != null && a.MedicalRecord.TestResults != null)
                .SelectMany(a => a.MedicalRecord.TestResults)
                .Count();

            // Calculate monthly statistics
            var monthlyStats = appointments
                .GroupBy(a => new { a.appointment_date.Year, a.appointment_date.Month })
                .Select(g => new MonthlyStat
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                    Total = g.Count(),
                    Completed = g.Count(a => a.status == "Completed"),
                    Revenue = g.Where(a => a.Billing != null).Sum(a => a.Billing.total_amount)
                })
                .OrderByDescending(s => s.Month)
                .ToList();

            ViewBag.MonthlyStats = monthlyStats;

            return View();
        }

        // ==================== VIEW MODEL CLASSES ====================
        public class MonthlyStat
        {
            public string Month { get; set; } = string.Empty;
            public int Total { get; set; }
            public int Completed { get; set; }
            public decimal Revenue { get; set; }
        }
    }
}
