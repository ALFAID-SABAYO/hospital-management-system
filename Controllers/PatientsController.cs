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
using System.Collections.Generic;

namespace Hospital.Controllers
{
    [Authorize(Roles = "Admin,Doctor,Patient,Receptionist")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Patients
        public async Task<IActionResult> Index(string searchString, string bloodGroup)
        {
            var patients = _context.Patients.AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(searchString))
            {
                patients = patients.Where(p =>
                    p.first_name.Contains(searchString) ||
                    p.last_name.Contains(searchString) ||
                    p.contact_number.Contains(searchString) ||
                    p.email.Contains(searchString));
            }

            // Filter blood group
            if (!string.IsNullOrEmpty(bloodGroup))
            {
                patients = patients.Where(p => p.blood_group == bloodGroup);
            }

            // Blood group dropdown
            var bloodGroups = await _context.Patients
                .Select(p => p.blood_group)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .ToListAsync();

            ViewBag.BloodGroups = new SelectList(bloodGroups);
            ViewBag.CurrentFilter = searchString;

            return View(await patients.ToListAsync());
        }

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Billing)
                .FirstOrDefaultAsync(p => p.patient_id == id);

            if (patient == null) return NotFound();

            // Statistics
            ViewBag.TotalAppointments = patient.Appointments?.Count ?? 0;
            ViewBag.PendingAppointments = patient.Appointments?.Count(a => a.status == "Pending") ?? 0;
            ViewBag.TotalBillAmount = patient.Appointments?.Where(a => a.Billing != null).Sum(a => a.Billing.total_amount) ?? 0;

            return View(patient);
        }

        // GET: Patients/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("patient_id,first_name,last_name,gender,date_of_birth,contact_number,email,address,blood_group")] Patient patient)
        {
            if (await _context.Patients.AnyAsync(p => p.email == patient.email))
            {
                ModelState.AddModelError("email", "Email already exists.");
            }

            if (patient.date_of_birth > DateTime.Today)
            {
                ModelState.AddModelError("date_of_birth", "Date of birth cannot be in the future.");
            }

            if (patient.date_of_birth < DateTime.Today.AddYears(-150))
            {
                ModelState.AddModelError("date_of_birth", "Date of birth seems unrealistic.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(patient);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Patient registered successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(patient);
        }

        // GET: Patients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            return View(patient);
        }

        // POST: Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("patient_id,first_name,last_name,gender,date_of_birth,contact_number,email,address,blood_group")] Patient patient)
        {
            if (id != patient.patient_id) return NotFound();

            if (await _context.Patients.AnyAsync(p => p.email == patient.email && p.patient_id != id))
            {
                ModelState.AddModelError("email", "Email already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Patient information updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.patient_id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        // GET: Patients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Appointments)
                .FirstOrDefaultAsync(p => p.patient_id == id);

            if (patient == null) return NotFound();

            ViewBag.HasActiveAppointments = patient.Appointments?.Any(a => a.status != "Cancelled" && a.status != "Completed") ?? false;
            return View(patient);
        }

        // POST: Patients/Delete/5
        [Authorize(Roles = "Doctor,Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.Appointments)
                .FirstOrDefaultAsync(p => p.patient_id == id);

            if (patient == null) return NotFound();

            var activeAppointments = patient.Appointments?.Where(a => a.status != "Cancelled" && a.status != "Completed").ToList();
            if (activeAppointments?.Any() == true)
            {
                TempData["ErrorMessage"] = "Cannot delete patient with active appointments.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Patient deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Patients/MedicalHistory/5
        public async Task<IActionResult> MedicalHistory(int? id)
        {
            if (id == null) return NotFound();

            var medicalRecords = await _context.MedicalRecords
                .Where(mr => mr.Appointment.patient_id == id)
                .Include(mr => mr.Appointment)
                    .ThenInclude(a => a.Doctor)
                .Include(mr => mr.Prescriptions)
                .Include(mr => mr.TestResults)
                .OrderByDescending(mr => mr.visit_date)
                .ToListAsync();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            ViewBag.PatientName = $"{patient.first_name} {patient.last_name}";
            return View(medicalRecords);
        }

        // GET: Patients/BillingHistory/5
        public async Task<IActionResult> BillingHistory(int? id)
        {
            if (id == null) return NotFound();

            var billings = await _context.Billings
                .Where(b => b.Appointment.patient_id == id)
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Doctor)
                .OrderByDescending(b => b.Appointment.appointment_date)
                .ToListAsync();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            ViewBag.PatientName = $"{patient.first_name} {patient.last_name}";
            ViewBag.TotalPaid = billings.Where(b => b.payment_status == "Paid").Sum(b => b.total_amount);
            ViewBag.TotalPending = billings.Where(b => b.payment_status != "Paid").Sum(b => b.total_amount);

            return View(billings);
        }

        // GET: Patients/GenerateReport/5
        public async Task<IActionResult> GenerateReport(int? id, string reportType)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Billing)
                .FirstOrDefaultAsync(p => p.patient_id == id);

            if (patient == null) return NotFound();

            return reportType?.ToLower() switch
            {
                "medical" => View("MedicalReport", patient),
                "billing" => View("BillingReport", patient),
                _ => View("PatientSummaryReport", patient),
            };
        }

        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> Dashboard()
        {
            var patientIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(patientIdStr, out int pid)) return RedirectToAction("Login", "Account");

            var patient = await _context.Patients
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.MedicalRecord)
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Billing)
                .FirstOrDefaultAsync(p => p.patient_id == pid);

            if (patient == null) return RedirectToAction("Login", "Account");

            ViewBag.PatientName = $"{patient.first_name} {patient.last_name}";
            ViewBag.PatientId = pid;
            ViewBag.TotalAppointments = patient.Appointments?.Count ?? 0;
            ViewBag.UpcomingAppointments = patient.Appointments?.Count(a => a.appointment_date > DateTime.Now && a.status != "Cancelled" && a.status != "Completed") ?? 0;
            ViewBag.PendingBills = patient.Appointments?.Where(a => a.Billing != null && a.Billing.payment_status != "Paid").Sum(a => a.Billing.total_amount) ?? 0;
            ViewBag.MedicalRecords = patient.Appointments?.Count(a => a.MedicalRecord != null) ?? 0;

            ViewBag.RecentAppointments = patient.Appointments?.Where(a => a.appointment_date > DateTime.Now).OrderBy(a => a.appointment_date).Take(5).ToList() ?? new List<Appointment>();
            ViewBag.RecentRecords = patient.Appointments?.Where(a => a.MedicalRecord != null).Select(a => a.MedicalRecord).OrderByDescending(mr => mr.visit_date).Take(5).ToList() ?? new List<MedicalRecord>();

            ViewBag.HasData = ViewBag.TotalAppointments > 0;

            return View(patient);
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(p => p.patient_id == id);
        }
    }
}
