namespace Attendence_System.Extensions
{
    /// <summary>
    /// Extensions for fuzzy Arabic text searching.
    /// Normalizes common Arabic character variants before comparison.
    /// </summary>
    public static class ArabicSearchExtensions
    {
        /// <summary>
        /// Normalizes an Arabic string by:
        /// - Unifying all alef forms (أ إ آ ٱ) to bare alef (ا)
        /// - Mapping teh marbuta (ة) to heh (ه)
        /// - Mapping dotless yeh (ى) to yeh (ي)
        /// - Stripping tashkeel (harakat) diacritics
        /// - Trimming whitespace
        /// </summary>
        public static string NormalizeArabic(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var result = input.Trim();

            // Unify alef variants → bare alef
            result = result
                .Replace('أ', 'ا')
                .Replace('إ', 'ا')
                .Replace('آ', 'ا')
                .Replace('ٱ', 'ا');

            // Teh marbuta → heh
            result = result.Replace('ة', 'ه');

            // Dotless yeh → yeh
            result = result.Replace('ى', 'ي');

            // Strip Arabic diacritics (tashkeel: U+064B – U+065F)
            var chars = result.ToCharArray();
            var filtered = System.Array.FindAll(chars, c => c < '\u064B' || c > '\u065F');
            result = new string(filtered);

            // Remove ALL whitespace so spacing differences are ignored entirely
            // e.g. "محمد احمد" == "محمداحمد"
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", "");

            return result;
        }

        /// <summary>
        /// Returns true if <paramref name="haystack"/> contains the
        /// normalized form of <paramref name="needle"/>, case-insensitively.
        /// </summary>
        public static bool ContainsArabicFuzzy(this string? haystack, string needle)
        {
            if (haystack == null) return false;
            if (string.IsNullOrWhiteSpace(needle)) return true;

            var normalizedHaystack = haystack.NormalizeArabic();
            var normalizedNeedle = needle.NormalizeArabic();

            return normalizedHaystack.Contains(normalizedNeedle, StringComparison.OrdinalIgnoreCase);
        }
    }
}
