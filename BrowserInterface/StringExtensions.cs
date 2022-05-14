namespace BrowserInterface
{
    /// <summary>
    /// Contains an extension method on strings, which allows for the consice filtering of strings based on forbidden characters.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Removes all instances of <paramref name="toRemove"/> from <paramref name="string"/>.
        /// </summary>
        /// <param name="string"></param>
        /// <param name="toRemove"></param>
        /// <returns></returns>
        public static string Filter(this string? @string, List<char> toRemove)
        {
            if(@string is null)
            {
                return string.Empty;
            }

            return new string(@string.Where(x => !toRemove.Contains(x)).ToArray());
        }
    }
}
