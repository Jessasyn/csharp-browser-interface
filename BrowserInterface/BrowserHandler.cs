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
    public sealed class BrowserHandler : IDisposable
    {
        /// <summary>
        /// The <see cref="Process"/> used to start a browser instance with.
        /// </summary>
        private readonly Process _process;

        /// <summary>
        /// The <see cref="StringBuilder"/> used to construct the final url.
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
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };

            this._stringBuilder = new StringBuilder();
        }

        /// <summary>
        /// Opens the <paramref name="urlBase"/> in the default browser of the user, with the provided <paramref name="queryParams"/>. <br/>
        /// This method works on mac, unix and windows.
        /// Keys and values in the <paramref name="queryParams"/> dictionary will be converted to their string representation, but are <see cref="object"/> to allow for more concise method calls.
        /// </summary>
        /// <param name="urlBase">The url to open.</param>
        /// <param name="queryParams">The query parameters to append to the url.</param>
        /// <returns> A <see cref="bool"/>, which indicates whether the url was opened properly or not.</returns>
        /// <exception cref="PlatformNotSupportedException">If the platform is not unix, windows or mac.</exception>
        /// <exception cref="ArgumentException">If the <paramref name="urlBase"/> is not a http or https url.</exception>
        /// <exception cref="FormatException"> If the <paramref name="urlBase"/> is not a valid <see cref="Uri"/>. </exception>
        /// <exception cref="InvalidOperationException"> If the <paramref name="queryParams"/> contain duplicate keys after coalescing. </exception>
        public bool OpenUrl(string urlBase, Dictionary<object, object>? queryParams = null)
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(nameof(BrowserHandler));
            }

            string url = this.FormUrl(urlBase, queryParams);

            int exitCode;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                exitCode = OpenUrlWindows(url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                exitCode = this.OpenUrlUnix(url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                exitCode = this.OpenUrlMac(url);
            }
            else
            {
                throw new PlatformNotSupportedException($"{nameof(BrowserHandler)} does not support opening urls for {RuntimeInformation.OSDescription}!");
            }

            if (exitCode != 0)
            {
                Console.Error.WriteLine($"Warning: opening [{url}] failed with non-zero error code [{exitCode}]!");
            }

            return exitCode == 0;
        }

        /// <summary>
        /// Forms a url out of a <paramref name="urlBase"/> and an optional collection of <paramref name="queryParams"/>.
        /// </summary>
        /// <param name="urlBase">The base of the url.</param>
        /// <param name="queryParams">The (optional) query parameters that will be sanitized.</param>
        /// <returns>A <see cref="string"/> which contains the sanitized url and optionally appended query parameters.</returns>
        /// <exception cref="ArgumentException">If the <paramref name="urlBase"/> is not a http or https url.</exception>
        /// <exception cref="FormatException"> If the <paramref name="urlBase"/> is not a valid <see cref="Uri"/>. </exception>
        /// <exception cref="InvalidOperationException"> If the <paramref name="queryParams"/> contain duplicate keys after coalescing. </exception>
        private string FormUrl(string urlBase, Dictionary<object, object>? queryParams = null)
        {
            this._stringBuilder.Clear()
                               .Append(urlBase);

            if (queryParams is Dictionary<object, object> { Count: > 0 } @params)
            {
                if (@params.Keys.GroupBy(k => k.ToString()).Any(c => c.Count() > 1))
                {
                    throw new InvalidOperationException($"Coalescing [{queryParams}] resulted in key colission!");
                }

                this._stringBuilder.Append('?');

                foreach (KeyValuePair<object, object> kvp in @params)
                {
                    this._stringBuilder.Append(kvp.Key)
                                       .Append('=')
                                       .Append(kvp.Value)
                                       .Append('&');
                }

                this._stringBuilder.Length -= 1;
            }

            Uri uri;

            try
            {
                uri = new Uri(this._stringBuilder.ToString());

                if (uri.Scheme is not "https" and not "http")
                {
                    throw new ArgumentException($"Expected http(s) url, got {uri.Scheme}!");
                }
            }
            catch (FormatException ex)
            {
                throw new FormatException($"Malformed url: {ex.Message}!");
            }

            return uri.ToString();
        }

        /// <summary>
        /// Executes <paramref name="command"/> to open a <paramref name="url"/>, in the default web browser of the user. <br/>
        /// This method works on *NIX. <br/>
        /// </summary>
        /// <returns>The exitcode of the command, after it has been run.</returns>
        /// <param name="url">The url to open.</param>
        private int OpenUrlRaw(string command, string url)
        {
            this._process.StartInfo.FileName = command;
            this._process.StartInfo.ArgumentList.Clear();
            this._process.StartInfo.ArgumentList.Add(url);
            this._process.Start();
            this._process.WaitForExit();

            return this._process.ExitCode;
        }

        /// <summary>
        /// Opens a single <see cref="HttpMethod.Get"/> <paramref name="url"/>, in the default web browser of the user. <br/>
        /// This method only functions on mac, and works by executing the 'open' file, with the <paramref name="url"/> as parameter.
        /// </summary>
        /// <returns>The exitcode of the command, after it has been run.</returns>
        /// <param name="url">The url to open.</param>
        private int OpenUrlMac(string url) => this.OpenUrlRaw("open", url);

        /// <summary>
        /// Opens a single <see cref="HttpMethod.Get"/> <paramref name="url"/>, in the default web browser of the user. <br/>
        /// This method only functions on unix, and works by executing the 'xdg-open' file, with the <paramref name="url"/> as parameter.
        /// </summary>
        /// <returns>The exitcode of the command, after it has been run.</returns>
        /// <param name="url">The url to open.</param>
        private int OpenUrlUnix(string url) => this.OpenUrlRaw("xdg-open", url);

        /// <summary>
        /// Opens a single <see cref="HttpMethod.Get"/> <paramref name="url"/>, in the default web browser of the user. <br/>
        /// This method only functions on windows and works by calling the "ShellExecute" function in the "Shell32.dll" file.
        /// </summary>
        /// <returns>0 iff the shellexecute was succesfull, 1 otherwise.</returns>
        /// <param name="url">The url to open.</param>
        private static int OpenUrlWindows(string url) => ShellExecute(0, null, url, null, null, 1) < 32 ? 1 : 0;

        /// <summary>
        /// Performs an operation on a specified file. <br/>
        /// Note: documentation from this function is taken directly from <seealso cref="https://docs.microsoft.com/nl-nl/windows/win32/api/shellapi/nf-shellapi-shellexecutea"/>.
        /// </summary>
        /// <param name="hWnd">A handle to the parent window used for displaying a UI or error messages. This value can be NULL if the operation is not associated with a window.</param>
        /// <param name="lpOperation">A pointer to a null-terminated string, referred to in this case as a verb, that specifies the action to be performed. The set of available verbs depends on the particular file or folder. Generally, the actions available from an object's shortcut menu are available verbs.</param>
        /// <param name="lpFile">A pointer to a null-terminated string that specifies the file or object on which to execute the specified verb. To specify a Shell namespace object, pass the fully qualified parse name. Note that not all verbs are supported on all objects. For example, not all document types support the "print" verb. If a relative path is used for the lpDirectory parameter do not use a relative path for lpFile.</param>
        /// <param name="lpParameters">If lpFile specifies an executable file, this parameter is a pointer to a null-terminated string that specifies the parameters to be passed to the application. The format of this string is determined by the verb that is to be invoked. If lpFile specifies a document file, lpParameters should be NULL.</param>
        /// <param name="lpDirectory">A pointer to a null-terminated string that specifies the default (working) directory for the action. If this value is NULL, the current working directory is used. If a relative path is provided at lpFile, do not use a relative path for lpDirectory.</param>
        /// <param name="nShowCmd">The flags that specify how an application is to be displayed when it is opened. If lpFile specifies a document file, the flag is simply passed to the associated application. It is up to the application to decide how to handle it. It can be any of the values that can be specified in the nCmdShow parameter for the ShowWindow function.</param>
        /// <returns>A <see cref="long"/>, indicating success if the value is greater than 32.</returns>
        [DllImport("Shell32.dll", BestFitMapping = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        private static extern long ShellExecute(int hWnd, string? lpOperation, string lpFile, string? lpParameters, string? lpDirectory, long nShowCmd);

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

        public sealed override int GetHashCode() => HashCode.Combine(this._process, this._stringBuilder, this._disposed, base.GetHashCode());
    }
}
