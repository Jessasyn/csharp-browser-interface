# csharp-browser-interface
A simple wrapper to easily open a browser from within C#, on any platform.

Specifically, to open a url (optionally, with query parameters), on the *default* browser of whatever OS it is running on (be that mac, unix, or windows, more obscure versions *may* not work, as described in `Compatability`). 
To do this, it uses functionality exposed by System.Diagnostics.Process to start a process, running in the background, which targets some specific command that opens a browser. 
This command is platform-specific.

Input sanitation exists; characters which are used in the shell of the corresponding system the program is running on will be removed. In addition, because the commands used to open the browser accept *any* URI, a check is present which forces the scheme of the URI to be either HTTP or HTTPS. Still, it might be possible to obtain shell access, given the right input, so *never* allow input from the user to directly reach the `OpenUrl` call.

Also, do note that you will only need this library if you really need to open a browser tab for the user. To perform a normal GET request, simply use the HttpClient implementation of C# itself.

## Compatibility

### Windows
On any supported version of windows, this library will fuction.

### Mac
On any supported version of mac, this library will function. 
This is of course assuming that mac does not eventually disallow the use of 3rd party software completely.

### Linux
The available shells on a given linux distribution will differ. Therefore, the library attempts to read out the contents of '/etc/shells' to obtain a list of all available shells on the distribution.
On some distributions, however, this file does not exist. If it does not, the library will fall back to '/bin/sh'. This means that if a distribution does not have the '/etc/shells' file *and* it does not have '/bin/sh' as shell, the library will **not** function.

## Why use a shell?
Because of windows (and possibly mac as well).

On linux, we can target binary executables directly (i.e. '/bin/xdg-open', the executable which is used to actually open the url), but on windows, the command 'start' is used. There is no executable called 'start' readily available (as in, trying to run 'start' does not work, nor does a find return it).
To avoid code duplication, and to keep everything as platform-agnostic as possible, we therefore open a shell, and run the appropriate command in it.
This may or may not also be the case in OSX, I have not checked. It would not surprise me, though.

I am currently looking into the possibilities of somehow importing the start function from whatever dll it is hidden in, and using it that way.

## Example usage:

```csharp
using BrowserHandler handler = new BrowserHandler();

handler.OpenUrl("https://www.google.com", new Dictionary<string, object> { { "q", "string to search for" } });
```

Because the handler uses a `Process` instance internally, you are encouraged to properly `Dispose` of the `BrowserHandler`, once it is no longer needed. This can be done through a `using` statement as shown above, or otherwise by using it in a class which implements `IDisposable`, and which calls `handler.Dispose()` in its own implementation of `Dispose()`.
