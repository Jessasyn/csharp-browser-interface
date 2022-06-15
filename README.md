# csharp-browser-interface
A simple wrapper to easily open a browser from within C#, on any platform.

Specifically, to open a url (optionally, with query parameters), on the *default* browser of whatever OS it is running on (be that mac, unix, or windows, more obscure versions *may* not work, as described in `Compatability`). 
To do this, it uses functionality exposed by System.Diagnostics.Process to start a process, running in the background, which targets some specific command that opens a browser. 
This command is platform-specific.

Input sanitation exists; on windows, a shell is opened, and characters that have special meaning within this shell are filtered out. Though this should prevent malicious input, please *never* allow user input to reach the `OpenUrl` function directly. This also goes for linux and mac! Malicious input could exploit the binaries used there, regardless of how safe this library is.

Also, do note that you will only need this library if you really need to open a browser tab for the user. To perform a normal GET request, simply use the HttpClient implementation of C# itself.

## Compatibility

### Windows
On any supported version of windows, this library will fuction.

### Mac
On any supported version of mac which has the file '/usr/bin/open', this library will function.
As far as I am aware, this means all supported versions of mac.

### Linux
On any supported version of linux which has the file '/usr/bin/xdg-open', this library will function.
As far as I am aware, this means all supported versions of linux.

## What is happening with windows?
Windows is a bit special. With Linux and Mac, we can just target some binary file directly ('xdg-open' and 'open', respectively). With Windows, that is not possible. The 'start' command is hidden somewhere within a dll file, and so the easiest way to access it is to open a terminal, and just 'type' the start command followed by the requested url.

In the future, I will be looking at the possibility of importing this function through `DllImport`.

## Example usage:

```csharp
using BrowserHandler handler = new BrowserHandler();

handler.OpenUrl("https://www.google.com", new Dictionary<string, object> { { "q", "string to search for" } });
```

Because the handler uses a `Process` instance internally, you are encouraged to properly `Dispose` of the `BrowserHandler`, once it is no longer needed. This can be done through a `using` statement as shown above, or otherwise by using it in a class which implements `IDisposable`, and which calls `handler.Dispose()` in its own implementation of `Dispose()`.
