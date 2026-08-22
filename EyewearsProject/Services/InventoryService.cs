using EyewearsProject.Models;
using Microsoft.EntityFrameworkCore;

namespace EyewearsProject.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _context;

        public InventoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Inventory> GetOrCreateAsync(int productVariantId)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductVariantId == productVariantId);

            if (inventory != null) return inventory;

            // Migration path: if this variant predates the Inventory table,
            // seed it from the old ProductVariant.StockQuantity so nothing is lost.
            var variant = await _context.ProductVariants.FindAsync(productVariantId);
            var startingQuantity = variant?.StockQuantity ?? 0;

            inventory = new Inventory
            {
                ProductVariantId = productVariantId,
                QuantityOnHand = startingQuantity,
                ReservedQuantity = 0,
                ReorderLevel = 10,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();

            return inventory;
        }

        public async Task<int> GetAvailableQuantityAsync(int productVariantId)
        {
            var inventory = await GetOrCreateAsync(productVariantId);
            return inventory.AvailableQuantity;
        }

        public async Task<InventoryTransaction?> AdjustToQuantityAsync(int productVariantId, int newQuantityOnHand, string? reason = null)
        {
            var inventory = await GetOrCreateAsync(productVariantId);
            var delta = newQuantityOnHand - inventory.QuantityOnHand;

            if (delta == 0) return null; // nothing changed, no transaction needed

            inventory.QuantityOnHand = Math.Max(0, newQuantityOnHand);
            inventory.UpdatedAt = DateTime.UtcNow;

            var variant = await _context.ProductVariants.FindAsync(productVariantId);
            if (variant != null)
                variant.StockQuantity = inventory.AvailableQuantity;

            var transaction = new InventoryTransaction
            {
                ProductVariantId = productVariantId,
                TransactionType = InventoryTransactionType.Adjustment,
                Quantity = delta, // signed — Adjustment is the one type allowed to carry +/-
                ReferenceType = "ManualAdjustment",
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            };

            _context.InventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }

        public async Task<InventoryTransaction> RecordTransactionAsync(
            int productVariantId,
            InventoryTransactionType type,
            int quantity,
            string? referenceType = null,
            string? referenceId = null,
            string? reason = null)
        {
            if (quantity < 0)
                throw new ArgumentException("Quantity must be a positive number — direction is implied by TransactionType.", nameof(quantity));

            var inventory = await GetOrCreateAsync(productVariantId);

            switch (type)
            {
                case InventoryTransactionType.Purchase:
                case InventoryTransactionType.Return:
                    inventory.QuantityOnHand += quantity;
                    break;

                case InventoryTransactionType.Sale:
                case InventoryTransactionType.Damage:
                case InventoryTransactionType.Transfer:
                    inventory.QuantityOnHand -= quantity;
                    // If this sale was previously reserved, release the reservation too
                    inventory.ReservedQuantity = Math.Max(0, inventory.ReservedQuantity - quantity);
                    break;

                case InventoryTransactionType.Reservation:
                    inventory.ReservedQuantity += quantity;
                    break;

                case InventoryTransactionType.Release:
                    inventory.ReservedQuantity = Math.Max(0, inventory.ReservedQuantity - quantity);
                    break;

                case InventoryTransactionType.Adjustment:
                    // For Adjustment, "quantity" already carries the correct sign via a signed helper —
                    // see AdjustToQuantityAsync below, which is what the Admin screen actually calls.
                    inventory.QuantityOnHand += quantity;
                    break;
            }

            inventory.QuantityOnHand = Math.Max(0, inventory.QuantityOnHand);
            inventory.UpdatedAt = DateTime.UtcNow;

            // Keep the legacy field in sync so existing views/checks that still read
            // ProductVariant.StockQuantity (customer-side availability checks, etc.)
            // keep working correctly without needing to be rewritten right away.
            var variant = await _context.ProductVariants.FindAsync(productVariantId);
            if (variant != null)
                variant.StockQuantity = inventory.AvailableQuantity;

            var transaction = new InventoryTransaction
            {
                ProductVariantId = productVariantId,
                TransactionType = type,
                Quantity = quantity,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            };

            _context.InventoryTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }
    }
}