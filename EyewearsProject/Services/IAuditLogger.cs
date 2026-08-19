namespace EyewearsProject.Services
{
    public interface IAuditLogger
    {
        Task LogAsync(string action, string entityType, string? entityId = null, string? details = null);
    }
}