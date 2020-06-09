# FailedRequestTraceViewer
Windows program for more easily viewing Failed Request Trace files (WPF) 

This is the second version uploaded, very rough but hopefully adds value.

The program will start listening for requests from IIS Failed Request Trace folders.  Note also that despite IIS configuration of how many requests to save, the program will start caching them in memory, and therefore the number of requests that the program loads is potentially unlimited.  Once the program ends, you won't be able to see those requests again if they are no longer on the hard drive.  As a result, do not leave this running and forget about it, or it will end up consuming all the memory.

Use the File... Import Folder option if you are reading traces that were already captured from another computer, or moved to another folder.  The program will only read files from the folder itself, not sub-folders.  The program will not pick up new files placed in the folder after the folder is imported.

The UI:

Key pieces of information are filled into columns in a grid in the top half of the window.  There are too many columns, but no option yet to hide ones you don't want to see.  First thing I usually do is maximize the window, and then resize the URL column to see better.

The bottom half is embedded web browser.  This can be turned off by clicking View... Web Preview.  When you single-click on a row, you should see the web representation of the request on the bottom.  There isn't much space here, so if you want to open it in the default application (hopefully the browser), double-click on the row, and it will launch in the default application.  You can also use keyboard controls like arrows to navigate the grid, whatever works with the built-in WPF Grid control.  Multiple-selection is disabled.  You may want to re-associate .xml extension with a browser.  If it doesn't look pretty, you are probably missing the freb.xsl file that is in the Failed Request Trace folder.  You may have accidentally deleted it.  IIS can put this file back, it may be recycling W3, or turning on/off the Failed Request Tracing, or something like that.

CURRENT ISSUES:

No remembered settings.
No column selection to hide or show columns
No display filtering which would help see only particular app pools, particular URLs / results, etc.
No capture filtering which would help reduce the memory overhead
No setting to limit how many requests are stored in memory
No clean up of files saved to the %TEMP% folder
