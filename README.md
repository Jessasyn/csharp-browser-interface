# csharp-browser-interface
A simple wrapper to easily open a browser from within C#, on any platform.

Specifically, to open a url (optionally, with query parameters), on the *default* browser of whatever OS it is running on (be that mac, unix, or windows, more obscure versions *may* not work, as described in `Compatability`). 
To do this, it uses functionality exposed by System.Diagnostics.Process to start a process, running in the background, which targets some specific command that opens a browser. 
This command is platform-specific.

There is only input sanitation in the sense that all query parameters can be of any type, and are coalesced to string, and are required to be unique. More sanitation is not performed because the direct functions / executables that open the browser are targeted. Still, it is not advisable to let user input directly reach a call to `OpenUrl`.

Also, do note that you will only need this library if you really need to open a browser tab for the user. To perform a normal GET request, simply use the HttpClient implementation of C# itself.

## Compatibility

### Windows
On any supported version of windows, which have the `ShellExecuteA` function in `Shell32.dll`, this library will function.
As far as I am aware, this means all supported versions of windows.

### Mac
On any supported version of mac which has the file `/usr/bin/open`, this library will function.
As far as I am aware, this means all supported versions of mac.

### Linux
On any supported version of linux which has the file `/usr/bin/xdg-open`, this library will function.
As far as I am aware, this means all supported versions of linux.

## Example usage:

```csharp
using BrowserHandler handler = new BrowserHandler();

handler.OpenUrl("https://www.google.com", new Dictionary<string, object> { { "q", "string to search for" } });
```

Because the handler uses a `Process` instance internally, you are encouraged to properly `Dispose` of the `BrowserHandler`, once it is no longer needed. This can be done through a `using` statement as shown above, or otherwise by using it in a class which implements `IDisposable`, and which calls `handler.Dispose()` in its own implementation of `Dispose()`.
