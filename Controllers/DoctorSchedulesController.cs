using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Hospital.Data;
using Hospital.Models;

namespace Hospital.Controllers
{
    [Authorize(Roles = "Admin,Doctor")]
    public class DoctorSchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorSchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DoctorSchedules
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.DoctorSchedules.Include(d => d.Doctor);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: DoctorSchedules/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctorSchedule = await _context.DoctorSchedules
                .Include(d => d.Doctor)
                .FirstOrDefaultAsync(m => m.schedule_id == id);
            if (doctorSchedule == null)
            {
                return NotFound();
            }

            return View(doctorSchedule);
        }

        // GET: DoctorSchedules/Create
        public IActionResult Create()
        {
            ViewData["doctor_id"] = new SelectList(_context.Doctors, "doctor_id", "doctor_id");
            return View();
        }

        // POST: DoctorSchedules/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("schedule_id,doctor_id,available_date,start_time,end_time")] DoctorSchedule doctorSchedule)
        {
            if (ModelState.IsValid)
            {
                _context.Add(doctorSchedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["doctor_id"] = new SelectList(_context.Doctors, "doctor_id", "doctor_id", doctorSchedule.doctor_id);
            return View(doctorSchedule);
        }

        // GET: DoctorSchedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctorSchedule = await _context.DoctorSchedules.FindAsync(id);
            if (doctorSchedule == null)
            {
                return NotFound();
            }
            ViewData["doctor_id"] = new SelectList(_context.Doctors, "doctor_id", "doctor_id", doctorSchedule.doctor_id);
            return View(doctorSchedule);
        }

        // POST: DoctorSchedules/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("schedule_id,doctor_id,available_date,start_time,end_time")] DoctorSchedule doctorSchedule)
        {
            if (id != doctorSchedule.schedule_id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctorSchedule);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorScheduleExists(doctorSchedule.schedule_id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["doctor_id"] = new SelectList(_context.Doctors, "doctor_id", "doctor_id", doctorSchedule.doctor_id);
            return View(doctorSchedule);
        }

        // GET: DoctorSchedules/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctorSchedule = await _context.DoctorSchedules
                .Include(d => d.Doctor)
                .FirstOrDefaultAsync(m => m.schedule_id == id);
            if (doctorSchedule == null)
            {
                return NotFound();
            }

            return View(doctorSchedule);
        }

        // POST: DoctorSchedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctorSchedule = await _context.DoctorSchedules.FindAsync(id);
            if (doctorSchedule != null)
            {
                _context.DoctorSchedules.Remove(doctorSchedule);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DoctorScheduleExists(int id)
        {
            return _context.DoctorSchedules.Any(e => e.schedule_id == id);
        }
    }
}
