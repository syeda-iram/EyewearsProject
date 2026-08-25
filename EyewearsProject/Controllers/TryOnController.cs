using EyewearsProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Controllers
{
    public class TryOnController : Controller
    {
        private readonly AppDbContext _context;

        public TryOnController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /TryOn?productId=1
        public async Task<IActionResult> Index(int? productId)
        {
            var products = await _context.Products
                .Include(p => p.Images)
                .Where(p => p.IsActive)
                .ToListAsync();

            ViewBag.Products = products;

            var selected = productId.HasValue
                ? products.FirstOrDefault(p => p.Id == productId.Value)
                : products.FirstOrDefault();

            ViewBag.SelectedProduct = selected;

            return View();
        }
    }
}