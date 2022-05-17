# csharp-browser-interface
A simple wrapper to easily open a browser from within C#, on any platform.

Specifically, to open a url (optionally, with query parameters), on the *default* browser of whatever OS it is running on (be that mac, unix, or windows, more obscure versions might not work). To do this, it uses functionality exposed by System.Diagnostics.Process to start a process, running in the background, which targets some specific command that opens a browser. This command is platform-specific.

Input sanitation exists; characters which are used in the shell of the corresponding system the program is running on will be removed. In addition, because the commands used to open the browser accept *any* URI, a check is present which forces the scheme of the URI to be either HTTP or HTTPS. Still, it might be possible to obtain shell access, given the right malicious input, so *never* allow input from the user to directly reach the `OpenUrl` call.

Also, do note that you will only need this library if you really need to open a browser tab for the user. To perform a normal GET request, simply use the HttpClient implementation of C# itself.
