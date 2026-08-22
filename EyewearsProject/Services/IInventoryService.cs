using EyewearsProject.Models;

namespace EyewearsProject.Services
{
    public interface IInventoryService
    {
        // Ensures an Inventory row exists for a variant (auto-creates one, seeded from legacy StockQuantity, if missing)
        Task<Inventory> GetOrCreateAsync(int productVariantId);

        // Returns how many units are actually available to sell right now
        Task<int> GetAvailableQuantityAsync(int productVariantId);

        // Records any inventory movement and updates QuantityOnHand/ReservedQuantity accordingly.
        // This is the ONLY method that should ever change stock numbers.
        Task<InventoryTransaction> RecordTransactionAsync(
            int productVariantId,
            InventoryTransactionType type,
            int quantity,
            string? referenceType = null,
            string? referenceId = null,
            string? reason = null);

        // Convenience for the Admin "set stock to this number" screen — computes the signed
        // delta from the current QuantityOnHand and logs it as an Adjustment transaction.
        Task<InventoryTransaction?> AdjustToQuantityAsync(int productVariantId, int newQuantityOnHand, string? reason = null);
    }
}