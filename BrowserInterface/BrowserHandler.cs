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
        /// The <see cref="Array"/> of <see cref="char"/> which should be ignored for unix.
        /// </summary>
        private static readonly char[] _unixChars = new char[6]
        {
            '\n',
            '|',
            '&',
            '\r',
            '\\',
            ';'
        };

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
        /// The <see cref="Array"/> of <see cref="char"/> which should be ignored for osx.
        /// </summary>
        private static readonly char[] _macChars = new char[5]
        {
            '\n',
            '&',
            '|',
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
        /// <exception cref="FormatException">Thrown iff the <paramref name="url"/> is not a http or https url, or key parameter sanitization results in key colission.</exception>
        public void OpenUrl(string url, Dictionary<string, object>? queryParams = null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                this.OpenUrlWindows(url, queryParams);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                this.OpenUrlUnix(url, queryParams);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                this.OpenUrlMac(url, queryParams);
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
        /// <returns>A <see cref="ValueTuple{string, Dictionary{string, string}}"/> which contains the sanitized url and query parameters.</returns>
        /// <exception cref="FormatException">Thrown iff the <paramref name="url"/> is not a http or https url, or key parameter sanitization results in key colission.</exception>
        private static (string, Dictionary<string, string>) SanitizeInput(char[] forbiddenCharacters, string url, Dictionary<string, object>? queryParams = null)
        {
            string urlOut = url.Filter(forbiddenCharacters);

            try
            {
                Uri uri = new Uri(urlOut);

                if (uri.Scheme is not "https" and not "http")
                {
                    throw new FormatException($"Expected http(s) url, got {uri.Scheme}!");
                }
            }
            catch(FormatException ex)
            {
                throw new FormatException($"Malformed url: {ex.Message}!");
            }

            Dictionary<string, string> paramOut = new Dictionary<string, string> { };

            if (queryParams is Dictionary<string, object> { Count: > 0 } @params &&
                @params.Select(kvp => paramOut.TryAdd(kvp.Key.Filter(forbiddenCharacters),
                                                      kvp.Value.ToString().Filter(forbiddenCharacters)))
                       .Any(k => !k))
            {
                throw new FormatException($"Coalescing [{queryParams}] resulted in key colission!");
            }

            return (urlOut, paramOut);
        }

        /// <summary>
        /// Using the provided <paramref name="shellName"/>, executes <paramref name="command"/> to open a <paramref name="url"/>, with the provided <paramref name="queryParams"/>, in the default web browser of the user. <br/>
        /// This method is platform independant. <br/>
        /// </summary>
        /// <param name="url">THe url to open.</param>
        /// <param name="queryParams">The query parameters to append.</param>
        private void OpenUrlRaw(string shellName, string command, string url, string paramSeparator, char[] forbiddenCharacters, Dictionary<string, object>? queryParams = null)
        {
            (url, Dictionary<string, string> sanitizedParams) = SanitizeInput(forbiddenCharacters, url, queryParams);

            _ = this._stringBuilder.Clear()
                                   .Append(command)
                                   .Append(' ')
                                   .Append(url);

            if (sanitizedParams.Count > 0)
            {
                _ = this._stringBuilder.Append('?');

                foreach (KeyValuePair<string, string> kvp in sanitizedParams)
                {
                    _ = this._stringBuilder.Append(kvp.Key)
                                           .Append('=')
                                           .Append(kvp.Value)
                                           .Append(paramSeparator);
                }

                this._stringBuilder.Length -= paramSeparator.Length;
            }

            this._process.StartInfo.FileName = shellName;
            _ = this._process.Start();
            this._process.StandardInput.WriteLine(this._stringBuilder);
            this._process.StandardInput.Flush();
            this._process.StandardInput.Close();
            this._process.WaitForExit();
        }

        /// <summary>
        /// Opens a single <see cref="HttpMethod.Get"/> <paramref name="url"/>, with the provided <paramref name="queryParams"/>, in the default web browser of the user. <br/>
        /// This method only functions on mac.
        /// </summary>
        /// <param name="url">THe url to open.</param>
        /// <param name="queryParams">The query parameters to append.</param>
        private void OpenUrlMac(string url, Dictionary<string, object>? queryParams = null) => this.OpenUrlRaw("bash", "open", url, "\\&", _macChars, queryParams);

        /// <summary>
        /// Opens a single <see cref="HttpMethod.Get"/> <paramref name="url"/>, with the provided <paramref name="queryParams"/>, in the default web browser of the user. <br/>
        /// This method only functions on unix.
        /// </summary>
        /// <param name="url">THe url to open.</param>
        /// <param name="queryParams">The query parameters to append.</param>
        private void OpenUrlUnix(string url, Dictionary<string, object>? queryParams = null) => this.OpenUrlRaw("bash", "xdg-open", url, "\\&", _unixChars, queryParams);

        /// <summary>
        /// Opens a single <see cref="HttpMethod.Get"/> <paramref name="url"/>, with the provided <paramref name="queryParams"/>, in the default web browser of the user. <br/>
        /// This method only functions on windows.
        /// </summary>
        /// <param name="url">The url to open.</param>
        /// <param name="queryParams">The query parameters to append.</param>
        private void OpenUrlWindows(string url, Dictionary<string, object>? queryParams = null) => this.OpenUrlRaw("cmd", "start", url, "^&", _winChars, queryParams);

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

                _ = this._stringBuilder.Clear();
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
