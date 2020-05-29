# FailedTraceRequestViewer
Windows program for more easily viewing Failed Trace Request requests (WPF) 

Initial version uploaded, very rough but hopefully adds value.

There is no toolbar, so there are three 'MessageBox' style pop ups at the beginning to ask information prior to the UI being loaded with Failed Trace Requests.

Question 1: "Do you want to add Failed Request Trace locations based on this computer's local IIS configuration?  (note, UI will be updated as new traces appear automatically)".

Generally you would want to say 'Yes' because you are in the middle of generating Failed Trace Requests in IIS.  It will read the web sites configuration of where the Failed Request Traces are being written, and load requests automatically from there.  It will be sorted based on the request start time.  The program will also start listening for requests from those folders, and the UI will automatically be updated.  There is no 'capture filters' here, so it could get volumunous.  Note also that despite IIS configuration of how many requests to save, we will start caching them in memory, and therefore the number of requests that we load is potentially unlimited.  Once the program ends, of course, you won't be able to see those requests again if they are no longer on the hard drive.  As a result, do not leave this running and forget about it, or it will end up consuming all the memory.

Question 2: "Do you want to add a local folder where Failed Request Traces exist?  (note 1: the UI will not automatically update, note 2: if the traces are from a different computer, the displayed Site information may be incorrect, note 3: times will appear in local time zone.)"

Generally this option is if you are reading traces that were already captured from another computer, or moved to another folder.  The program will only read files from the folder itself, not sub-folders.  The program will not pick up new files placed in the folder after the program has started up.

Question 3: "Do you want to strip unique identifiers when saving request files to disk? (may help when text-comparing two requests)."

If select yes, it will actually replace some unique identifiers that I've seen in my own Failed Request Trace files but not comprehensive and doesn't look for specific text patterns.  Request Ids that look like GUIDS are replaced with 00000-000-000-... you get the picture.  Computer names are replaced with something generic like SERVERNAME.  It makes it a bit easier to Windiff two request files.  But note that there is no built-in difference check.  You have to compare the files that are written to the %TEMP% folder.

The UI:

There is no toolbar.   Key pieces of information are filled into columns in a grid in the top half of the window.  There are too many columns, but no option to hide ones you don't want to see.  First thing I usually do is maximize the window, and then resize the URL column to see better.

The bottom half is embedded web browser.  When you single-click on a row, you should see the web representation of the request on the bottom.  There isn't much space here, so if you want to open it in the default application (hopefully the browser), double-click on the row, and it will launch in the default application.  You can also use keyboard controls like arrows to navigate the grid, whatever works with the built-in WPF Grid control.  Multiple-selection is disabled.  You may want to re-associate .xml extension with a browser.  If it doesn't look pretty, you are probably missing the freb.xsl file that is in the Failed Request Trace folder.  You may have accidentally deleted it.  IIS can put this file back, it may be recycling W3, or turning on/off the Failed Request Tracing, or something like that.

CURRENT ISSUES:

No toolbar, which would help avoid the three prompts at the start of the program.
No settings (and therefore no remembered settings).
No column selection to hide or show columns
No display filtering which would help see only particular app pools, particular URLs / results, etc.
No capture filtering which would help reduce the memory overhead
No setting to limit how many requests are stored in memory
No clean up of files saved to the %TEMP% folder
