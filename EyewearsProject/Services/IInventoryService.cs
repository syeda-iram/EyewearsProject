using EyewearsProject.Models;

namespace EyewearsProject.Services
{
    public interface IInventoryService
    {
        Task EnsureInventoryExistsAsync(int productVariantId);

        Task<Inventory> GetOrCreateAsync(int productVariantId);

        Task<int> GetQuantityAsync(int productVariantId);

        Task<int> GetAvailableQuantityAsync(int productVariantId);

        Task AdjustToQuantityAsync(
            int productVariantId,
            int newQuantity,
            string? reason = null);

        Task RecordTransactionAsync(
            int productVariantId,
            InventoryTransactionType transactionType,
            int quantity,
            string? referenceType = null,
            string? reason = null,
            string? referenceId = null);
    }
}