using Hospital.Data;
using Hospital.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hospital.Controllers
{
    [Authorize(Roles = "Admin,Receptionist,Doctor")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        public IActionResult Index()
        {
            return View();
        }

        // GET: Reports/DailyAppointments
        public async Task<IActionResult> DailyAppointments(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            
            var appointments = await _context.Appointments
                .Where(a => a.appointment_date.Date == selectedDate.Date)
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Billing)
                .OrderBy(a => a.appointment_date)
                .ToListAsync();

            ViewBag.SelectedDate = selectedDate;
            ViewBag.TotalAppointments = appointments.Count;
            ViewBag.CompletedAppointments = appointments.Count(a => a.status == "Completed");
            ViewBag.PendingAppointments = appointments.Count(a => a.status == "Scheduled" || a.status == "Confirmed");
            ViewBag.CancelledAppointments = appointments.Count(a => a.status == "Cancelled");

            return View(appointments);
        }

        // GET: Reports/BillingSummary
        public async Task<IActionResult> BillingSummary(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;

            var billings = await _context.Billings
                .Where(b => (b.payment_date.HasValue && b.payment_date >= start && b.payment_date <= end) || 
                           (!b.payment_date.HasValue && b.Appointment != null && b.Appointment.appointment_date >= start && b.Appointment.appointment_date <= end))
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Patient)
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Doctor)
                .OrderByDescending(b => b.payment_date ?? b.Appointment.appointment_date)
                .ToListAsync();

            var totalAmount = billings.Sum(b => b.total_amount);
            var paidAmount = billings.Where(b => b.payment_status == "Paid").Sum(b => b.total_amount);
            var pendingAmount = billings.Where(b => b.payment_status == "Pending").Sum(b => b.total_amount);

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.PaidAmount = paidAmount;
            ViewBag.PendingAmount = pendingAmount;
            ViewBag.TotalBills = billings.Count;
            ViewBag.PaidBills = billings.Count(b => b.payment_status == "Paid");
            ViewBag.PendingBills = billings.Count(b => b.payment_status == "Pending");

            return View(billings);
        }

        // Export Daily Appointments to PDF
        [HttpGet]
        public async Task<IActionResult> ExportDailyAppointmentsPDF(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            
            var appointments = await _context.Appointments
                .Where(a => a.appointment_date.Date == selectedDate.Date)
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderBy(a => a.appointment_date)
                .ToListAsync();

            // For now, return JSON. In production, use a PDF library like iTextSharp or QuestPDF
            return Json(new
            {
                date = selectedDate.ToString("yyyy-MM-dd"),
                appointments = appointments.Select(a => new
                {
                    time = a.appointment_date.ToString("HH:mm"),
                    patient = a.Patient?.first_name + " " + a.Patient?.last_name,
                    doctor = a.Doctor?.first_name + " " + a.Doctor?.last_name,
                    status = a.status
                })
            });
        }

        // Export Daily Appointments to Excel
        [HttpGet]
        public async Task<IActionResult> ExportDailyAppointmentsExcel(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            
            var appointments = await _context.Appointments
                .Where(a => a.appointment_date.Date == selectedDate.Date)
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderBy(a => a.appointment_date)
                .ToListAsync();

            // For now, return CSV format. In production, use a library like EPPlus or ClosedXML
            var csv = "Time,Patient,Doctor,Status\n";
            foreach (var app in appointments)
            {
                csv += $"{app.appointment_date:HH:mm},{app.Patient?.first_name} {app.Patient?.last_name},{app.Doctor?.first_name} {app.Doctor?.last_name},{app.status}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"DailyAppointments_{selectedDate:yyyyMMdd}.csv");
        }

        // Export Billing Summary to PDF
        [HttpGet]
        public async Task<IActionResult> ExportBillingSummaryPDF(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;

            var billings = await _context.Billings
                .Where(b => (b.payment_date.HasValue && b.payment_date >= start && b.payment_date <= end) ||
                           (!b.payment_date.HasValue && b.Appointment != null && b.Appointment.appointment_date >= start && b.Appointment.appointment_date <= end))
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Patient)
                .OrderByDescending(b => b.payment_date ?? b.Appointment.appointment_date)
                .ToListAsync();

            // For now, return JSON. In production, use a PDF library
            return Json(new
            {
                startDate = start.ToString("yyyy-MM-dd"),
                endDate = end.ToString("yyyy-MM-dd"),
                totalAmount = billings.Sum(b => b.total_amount),
                billings = billings.Select(b => new
                {
                    date = b.payment_date?.ToString("yyyy-MM-dd"),
                    patient = b.Appointment?.Patient?.first_name + " " + b.Appointment?.Patient?.last_name,
                    amount = b.total_amount,
                    status = b.payment_status
                })
            });
        }

        // Export Billing Summary to Excel
        [HttpGet]
        public async Task<IActionResult> ExportBillingSummaryExcel(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;

            var billings = await _context.Billings
                .Where(b => (b.payment_date.HasValue && b.payment_date >= start && b.payment_date <= end) ||
                           (!b.payment_date.HasValue && b.Appointment != null && b.Appointment.appointment_date >= start && b.Appointment.appointment_date <= end))
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Patient)
                .OrderByDescending(b => b.payment_date ?? b.Appointment.appointment_date)
                .ToListAsync();

            // CSV format for now
            var csv = "Date,Patient,Amount,Status\n";
            foreach (var bill in billings)
            {
                csv += $"{bill.payment_date:yyyy-MM-dd},{bill.Appointment?.Patient?.first_name} {bill.Appointment?.Patient?.last_name},{bill.total_amount},{bill.payment_status}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"BillingSummary_{start:yyyyMMdd}_{end:yyyyMMdd}.csv");
        }
    }
}

