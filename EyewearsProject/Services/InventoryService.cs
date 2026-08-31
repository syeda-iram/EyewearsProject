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

        // =====================================================
        // ENSURE INVENTORY
        // =====================================================

        public async Task EnsureInventoryExistsAsync(
            int productVariantId)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(
                    i => i.ProductVariantId == productVariantId);

            if (inventory != null)
                return;

            inventory = new Inventory
            {
                ProductVariantId = productVariantId,
                QuantityOnHand = 0,
                ReservedQuantity = 0,
                ReorderLevel = 10,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Inventories.Add(inventory);

            await _context.SaveChangesAsync();
        }

        // =====================================================
        // GET OR CREATE INVENTORY
        // =====================================================

        public async Task<Inventory> GetOrCreateAsync(
            int productVariantId)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(
                    i => i.ProductVariantId == productVariantId);

            if (inventory != null)
                return inventory;

            inventory = new Inventory
            {
                ProductVariantId = productVariantId,
                QuantityOnHand = 0,
                ReservedQuantity = 0,
                ReorderLevel = 10,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Inventories.Add(inventory);

            await _context.SaveChangesAsync();

            return inventory;
        }

        // =====================================================
        // GET CURRENT QUANTITY
        // =====================================================

        public async Task<int> GetQuantityAsync(
            int productVariantId)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(
                    i => i.ProductVariantId == productVariantId);

            return inventory?.QuantityOnHand ?? 0;
        }

        // =====================================================
        // GET AVAILABLE QUANTITY
        // =====================================================

        public async Task<int> GetAvailableQuantityAsync(
            int productVariantId)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(
                    i => i.ProductVariantId == productVariantId);

            if (inventory == null)
                return 0;

            return inventory.AvailableQuantity;
        }

        // =====================================================
        // SET STOCK TO EXACT QUANTITY
        // =====================================================

        public async Task AdjustToQuantityAsync(
            int productVariantId,
            int newQuantity,
            string? reason = null)
        {
            if (newQuantity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(newQuantity));
            }

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(
                    i => i.ProductVariantId == productVariantId);

            if (inventory == null)
            {
                inventory = new Inventory
                {
                    ProductVariantId = productVariantId,
                    QuantityOnHand = 0,
                    ReservedQuantity = 0,
                    ReorderLevel = 10,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Inventories.Add(inventory);

                await _context.SaveChangesAsync();
            }

            var oldQuantity = inventory.QuantityOnHand;

            if (oldQuantity == newQuantity)
                return;

            var difference = newQuantity - oldQuantity;

            inventory.QuantityOnHand = newQuantity;
            inventory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await RecordTransactionAsync(
                productVariantId,
                difference > 0
                    ? InventoryTransactionType.Purchase
                    : InventoryTransactionType.Adjustment,
                Math.Abs(difference),
                referenceType: "ProductEdit",
                reason: reason ??
                    $"Stock changed from {oldQuantity} to {newQuantity}");
        }

        // =====================================================
        // RECORD TRANSACTION
        // =====================================================

        public async Task RecordTransactionAsync(
            int productVariantId,
            InventoryTransactionType transactionType,
            int quantity,
            string? referenceType = null,
            string? reason = null,
            string? referenceId = null)
        {
            if (quantity <= 0)
                return;

            var transaction = new InventoryTransaction
            {
                ProductVariantId = productVariantId,
                TransactionType = transactionType,
                Quantity = quantity,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            };

            _context.InventoryTransactions.Add(transaction);

            await _context.SaveChangesAsync();
        }
    }
}