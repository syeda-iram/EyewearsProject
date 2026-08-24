using EyewearsProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EyewearsProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var banners = await _context.CmsContents
                .Where(c => c.Type == CmsPageType.Banner && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            ViewBag.FeaturedProducts = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Id)
                .Take(4)
                .ToListAsync();

            return View(banners);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}