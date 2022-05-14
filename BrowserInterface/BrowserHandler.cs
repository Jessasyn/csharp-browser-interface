#region GenericNameSpaces
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
#endregion GenericNameSpaces

namespace BrowserInterface
{
    /// <summary>
    /// A simple class, which simplifies opening a GET URL in the browser of the user, optionally with query parameters. <br/>
    /// For (hopefully) obvious reasons, POST is not supported.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
    public sealed class BrowserHandler : IDisposable
    {
        /// <summary>
        /// The list of characters which should be ignored for unix.
        /// </summary>
        private static readonly List<char> _unixTerminalCharacters = new List<char>
        {
            '\n',
            '|',
            '&',
            '\r',
            '\\',
            ';'
        };

        /// <summary>
        /// The list of characters which should be ignored for windows.
        /// </summary>
        private static readonly List<char> _windowsTerminalCharacters = new List<char>
        {
            '\n',
            '\r',
            '&',
            '^',
            '\\',
            ';'
        };

        /// <summary>
        /// The list of characters which should be ignored for osx.
        /// </summary>
        private static readonly List<char> _macTerminalCharacters = new List<char>
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
                    UseShellExecute = true
                }
            };

            this._stringBuilder = new StringBuilder();
        }

        /// <summary>
        /// Opens the <paramref name="url"/> in the default browser of the user, with the provided <paramref name="queryParams"/>. <br/>
        /// This method works on mac, unix and windows.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="queryParams"></param>
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
        }

        /// <summary>
        /// Sanitizes the input url and query parameters, to circumvent possible security holes.
        /// </summary>
        /// <param name="forbiddenSubStrings"></param>
        /// <param name="url"></param>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static (string, Dictionary<string, string>) SanitizeInput(List<char> forbiddenCharacters, string url, Dictionary<string, object>? queryParams = null)
        {
            string urlOut = url.Filter(forbiddenCharacters);

            Dictionary<string, string> paramOut = new Dictionary<string, string> { };

            if (queryParams is Dictionary<string, object> { Count: > 0 } @params && 
                @params.Select(kvp => paramOut.TryAdd(kvp.Key.Filter(forbiddenCharacters), 
                                                      kvp.Value.ToString().Filter(forbiddenCharacters)))
                       .Any(k => !k))
            {
                throw new InvalidOperationException($"Coalescing [{queryParams}] resulted in key colission!");
            }

            return (urlOut, paramOut);
        }

        /// <summary>
        /// Using the provided <paramref name="shellName"/>, executes <paramref name="command"/> to open a <paramref name="url"/>, with the provided <paramref name="queryParams"/>, in the default web browser of the user. <br/>
        /// This method is platform independant.
        /// </summary>
        /// <param name="url">THe url to open.</param>
        /// <param name="queryParams">The query parameters to append.</param>
        private void OpenUrlRaw(string shellName, string command, string url, string paramSeparator, Dictionary<string, string>? queryParams = null)
        {
            this._stringBuilder.Clear();
            this._stringBuilder.Append(command);
            this._stringBuilder.Append(' ');
            this._stringBuilder.Append(url);

            if (queryParams is Dictionary<string, string> { Count: > 0 } @params)
            {
                this._stringBuilder.Append('?');

                foreach (KeyValuePair<string, string> kvp in @params)
                {
                    this._stringBuilder.Append(kvp.Key);
                    this._stringBuilder.Append('=');
                    this._stringBuilder.Append(kvp.Value);
                    this._stringBuilder.Append(paramSeparator);
                }

                this._stringBuilder.Length -= paramSeparator.Length;
            }

            this._process.StartInfo.FileName = shellName;
            this._process.Start();
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
        private void OpenUrlMac(string url, Dictionary<string, object>? queryParams = null)
        {
            (url, Dictionary<string, string> sanitizedParams) = SanitizeInput(_macTerminalCharacters, url, queryParams);

            this.OpenUrlRaw("bash", "open", url, "\\&", sanitizedParams);
        }

        /// <summary>
        /// Opens a single <see cref="HttpMethod.Get"/> <paramref name="url"/>, with the provided <paramref name="queryParams"/>, in the default web browser of the user. <br/>
        /// This method only functions on unix.
        /// </summary>
        /// <param name="url">THe url to open.</param>
        /// <param name="queryParams">The query parameters to append.</param>
        private void OpenUrlUnix(string url, Dictionary<string, object>? queryParams = null)
        {
            (url, Dictionary<string, string> sanitizedParams) = SanitizeInput(_unixTerminalCharacters, url, queryParams);

            this.OpenUrlRaw("bash", "xdg-open", url, "\\&", sanitizedParams);
        }

        /// <summary>
        /// Opens a single <see cref="HttpMethod.Get"/> <paramref name="url"/>, with the provided <paramref name="queryParams"/>, in the default web browser of the user. <br/>
        /// This method only functions on windows.
        /// </summary>
        /// <param name="url">The url to open.</param>
        /// <param name="queryParams">The query parameters to append.</param>
        private void OpenUrlWindows(string url, Dictionary<string, object>? queryParams = null)
        {
            (url, Dictionary<string, string> sanitizedParams) = SanitizeInput(_windowsTerminalCharacters, url, queryParams);

            this.OpenUrlRaw("cmd", "start", url, "^&", sanitizedParams);
        }

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
