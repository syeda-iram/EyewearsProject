using EyewearsProject.Models;

namespace EyewearsProject.Services
{
    public interface ICartService
    {
        // Logged-in customer -> reads/writes the CartLines table for that user.
        // Guest (not logged in) -> reads/writes a per-browser-session list.
        // Either way, callers just get/set List<CartItem> and never think about which.
        Task<List<CartItem>> GetCartAsync();

        Task AddAsync(CartItem item);

        Task UpdateQuantityAsync(string lineId, int quantity);

        Task RemoveAsync(string lineId);

        Task ClearAsync();

        // Called right after a successful login. Folds anything sitting in the
        // guest session cart into that user's own DB cart, then wipes the
        // session cart so it can never be picked up by whoever logs in next.
        Task MergeGuestCartIntoUserAsync();

        // Called on logout, defensively, so a leftover guest cart can never be
        // seen by (or merged into) the next person who uses this browser.
        void ClearGuestSessionCart();
    }
}