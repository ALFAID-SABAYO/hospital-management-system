using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Hospital.Data;
using Hospital.Models;

namespace Hospital.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // GET: Departments
        // Adjustable:
        //  - Shows department list
        //  - Shows REAL doctor count (from database)
        // =====================================================
        public async Task<IActionResult> Index()
        {
            /*
             * We calculate DoctorCount in the controller (NOT in the View)
             * This avoids random values and multiple DB hits
             * This follows proper MVC architecture
             */

            var departments = await _context.Departments
                .Select(d => new DepartmentWithDoctorCountVM
                {
                    DepartmentId = d.department_id,
                    Name = d.name,
                    Location = d.location,

                    // REAL count of doctors in this department
                    DoctorCount = _context.Doctors
                        .Count(doc => doc.department_id == d.department_id)
                })
                .ToListAsync();

            return View(departments);
        }

        // =====================================================
        // GET: Departments/Details/5
        // =====================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.department_id == id);

            if (department == null)
                return NotFound();

            return View(department);
        }

        // =====================================================
        // GET: Departments/Create
        // =====================================================
        public IActionResult Create()
        {
            return View();
        }

        // =====================================================
        // POST: Departments/Create
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Department created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(department);
        }

        // =====================================================
        // GET: Departments/Edit/5
        // =====================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            return View(department);
        }

        // =====================================================
        // POST: Departments/Edit/5
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            if (id != department.department_id)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(department);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Department updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(department);
        }

        // =====================================================
        // GET: Departments/Delete/5
        // =====================================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.department_id == id);

            if (department == null)
                return NotFound();

            return View(department);
        }

        // =====================================================
        // POST: Departments/Delete/5
        // =====================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);

            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Department deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // Helper
        // =====================================================
        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(d => d.department_id == id);
        }
    }

    // =====================================================
    // VIEW MODEL
    // Used only for Index page
    // =====================================================
    public class DepartmentViewModel
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }

        // Calculated value (not stored in DB)
        public int DoctorCount { get; set; }
    }
}
