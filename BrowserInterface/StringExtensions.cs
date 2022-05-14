namespace BrowserInterface
{
    /// <summary>
    /// Contains an extension method on <see cref="string"/>s, which allows for the consice filtering of <see cref="string"/>s based on forbidden characters.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Removes all instances of <paramref name="toRemove"/> from <paramref name="string"/>.
        /// </summary>
        /// <param name="string">The <see cref="string"/> that will be filtered. Can be <see langword="null"/>.</param>
        /// <param name="toRemove">The array of <see cref="char"/> that will be removed from the <paramref name="string"/>.</param>
        /// <returns>The filtered <paramref name="string"/>, or <see cref="string.Empty"/>, if the input was <see langword="null"/>.</returns>
        public static string Filter(this string? @string, char[] toRemove)
        {
            if (@string is null)
            {
                return string.Empty;
            }

            return new string(@string.Where(x => !toRemove.Contains(x)).ToArray());
        }
    }
}
