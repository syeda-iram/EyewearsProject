using EyewearsProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AdminRoles.OrdersModule)]
    public class PrescriptionsController : Controller
    {
        private readonly AppDbContext _context;

        public PrescriptionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Prescriptions/AdminDetails/5
        // Lets Order Managers view any customer's prescription attached to an
        // order — unlike the customer-facing PrescriptionsController, this is
        // deliberately NOT filtered by UserId, since staff need to look up
        // prescriptions belonging to whichever customer placed the order.
        public async Task<IActionResult> AdminDetails(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null) return NotFound();

            ViewData["Title"] = $"Prescription — {prescription.PatientName}";
            return View(prescription);
        }
    }
}