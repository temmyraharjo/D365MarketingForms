using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace D365MarketingForms.Server.Utilities
{
    /// <summary>
    /// Utility class for slug generation and decoding
    /// </summary>
    public static class SlugUtility
    {
        private static readonly Dictionary<string, string> _specialCharMap = new()
        {
            // Common replacements for language-specific characters
            { "ä", "ae" }, { "ö", "oe" }, { "ü", "ue" }, { "ß", "ss" },
            { "æ", "ae" }, { "ø", "oe" }, { "å", "aa" }, { "ñ", "n" }
        };

        /// <summary>
        /// Generates a URL-friendly slug from a string
        /// </summary>
        /// <param name="input">The string to convert to a slug</param>
        /// <param name="maxLength">The maximum length of the slug (default: 100)</param>
        /// <returns>A URL-friendly slug</returns>
        public static string GenerateSlug(string input, int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Save original text for the dictionary
            StoreMappingForDeslug(input);

            // Replace known special characters with their ASCII equivalents
            foreach (var kvp in _specialCharMap)
            {
                input = input.Replace(kvp.Key, kvp.Value, StringComparison.OrdinalIgnoreCase);
            }

            // Convert to lowercase and normalize
            var normalizedString = input.ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            // Remove diacritics (accents)
            var stringBuilder = new StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            
            // Replace spaces and non-word characters with hyphens
            var slug = Regex.Replace(stringBuilder.ToString(), @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"[\s-]+", "-");
            
            // Trim hyphens from start and end
            slug = slug.Trim('-');
            
            // Ensure the slug doesn't exceed the maximum length
            if (slug.Length > maxLength)
                slug = slug.Substring(0, maxLength).TrimEnd('-');
            
            return slug;
        }

        /// <summary>
        /// Ensures a slug is unique by appending a number if necessary
        /// </summary>
        public static string EnsureUniqueSlug(string baseSlug, IEnumerable<string> existingSlugs, int maxLength = 100)
        {
            if (!existingSlugs.Contains(baseSlug))
                return baseSlug;
            
            var slugSet = new HashSet<string>(existingSlugs);
            var uniqueSlug = baseSlug;
            var counter = 1;
            
            while (slugSet.Contains(uniqueSlug))
            {
                var suffix = $"-{counter}";
                uniqueSlug = baseSlug.Length + suffix.Length > maxLength
                    ? $"{baseSlug.Substring(0, maxLength - suffix.Length)}{suffix}"
                    : $"{baseSlug}{suffix}";
                
                counter++;
            }
            
            return uniqueSlug;
        }

        // Thread-safe in-memory storage for original text to slug mappings
        private static readonly ConcurrentDictionary<string, string> _slugToTextMap = new();

        /// <summary>
        /// Stores the original text and its generated slug for later de-slugging
        /// </summary>
        private static void StoreMappingForDeslug(string original)
        {
            var slug = GenerateSlugCore(original);
            _slugToTextMap.TryAdd(slug, original);
        }

        // Core slug generation logic without storing mapping
        private static string GenerateSlugCore(string input)
        {
            var normalizedString = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();
            
            foreach (var c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }
            
            var slug = Regex.Replace(stringBuilder.ToString(), @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"[\s-]+", "-");
            return slug.Trim('-');
        }

        /// <summary>
        /// Attempts to convert a slug back to its original text
        /// </summary>
        /// <param name="slug">The slug to decode</param>
        /// <returns>The original text if found, or a best-effort de-slugged version</returns>
        public static string DeSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return string.Empty;

            // If we have the exact original, return it
            if (_slugToTextMap.TryGetValue(slug, out var originalText))
                return originalText;

            // Otherwise, do a best-effort de-slugging
            // Replace hyphens with spaces and capitalize each word
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(slug.Replace('-', ' '));
        }

        /// <summary>
        /// Clears the slug-to-text mapping cache
        /// </summary>
        public static void ClearMappingCache()
        {
            _slugToTextMap.Clear();
        }
    }
}