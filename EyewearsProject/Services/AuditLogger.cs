using EyewearsProject.Models;
using Microsoft.AspNetCore.Http;

namespace EyewearsProject.Services
{
    public class AuditLogger : IAuditLogger
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogger(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string entityType, string? entityId = null, string? details = null)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            var log = new AuditLog
            {
                UserId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                UserEmail = user?.Identity?.Name ?? "system",
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}