# csharp-browser-interface
A simple wrapper to easily open a browser from within C#, on any platform.

Specifically, to open a url (optionally, with query parameters), on the *default* browser of whatever OS it is running on (be that mac, unix, or windows, more obscure versions might not work). To do this, it uses functionality exposed by System.Diagnostics.Process to start a shell, running in the background, which targets some specific command that opens a browser. This command is platform-specific.

I have attempted to include input sanitation, but because this approach uses a normal shell, it can very well be possible to escape the command, and gain shell access on any system that the code is run. Therefore, **never** allow user input to directly reach the OpenUrl call.

Also, do note that you will only need this library if you really need to open a browser tab for the user. To perform a GET request, simply use the HttpClient.
