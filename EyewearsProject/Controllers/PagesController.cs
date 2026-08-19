using EyewearsProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Controllers
{
    public class PagesController : Controller
    {
        private readonly AppDbContext _context;
        public PagesController(AppDbContext context) => _context = context;

        // GET: /Pages/about-us
        [Route("Pages/{slug}")]
        public async Task<IActionResult> View(string slug)
        {
            var page = await _context.CmsContents
                .FirstOrDefaultAsync(c => c.Slug == slug && c.Type == CmsPageType.Page && c.IsActive);

            if (page == null) return NotFound();

            return View(page);
        }
    }
}