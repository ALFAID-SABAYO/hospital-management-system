using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hospital.Data;

namespace Hospital.Controllers
{
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                bool canConnect = await _context.Database.CanConnectAsync();
                ViewData["Message"] = canConnect ?
                    "✅ Connected to Neon PostgreSQL!" :
                    "❌ Could not connect to database";
                return View();
            }
            catch (Exception ex)
            {
                ViewData["Message"] = $"❌ Error: {ex.Message}";
                return View();
            }
        }
    }
}