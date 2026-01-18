using Hospital.Data;
using Hospital.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hospital.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Notifications/GetUpcomingAppointments
        [HttpGet]
        public async Task<IActionResult> GetUpcomingAppointments()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var upcomingAppointments = new List<object>();

            if (userRole == "Patient")
            {
                if (int.TryParse(userId, out int patientId))
                {
                    var appointments = await _context.Appointments
                        .Where(a => a.patient_id == patientId &&
                                   a.appointment_date > DateTime.Now &&
                                   a.appointment_date <= DateTime.Now.AddDays(7) &&
                                   a.status != "Cancelled")
                        .Include(a => a.Doctor)
                        .OrderBy(a => a.appointment_date)
                        .ToListAsync();

                    upcomingAppointments = appointments.Select(a => new
                    {
                        id = a.appointment_id,
                        date = a.appointment_date.ToString("MMM dd, yyyy HH:mm"),
                        doctor = $"{a.Doctor?.first_name} {a.Doctor?.last_name}",
                        status = a.status,
                        hoursUntil = Math.Round((a.appointment_date - DateTime.Now).TotalHours, 1)
                    }).ToList<object>();
                }
            }
            else if (userRole == "Doctor")
            {
                if (int.TryParse(userId, out int doctorId))
                {
                    var appointments = await _context.Appointments
                        .Where(a => a.doctor_id == doctorId &&
                                   a.appointment_date > DateTime.Now &&
                                   a.appointment_date <= DateTime.Now.AddDays(7) &&
                                   a.status != "Cancelled")
                        .Include(a => a.Patient)
                        .OrderBy(a => a.appointment_date)
                        .ToListAsync();

                    upcomingAppointments = appointments.Select(a => new
                    {
                        id = a.appointment_id,
                        date = a.appointment_date.ToString("MMM dd, yyyy HH:mm"),
                        patient = $"{a.Patient?.first_name} {a.Patient?.last_name}",
                        status = a.status,
                        hoursUntil = Math.Round((a.appointment_date - DateTime.Now).TotalHours, 1)
                    }).ToList<object>();
                }
            }
            else if (userRole == "Admin" || userRole == "Receptionist")
            {
                var appointments = await _context.Appointments
                    .Where(a => a.appointment_date > DateTime.Now &&
                               a.appointment_date <= DateTime.Now.AddDays(7) &&
                               a.status != "Cancelled")
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .OrderBy(a => a.appointment_date)
                    .Take(10)
                    .ToListAsync();

                upcomingAppointments = appointments.Select(a => new
                {
                    id = a.appointment_id,
                    date = a.appointment_date.ToString("MMM dd, yyyy HH:mm"),
                    patient = $"{a.Patient?.first_name} {a.Patient?.last_name}",
                    doctor = $"{a.Doctor?.first_name} {a.Doctor?.last_name}",
                    status = a.status,
                    hoursUntil = Math.Round((a.appointment_date - DateTime.Now).TotalHours, 1)
                }).ToList<object>();
            }

            return Json(new
            {
                count = upcomingAppointments.Count,
                appointments = upcomingAppointments
            });
        }
    }
}

