#region GenericNameSpaces
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
#endregion GenericNameSpaces

namespace BrowserInterface
{
    /// <summary>
    /// A simple class, which simplifies opening a <see cref="HttpMethod.Get"/> URL in the (default) browser of the user, optionally with query parameters. <br/>
    /// For (hopefully) obvious reasons, <see cref="HttpMethod.Post"/> is not supported. <br/>
    /// It is encouraged to use the <see cref="BrowserHandler"/> as service, or inside a <see langword="using"/> statement, so no memory leaks occur, however small.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
    public sealed class BrowserHandler : IDisposable
    {
        /// <summary>
        /// The <see cref="Array"/> of <see cref="char"/> which should be ignored for windows.
        /// </summary>
        private static readonly char[] _winChars = new char[6]
        {
            '\n',
            '\r',
            '&',
            '^',
            '\\',
            ';'
        };

        /// <summary>
        /// The <see cref="Process"/> used to start a browser instance with.
        /// </summary>
        private readonly Process _process;

        /// <summary>
        /// The <see cref="StringBuilder"/> used to construct the final command.
        /// </summary>
        private readonly StringBuilder _stringBuilder;

        /// <summary>
        /// <see langword="true"/> iff this <see cref="BrowserHandler"/> has already been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Constructs a new <see cref="BrowserHandler"/>, by creating several resources that are required for accessing the browser.
        /// </summary>
        public BrowserHandler()
        {
            this._process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };

            this._stringBuilder = new StringBuilder();
        }

        /// <summary>
        /// Opens the <paramref name="url"/> in the default browser of the user, with the provided <paramref name="queryParams"/>. <br/>
        /// This method works on mac, unix and windows.
        /// Elements in the <paramref name="queryParams"/> dictionary will be converted to their string representation, but are <see cref="object"/> to allow for more concise method calls.
        /// </summary>
        /// <param name="url">The url to open.</param>
        /// <param name="queryParams">The query parameters to append to the url.</param>
        /// <exception cref="PlatformNotSupportedException">If the platform is not unix, windows or mac.</exception>
        /// <exception cref="FormatException">If the <paramref name="url"/> is not a http or https url.</exception>
        public void OpenUrl(string url, Dictionary<string, object>? queryParams = null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                this.OpenUrlWindows(this.SanitizeInput(_winChars, url, "^&", queryParams));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                this.OpenUrlUnix(this.SanitizeInput(Array.Empty<char>(), url, "&", queryParams));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                this.OpenUrlUnix(this.SanitizeInput(Array.Empty<char>(), url, "&", queryParams));
            }
            else
            {
                throw new PlatformNotSupportedException($"{nameof(BrowserHandler)} does not support opening urls for {RuntimeInformation.OSDescription}!");
            }
        }

        /// <summary>
        /// Sanitizes the input url and query parameters, to circumvent possible security holes.
        /// </summary>
        /// <param name="forbiddenCharacters">The characters that are not allowed in the url or the query parameter's string repsentation.</param>
        /// <param name="url">The url that will be sanitized.</param>
        /// <param name="queryParams">The (optional) query parameters that will be sanitized.</param>
        /// <returns>A <see cref="string"/> which contains the sanitized url and optionally appended query parameters.</returns>
        /// <exception cref="InvalidOperationException">Thrown iff key colission occurs during key parameter sanitization.</exception>
        /// <exception cref="FormatException">If the <paramref name="url"/> is not a http or https url.</exception>
        private string SanitizeInput(char[] forbiddenCharacters, string url, string paramSeparator, Dictionary<string, object>? queryParams = null)
        {
            string urlOut = url.Filter(forbiddenCharacters);

            try
            {
                Uri uri = new Uri(urlOut);

                if (uri.Scheme is not "https" and not "http")
                {
                    throw new ArgumentException($"Expected http(s) url, got {uri.Scheme}!");
                }
            }
            catch (FormatException ex)
            {
                throw new FormatException($"Malformed url: {ex.Message}!");
            }

            this._stringBuilder.Clear();
            this._stringBuilder.Append(urlOut);

            // Note: once dll importing is used on windows, sanitization can be removed entirely.
            // Coalescing should still be done, which might still provide an InvalidOperationException.
            // Allowing object is useful, because it makes for more concise function calls (with string builders, or streams, for instance).

            Dictionary<string, string> paramOut = new Dictionary<string, string> { };

            if (queryParams is Dictionary<string, object> { Count: > 0 } @params &&
                @params.Select(kvp => paramOut.TryAdd(kvp.Key.Filter(forbiddenCharacters),
                                                      kvp.Value.ToString().Filter(forbiddenCharacters)))
                       .Any(k => !k))
            {
                throw new InvalidOperationException($"Coalescing [{queryParams}] resulted in key colission!");
            }

            if (paramOut.Count > 0)
            {
                this._stringBuilder.Append('?');

                foreach (KeyValuePair<string, string> kvp in paramOut)
                {
                    this._stringBuilder.Append(kvp.Key);
                    this._stringBuilder.Append('=');
                    this._stringBuilder.Append(kvp.Value);
                    this._stringBuilder.Append(paramSeparator);
                }

                this._stringBuilder.Length -= paramSeparator.Length;
            }

            return this._stringBuilder.ToString();
        }

        /// <summary>
        /// Executes <paramref name="command"/> to open a <paramref name="url"/>, in the default web browser of the user. <br/>
        /// This method is platform independant. <br/>
        /// </summary>
        /// <param name="url">The url to open.</param>
        private void OpenUrlRaw(string command, string url)
        {
            this._process.StartInfo.FileName = command;
            this._process.StartInfo.ArgumentList.Add(url);
            this._process.Start();
            this._process.WaitForExit();
        }

        /// <summary>
        /// Opens a single <see cref="HttpMethod.Get"/> <paramref name="url"/>, in the default web browser of the user. <br/>
        /// This method only functions on mac.
        /// </summary>
        /// <param name="url">THe url to open.</param>
        private void OpenUrlMac(string url) => this.OpenUrlRaw("open", url);

        /// <summary>
        /// Opens a single <see cref="HttpMethod.Get"/> <paramref name="url"/>, in the default web browser of the user. <br/>
        /// This method only functions on unix.
        /// </summary>
        /// <param name="url">THe url to open.</param>
        /// <param name="queryParams">The query parameters to append.</param>
        private void OpenUrlUnix(string url) => this.OpenUrlRaw("xdg-open", url);

        /// <summary>
        /// Opens a single <see cref="HttpMethod.Get"/> <paramref name="url"/>, in the default web browser of the user. <br/>
        /// It does this by opening a shell, and then executing the 'start' command in it, followed by the url.
        /// This method only functions on windows.
        /// </summary>
        /// <param name="url">The url to open.</param>
        private void OpenUrlWindows(string url)
        {
            this._process.StartInfo.FileName = "cmd";
            this._process.Start();
            this._process.StandardInput.WriteLine($"start {url}");
            this._process.StandardInput.Flush();
            this._process.StandardInput.Close();
            this._process.WaitForExit();
        }

        /// <summary>
        /// Disposes the <see cref="_process"/>, and removes contents of the <see cref="_stringBuilder"/>.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if this <see cref="BrowserHandler"/> is disposing.</param>
        private void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    this._process.Dispose();
                }

                this._stringBuilder.Clear();
                this._disposed = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public sealed override string ToString() => $"{this._process.ProcessName}-{this._stringBuilder.Length}-{this._disposed}";

        public sealed override int GetHashCode() => HashCode.Combine(this._process, this._stringBuilder, this._disposed);
    }
}
