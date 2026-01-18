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
    [Authorize(Roles = "Admin,Receptionist")]
    public class BillingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BillingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Billings
        // Single Index method with optional filtering
        public async Task<IActionResult> Index(DateTime? fromDate = null, DateTime? toDate = null, string status = null)
        {
            var bills = _context.Billings
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Doctor)
                .AsQueryable();

            if (fromDate.HasValue)
                bills = bills.Where(b => b.Appointment.appointment_date >= fromDate.Value);

            if (toDate.HasValue)
                bills = bills.Where(b => b.Appointment.appointment_date <= toDate.Value);

            if (!string.IsNullOrEmpty(status))
                bills = bills.Where(b => b.payment_status == status);

            // Pass statistics to ViewBag for dashboard cards
            ViewBag.TotalBills = await bills.CountAsync();
            ViewBag.TotalAmount = await bills.SumAsync(b => b.total_amount);
            ViewBag.PaidAmount = await bills.Where(b => b.payment_status == "Paid").SumAsync(b => b.total_amount);
            ViewBag.PendingAmount = await bills.Where(b => b.payment_status != "Paid").SumAsync(b => b.total_amount);
            ViewBag.PaidBills = await bills.Where(b => b.payment_status == "Paid").CountAsync();
            ViewBag.PendingBills = await bills.Where(b => b.payment_status != "Paid").CountAsync();

            // Store filter values for export functionality
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Status = status;

            return View(await bills.OrderByDescending(b => b.payment_date).ToListAsync());
        }

        // GET: Billings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var billing = await _context.Billings
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.bill_id == id);

            if (billing == null) return NotFound();

            return View(billing);
        }

        // GET: Billings/Create
        public IActionResult Create()
        {
            ViewData["appointment_id"] = new SelectList(_context.Appointments, "appointment_id", "appointment_id");
            return View();
        }

        // POST: Billings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("bill_id,appointment_id,total_amount,payment_status,payment_date")] Billing billing)
        {
            if (ModelState.IsValid)
            {
                bool exists = await _context.Billings.AnyAsync(b => b.appointment_id == billing.appointment_id);
                if (exists)
                {
                    ModelState.AddModelError("appointment_id", "Payment already done for this appointment.");
                    ViewData["appointment_id"] = new SelectList(_context.Appointments, "appointment_id", "appointment_id", billing.appointment_id);
                    return View(billing);
                }

                if (billing.payment_date.HasValue)
                    billing.payment_date = DateTime.SpecifyKind(billing.payment_date.Value, DateTimeKind.Local).ToUniversalTime();

                _context.Add(billing);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["appointment_id"] = new SelectList(_context.Appointments, "appointment_id", "appointment_id", billing.appointment_id);
            return View(billing);
        }

        // GET: Billings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var billing = await _context.Billings.FindAsync(id);
            if (billing == null) return NotFound();

            ViewData["appointment_id"] = new SelectList(_context.Appointments, "appointment_id", "appointment_id", billing.appointment_id);
            return View(billing);
        }

        // POST: Billings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("bill_id,appointment_id,total_amount,payment_status,payment_date")] Billing billing)
        {
            if (id != billing.bill_id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(billing);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BillingExists(billing.bill_id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["appointment_id"] = new SelectList(_context.Appointments, "appointment_id", "appointment_id", billing.appointment_id);
            return View(billing);
        }

        // GET: Billings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var billing = await _context.Billings
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.bill_id == id);

            if (billing == null) return NotFound();

            return View(billing);
        }

        // POST: Billings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var billing = await _context.Billings.FindAsync(id);
            if (billing != null) _context.Billings.Remove(billing);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BillingExists(int id)
        {
            return _context.Billings.Any(e => e.bill_id == id);
        }

        // GET: Billings/MyBills (for patients)
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> MyBills()
        {
            var patientId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(patientId) || !int.TryParse(patientId, out int pid))
                return RedirectToAction("Login", "Account");

            var billings = await _context.Billings
                .Where(b => b.Appointment.patient_id == pid)
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Doctor)
                .OrderByDescending(b => b.payment_date)
                .ToListAsync();

            return View("Index", billings);
        }

        // GET: Export Financial Report as CSV
        public async Task<IActionResult> ExportFinancialReport(DateTime? startDate = null, DateTime? endDate = null, string status = null)
        {
            var bills = _context.Billings
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Doctor)
                .AsQueryable();

            // Apply filters
            if (startDate.HasValue)
                bills = bills.Where(b => b.Appointment != null && b.Appointment.appointment_date >= startDate.Value);

            if (endDate.HasValue)
                bills = bills.Where(b => b.Appointment != null && b.Appointment.appointment_date <= endDate.Value);

            if (!string.IsNullOrEmpty(status))
                bills = bills.Where(b => b.payment_status == status);

            var billsList = await bills.OrderByDescending(b => b.payment_date ?? DateTime.MinValue).ToListAsync();

            // Generate CSV content
            var csv = "Bill ID,Patient Name,Appointment Date,Doctor,Amount,Payment Status,Payment Date\n";
            foreach (var bill in billsList)
            {
                var patientName = bill.Appointment?.Patient != null 
                    ? $"{bill.Appointment.Patient.first_name} {bill.Appointment.Patient.last_name}" 
                    : "N/A";
                var appointmentDate = bill.Appointment?.appointment_date.ToString("yyyy-MM-dd") ?? "N/A";
                var doctorName = bill.Appointment?.Doctor != null 
                    ? $"Dr. {bill.Appointment.Doctor.first_name} {bill.Appointment.Doctor.last_name}" 
                    : "N/A";
                var paymentDate = bill.payment_date?.ToString("yyyy-MM-dd") ?? "Pending";
                
                csv += $"{bill.bill_id},\"{patientName}\",{appointmentDate},\"{doctorName}\",{bill.total_amount},{bill.payment_status},{paymentDate}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            var fileName = $"FinancialReport_{startDate?.ToString("yyyyMMdd") ?? "All"}_{endDate?.ToString("yyyyMMdd") ?? DateTime.Today.ToString("yyyyMMdd")}.csv";
            return File(bytes, "text/csv", fileName);
        }

        // GET: Export Financial Report Summary as CSV
        public async Task<IActionResult> ExportFinancialSummary(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.Today.AddMonths(-1);
            var end = endDate ?? DateTime.Today;

            var bills = await _context.Billings
                .Include(b => b.Appointment)
                .Where(b => b.Appointment != null && 
                           b.Appointment.appointment_date >= start && 
                           b.Appointment.appointment_date <= end)
                .ToListAsync();

            var totalAmount = bills.Sum(b => b.total_amount);
            var paidAmount = bills.Where(b => b.payment_status == "Paid").Sum(b => b.total_amount);
            var pendingAmount = bills.Where(b => b.payment_status == "Pending").Sum(b => b.total_amount);

            // Generate summary CSV
            var csv = "Financial Report Summary\n";
            csv += $"Report Period: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}\n";
            csv += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";
            csv += "Category,Amount\n";
            csv += $"Total Bills,{bills.Count}\n";
            csv += $"Total Amount,{totalAmount:C}\n";
            csv += $"Paid Amount,{paidAmount:C}\n";
            csv += $"Pending Amount,{pendingAmount:C}\n";
            csv += $"Paid Bills,{bills.Count(b => b.payment_status == "Paid")}\n";
            csv += $"Pending Bills,{bills.Count(b => b.payment_status == "Pending")}\n";

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            var fileName = $"FinancialSummary_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";
            return File(bytes, "text/csv", fileName);
        }
    }
}
