using Hospital.Data;
using Hospital.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hospital.Controllers
{
    // Only logged-in users with these roles can access appointments
    [Authorize(Roles = "Admin,Doctor,Patient,Receptionist")]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // 📅 CALENDAR VIEW (Admin: All | Doctor: Own | Patient: Own)
        // =====================================================
        public async Task<IActionResult> Calendar()
        {
            // Base query with relations
            var appointmentsQuery = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsQueryable();

            // Logged-in user ID
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                // Patient sees only their appointments
                if (User.IsInRole("Patient"))
                    appointmentsQuery = appointmentsQuery.Where(a => a.patient_id == userId);

                // Doctor sees only their appointments
                else if (User.IsInRole("Doctor"))
                    appointmentsQuery = appointmentsQuery.Where(a => a.doctor_id == userId);
            }

            var appointments = await appointmentsQuery.ToListAsync();

            // Convert appointments to calendar events
            var events = appointments.Select(a => new CalendarEvent
            {
                id = a.appointment_id,
                title = $"Patient: {a.Patient?.first_name} {a.Patient?.last_name} - Dr: {a.Doctor?.first_name} {a.Doctor?.last_name}",
                start = a.appointment_date.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = a.appointment_date.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                color = a.status switch
                {
                    "Cancelled" => "red",
                    "Completed" => "green",
                    "Scheduled" => "blue",
                    "Confirmed" => "blue",
                    "In Progress" => "orange",
                    _ => "gray"
                },
                url = Url.Action("Details", "Appointments", new { id = a.appointment_id })
            }).ToList();

            return View(events); // MUST always return a value
        }

        // =====================================================
        // 📋 INDEX VIEW (Role-based filtering)
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var query = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .AsQueryable();

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                if (User.IsInRole("Patient"))
                    query = query.Where(a => a.patient_id == userId);
                else if (User.IsInRole("Doctor"))
                    query = query.Where(a => a.doctor_id == userId);
            }

            return View(await query
                .OrderByDescending(a => a.appointment_date)
                .ToListAsync());
        }

        // =====================================================
        // 🔍 DETAILS
        // =====================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.appointment_id == id);

            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // =====================================================
        // ➕ CREATE (GET)
        // =====================================================
        public IActionResult Create()
        {
            // For Admin: load all patients
            if (User.IsInRole("Admin"))
            {
                ViewBag.patient_id = new SelectList(_context.Patients, "patient_id", "full_name");
            }
            else if (User.IsInRole("Patient"))
            {
                // Get current patient based on logged-in user's email
                var patient = _context.Patients.FirstOrDefault(p => p.email == User.Identity.Name);
                if (patient != null)
                {
                    // Only include the current patient
                    ViewBag.patient_id = new SelectList(new List<Patient> { patient }, "patient_id", "full_name");
                }
            }

            // Load doctors
            ViewBag.doctor_id = new SelectList(_context.Doctors, "doctor_id", "full_name");

            return View();
        }


        // =====================================================
        // ➕ CREATE (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            if (User.IsInRole("Patient"))
            {
                var patient = _context.Patients.FirstOrDefault(p => p.email == User.Identity.Name);
                if (patient != null)
                {
                    appointment.patient_id = patient.patient_id; // Ensure patient can't change ID
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // reload dropdowns
            ViewBag.doctor_id = new SelectList(_context.Doctors, "doctor_id", "full_name", appointment.doctor_id);
            if (User.IsInRole("Admin"))
            {
                ViewBag.patient_id = new SelectList(_context.Patients, "patient_id", "full_name", appointment.patient_id);
            }
            else if (User.IsInRole("Patient"))
            {
                var patient = _context.Patients.FirstOrDefault(p => p.email == User.Identity.Name);
                ViewBag.patient_id = new SelectList(new List<Patient> { patient }, "patient_id", "full_name", patient.patient_id);
            }

            return View(appointment);
        }


        // =====================================================
        // ✏️ EDIT (GET)
        // =====================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            ViewData["doctor_id"] = new SelectList(_context.Doctors, "doctor_id", "FullName", appointment.doctor_id);
            ViewData["patient_id"] = new SelectList(_context.Patients, "patient_id", "FullName", appointment.patient_id);

            return View(appointment);
        }

        // =====================================================
        // ✏️ EDIT (POST)
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Appointment appointment)
        {
            if (id != appointment.appointment_id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    appointment.appointment_date =
                        DateTime.SpecifyKind(appointment.appointment_date, DateTimeKind.Local)
                                .ToUniversalTime();

                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Appointments.Any(e => e.appointment_id == id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["doctor_id"] = new SelectList(_context.Doctors, "doctor_id", "FullName", appointment.doctor_id);
            ViewData["patient_id"] = new SelectList(_context.Patients, "patient_id", "FullName", appointment.patient_id);

            return View(appointment);
        }

        // =====================================================
        // 🗑 DELETE
        // =====================================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.appointment_id == id);

            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // =====================================================
        // 🗑 DELETE CONFIRMED
        // =====================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
                _context.Appointments.Remove(appointment);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
