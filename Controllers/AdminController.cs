using Hospital.Data;
using Hospital.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;

namespace Hospital.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var nextMonth = thisMonth.AddMonths(1);

            // Statistics
            ViewBag.TotalPatients = await _context.Patients.CountAsync();
            ViewBag.TotalDoctors = await _context.Doctors.CountAsync();
            ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
            ViewBag.TodayAppointments = await _context.Appointments
                .Where(a => a.appointment_date.Date == today)
                .CountAsync();
            
            // Monthly statistics
            ViewBag.MonthAppointments = await _context.Appointments
                .Where(a => a.appointment_date >= thisMonth && a.appointment_date < nextMonth)
                .CountAsync();
            ViewBag.CompletedAppointments = await _context.Appointments
                .Where(a => a.status == "Completed")
                .CountAsync();
            ViewBag.PendingAppointments = await _context.Appointments
                .Where(a => a.status == "Scheduled" || a.status == "Confirmed")
                .CountAsync();
            
            // Billing statistics
            ViewBag.TotalBills = await _context.Billings.CountAsync();
            ViewBag.TotalRevenue = await _context.Billings
                .Where(b => b.payment_status == "Paid")
                .SumAsync(b => b.total_amount);
            ViewBag.PendingRevenue = await _context.Billings
                .Where(b => b.payment_status == "Pending")
                .SumAsync(b => b.total_amount);
            ViewBag.OverdueBills = await _context.Billings
                .Where(b => b.payment_status == "Pending" && 
                           b.Appointment != null && 
                           b.Appointment.appointment_date < today.AddDays(-30))
                .CountAsync();

            // Recent appointments
            var recentAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.appointment_date)
                .Take(10)
                .ToListAsync();
            ViewBag.RecentAppointments = recentAppointments;

            // Upcoming appointments (next 7 days)
            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.appointment_date >= today && a.appointment_date <= today.AddDays(7))
                .OrderBy(a => a.appointment_date)
                .Take(10)
                .ToListAsync();
            ViewBag.UpcomingAppointments = upcomingAppointments;

            // Past appointments that need attention
            var pastAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.appointment_date < today && (a.status == "Scheduled" || a.status == "Confirmed"))
                .OrderByDescending(a => a.appointment_date)
                .Take(10)
                .ToListAsync();
            ViewBag.PastAppointments = pastAppointments;

            // Overdue bills
            var overdueBills = await _context.Billings
                .Include(b => b.Appointment)
                    .ThenInclude(a => a.Patient)
                .Where(b => b.payment_status == "Pending" && 
                           b.Appointment != null && 
                           b.Appointment.appointment_date < today.AddDays(-30))
                .OrderBy(b => b.Appointment.appointment_date)
                .Take(10)
                .ToListAsync();
            ViewBag.OverdueBillsList = overdueBills;

            return View("Dashboard");
        }
    }
}

