namespace EyewearsProject.Models
{
    public static class LensOptions
    {
        public static readonly Dictionary<string, decimal> LensTypes = new()
        {
            { "Single Vision", 0 },
            { "Bifocal", 1800 },
            { "Progressive", 3200 }
        };

        public static readonly Dictionary<string, decimal> Coatings = new()
        {
            { "None", 0 },
            { "Anti-Reflective", 600 },
            { "Blue-Cut", 900 },
            { "Photochromic", 2400 }
        };
    }
}