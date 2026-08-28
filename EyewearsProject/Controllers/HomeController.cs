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
            // -----------------------------
            // BANNERS
            // -----------------------------
            var banners = await _context.CmsContents
                .Where(c =>
                    c.Type == CmsPageType.Banner &&
                    c.IsActive)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();


            // -----------------------------
            // PARENT CATEGORIES ONLY
            // -----------------------------
            var parentCategories = await _context.Categories
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.SubCategories)
                .OrderBy(c => c.Name)
                .ToListAsync();


            // -----------------------------
            // PRODUCT COUNTS
            // Parent category + subcategories
            // -----------------------------
            var categoryProductCounts =
                await _context.Products
                    .Where(p => p.IsActive)
                    .GroupBy(p => p.CategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(
                        x => x.CategoryId,
                        x => x.Count);

            var categoryCounts = new Dictionary<int, int>();

            foreach (var category in parentCategories)
            {
                // Products directly belonging to parent
                var count = categoryProductCounts
                    .GetValueOrDefault(category.Id, 0);

                // Products belonging to its subcategories
                if (category.SubCategories != null)
                {
                    foreach (var subCategory in category.SubCategories)
                    {
                        count += categoryProductCounts
                            .GetValueOrDefault(subCategory.Id, 0);
                    }
                }
                categoryCounts[category.Id] = count;
            }

            ViewBag.Categories = parentCategories;
            ViewBag.CategoryProductCounts = categoryCounts;

            // -----------------------------
            // FEATURED PRODUCTS
            // -----------------------------
            ViewBag.FeaturedProducts = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.Id)
                .Take(4)
                .ToListAsync();


            return View(banners);
        }


        public IActionResult Privacy()
        {
            return View();
        }


        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                });
        }
    }
}