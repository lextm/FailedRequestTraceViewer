using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
using System.Windows.Forms;
using System.IO;
using System.Xml;

namespace FailedRequestTraceViewer2
{
    public class FailedRequestTraceFile
    {
        public string sID { get; set; }
        public DateTime sTime { get; set; }
        public string sTrigger { get; set; }
        private string _sResult;
        public string sResult { 
            get { 
                if (String.IsNullOrEmpty(sTrigger) || ( _sResult == sTrigger)) return _sResult;
                return sTrigger + "->" + _sResult;
            } 
            set {
                _sResult = value;
            } 
        }
        public string sSite { get; set; }
        public string sUser { get; set; }
        public string sProtocol { get; set; }
        public string sMethod { get; set; }
        public string sHost { get; set; }
        public string sURL { get; set; }
        public string sBody { get; set; }
        public string sContentType { get; set; }
        public string sAppPool { get; set; }
        private string _sTimeTaken;
        public string sTimeTaken { get {
                if (Int32.Parse(_sTimeTaken) == 0)
                    return "0ms";
                TimeSpan ts = new TimeSpan(0,0,0,0,Int32.Parse(_sTimeTaken));
                return (
                    (ts.Hours > 0 ? ts.Hours.ToString() + "h " : "") +
                    (ts.Minutes > 0 ? ts.Minutes.ToString() + "m " : "") +
                    (ts.Seconds > 0 ? ts.Seconds.ToString() + "s " : "") +
                    (ts.Milliseconds > 0 ? ts.Milliseconds.ToString() + "ms" : "")
                    );
                //if (Int32.Parse(sTimeTaken) < 1000) return sTimeTaken + "ms";
                //if (int32.Parse(sTimeTaken) < 60000) return 
            } 
            set {
                _sTimeTaken = value; 
            } 
        }
        public string sProcessID { get; set; }
        // Note that the FailedTraceRequest file is XML formatted, therefore we put in a string
        public string sFilePath { get; set; }
        public string sFileContents { get; set; }
    }
    public class FRTUtil
    {
        //bool fFinishedImporting = true;// false change to true

        Object lockAddingFRTFiles;
        public List<string> frtCustomFolderPaths { get; set; } // note we will *not* subscribe to changes here
        public List<string> frtIISFolderPaths { get; set; } // note we *will* subscribe to changes here

        public bool frtStripUniqueIds = false;

        public Dictionary<string, FailedRequestTraceFile> initialRequestFiles;// = new Dictionary<string, FailedRequestTraceFile>();
        public SortedDictionary<DateTime, string> filesSortedByDate;

        public FRTUtil()
        {
            lockAddingFRTFiles = new object();
            frtCustomFolderPaths = new List<string>();
            frtIISFolderPaths = new List<string>();
            filesSortedByDate = new SortedDictionary<DateTime, string>();

            initialRequestFiles = new Dictionary<string, FailedRequestTraceFile>();
        }

        // returns list of full paths to failed request locations (doesn't include subfolders)
        // no user interaction

        // returns false if program should end.
        public bool GetFailedRequestLocations()
        {
            try
            {
                ServerManager thisIIS = new ServerManager();
                if (thisIIS.Sites.Count() == 0)
                {
                    MessageBox.Show("There are no IIS sites!", "ERROR");
                    return false;
                }

                if (MessageBox.Show(
        // message description
        "Do you want to add Failed Request Trace locations based on this computer's local IIS configuration? (note, UI will be updated as new traces appear automatically)",
        // caption
        "",
        // button options
        MessageBoxButtons.YesNo

   ) == DialogResult.Yes)
                {
                    foreach (Site site in thisIIS.Sites)
                    {
                        if ((site.TraceFailedRequestsLogging.Directory.Length > 0)
                            && Directory.Exists(Environment.ExpandEnvironmentVariables(site.TraceFailedRequestsLogging.Directory)))
                        {
                            //if (MessageBox.Show(
                            //        // message description
                            //        "Show failed trace requests for site " + site.Name + "?  (Logging is " + (site.TraceFailedRequestsLogging.Enabled ? "enabled" : "disabled") + ")",
                            //        // caption
                            //        "Add IIS Location",
                            //        // button options
                            //        MessageBoxButtons.YesNo

                            //   ) == DialogResult.Yes)
                            //{
                                frtIISFolderPaths.Add(Environment.ExpandEnvironmentVariables(site.TraceFailedRequestsLogging.Directory));
                            //}
                        }
                    }
                }


            }
            catch (Exception eeek)
            {

            }

            if (MessageBox.Show(
                    // message description
                    "Do you want to add a local folder where Failed Request Traces exist? (note 1: the UI will not update automatically, note 2: if the traces are from a different computer, the displayed Site information may be incorrect, note 3: times will appear in local time zone.)",
                    // caption
                    "Add Custom Location",
                    // button options
                    MessageBoxButtons.YesNo

               ) == DialogResult.Yes)
            {

                //Interaction.InputBox("Enter ?", "Title", "Default Text");
                System.Windows.Forms.FolderBrowserDialog folderToAdd = new System.Windows.Forms.FolderBrowserDialog();
                folderToAdd.ShowNewFolderButton = true;
                folderToAdd.Description = "Select folder to add a custom Failed Request Trace location";
                // Default to the My Documents folder.
                folderToAdd.RootFolder = Environment.SpecialFolder.MyComputer;

                System.Windows.Forms.DialogResult result = folderToAdd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    frtCustomFolderPaths.Add(folderToAdd.SelectedPath);
                }
            }

            if (frtIISFolderPaths.Count() + frtCustomFolderPaths.Count() == 0)
            {
                MessageBox.Show("There are no folders to inspect", "ERROR");

                return false;
            }

            if (MessageBox.Show(
        // message description
        "Do you want to strip unique identifiers when saving request files to disk? (may help when text-comparing two requests)",
        // caption
        "Strip Unique Identifiers",
        // button options
        MessageBoxButtons.YesNo

   ) == DialogResult.Yes)
            {
                frtStripUniqueIds = true;
            }

                return true;
        }

        public string RetryReadingInputFile(string sPath)
        {
            bool fDone = false;
            int iRetryCount = 0;
            string sRet = "";
            while (!fDone)
            {
                try
                {
                    sRet = File.ReadAllText(sPath);
                    fDone = true;
                } catch (System.IO.IOException eeek)
                {
                    // likely file is in use
                    System.Threading.Thread.Sleep(25);
                } catch (Exception eeek)
                {
                    fDone = true;
                }
                iRetryCount++;
            }
            return fDone?sRet:null;
        }

        public bool GenerateFRTObjectFromFile(string sPathToInputFile, out FailedRequestTraceFile outputFRTObject)
        {
            // if fails, set the out object to null.
            outputFRTObject = null;

            // although some information could be missing that causes an exception,
            // when it appears to be a failedrequesttracefile, put a log entry
            bool fFailedRequestFile = false;

            outputFRTObject = new FailedRequestTraceFile();
            try
            {
                outputFRTObject.sFilePath = sPathToInputFile;
                outputFRTObject.sFileContents = RetryReadingInputFile(sPathToInputFile);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(outputFRTObject.sFileContents);
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("a", "http://schemas.microsoft.com/win/2004/08/events/event");

                XmlNode nxml = doc.SelectSingleNode("/failedRequest");
                if (nxml == null)
                    return false;
                // matching node is found
                fFailedRequestFile = true; // in case a try/catch occurs, we know it reached this point
                XmlNode sTC = doc.SelectSingleNode("/failedRequest/a:Event/a:System/a:TimeCreated", nsmgr);
                outputFRTObject.sTime = DateTime.Parse( sTC.Attributes["SystemTime"].Value);
                Uri frturl = new Uri(nxml.Attributes["url"].Value);
                outputFRTObject.sHost = frturl.Host;
                outputFRTObject.sProtocol = frturl.Scheme;
                outputFRTObject.sURL = frturl.PathAndQuery;
                //frtFile.
                //frtFile.sURL = nxml.Attributes["url"].Value;
                outputFRTObject.sID = "";
                outputFRTObject.sMethod = nxml.Attributes["verb"].Value;
                //outputFRTObject.sResult = nxml.Attributes["statusCode"].Value;
                outputFRTObject.sAppPool = nxml.Attributes["appPoolId"].Value;
                outputFRTObject.sTimeTaken = nxml.Attributes["timeTaken"].Value;
                outputFRTObject.sProcessID = nxml.Attributes["processId"].Value;
                outputFRTObject.sSite = nxml.Attributes["siteId"].Value;
                if (nxml.Attributes["triggerStatus"] != null)
                    outputFRTObject.sTrigger = nxml.Attributes["triggerStatus"].Value;
                //frtFile.sTime = "";
                outputFRTObject.sUser = "";
                // Add the namespace.  
                XmlNodeList bytessent = nxml.SelectNodes("//a:Data[@Name='BytesSent']", nsmgr);
                if (bytessent.Count > 0) 
                    outputFRTObject.sBody = bytessent[bytessent.Count - 1].FirstChild.Value.ToString();
                else 
                    outputFRTObject.sBody = "";

                XmlNodeList httpstatus = nxml.SelectNodes("//a:Data[@Name='HttpStatus']", nsmgr);
                if (httpstatus.Count > 1)
                    outputFRTObject.sResult = httpstatus[httpstatus.Count - 2].FirstChild.Value.ToString();
                else if (httpstatus.Count > 0)
                {
                    outputFRTObject.sResult = httpstatus[httpstatus.Count - 1].FirstChild.Value.ToString();
                } else
                    outputFRTObject.sResult = "";

                XmlNodeList contenttypes = doc.SelectNodes("//a:Data[@Name='HeaderName']", nsmgr);
                for (var i = 0; i < contenttypes.Count; i++)
                {
                    if (contenttypes[contenttypes.Count - 1 - i].FirstChild.Value == "Content-Type")
                    {
                        outputFRTObject.sContentType = contenttypes[contenttypes.Count - 1 - i].NextSibling.FirstChild.Value;
                        break;
                    }
                }
                //                frtFile.

                // now handle authentication
                string sAuthType = nxml.Attributes["authenticationType"].Value;
                if ((sAuthType == "Negotiate") || (sAuthType == "Basic"))
                {
                    outputFRTObject.sUser = nxml.Attributes["userName"].Value;
                }
                else if (sAuthType == "anonymous")
                {

                }
                else if (sAuthType == "NOT_AVAILABLE")
                {

                }

                // STRIP CONTENT
                if (frtStripUniqueIds)
                {
                    //  <TimeCreated SystemTime="2020-05-26T16:56:18.549Z"/>
                    //< Correlation ActivityID = "{800001DD-0002-FF00-B63F-84710C7967BB}" />
                    //< Execution ProcessID = "48256" ThreadID = "30264" />
                    //< Computer > BC - 427269616E43 </ Computer >
                    // computer name can just do search and replace.

                    XmlNodeList sCNs = doc.SelectNodes("/failedRequest/a:Event/a:System/a:Computer", nsmgr);
                    foreach (XmlNode ynot in sCNs)
                    {
                        ynot.FirstChild.Value = "COMPUTER";
                        //ynot.ParentNode.RemoveChild(ynot);
                    }

                    XmlNodeList sTCs = doc.SelectNodes("/failedRequest/a:Event/a:System/a:TimeCreated", nsmgr);
                    foreach (XmlNode ynot in sTCs)
                    {
                        ynot.Attributes["SystemTime"].Value = "1970-01-01T00:00:00.000Z";
                        //ynot.ParentNode.RemoveChild(ynot);
                    }
                    XmlNodeList sCOs = doc.SelectNodes("/failedRequest/a:Event/a:System/a:Correlation", nsmgr);
                    foreach (XmlNode ynot in sCOs)
                    {
                        ynot.Attributes["ActivityID"].Value = "{00000000-0000-0000-0000-000000000000}";
                        //ynot.ParentNode.RemoveChild(ynot);
                    }
                    XmlNodeList sEXs = doc.SelectNodes("/failedRequest/a:Event/a:System/a:Execution", nsmgr);
                    foreach (XmlNode ynot in sEXs)
                    {
                        ynot.Attributes["ProcessID"].Value = "0";
                        ynot.Attributes["ThreadID"].Value = "0";
                        //ynot.ParentNode.RemoveChild(ynot);
                    }
                    XmlNodeList sCIDs = doc.SelectNodes("/failedRequest/a:Event/a:EventData/a:Data[@Name='ContextId']", nsmgr);
                    foreach (XmlNode ynot in sCIDs)
                    {
                        ynot.FirstChild.Value = "{00000000-0000-0000-0000-000000000000}";
                        //ynot.ParentNode.RemoveChild(ynot);
                    }
                    XmlNodeList sCID2s = doc.SelectNodes("/failedRequest/a:Event/a:EventData/a:Data[@Name='Context ID']", nsmgr);
                    foreach (XmlNode ynot in sCID2s)
                    {
                        ynot.FirstChild.Value = "{00000000-0000-0000-0000-000000000000}";
                        //ynot.ParentNode.RemoveChild(ynot);
                    }
                    // write it back out to contents.. pretty print it
                    // Format the XML text.
                    StringWriter string_writer = new StringWriter();
                    XmlTextWriter xml_text_writer = new XmlTextWriter(string_writer);
                    xml_text_writer.Formatting = Formatting.Indented;
                    doc.WriteTo(xml_text_writer);

                    // Display the result.
                    outputFRTObject.sFileContents = string_writer.ToString();

                    //outputFRTObject.sFileContents = doc.OuterXml;

                }
                return true;

            }
            catch (Exception eeek) // any exception just invalidates the file
            {
                if (fFailedRequestFile)
                {
                    outputFRTObject.sURL = "PARSE ERROR! " + outputFRTObject.sURL;
                    return true;
                } else
                {
                    outputFRTObject = null;
                    return false;
                }
            }

            // logic error (?) if it reaches here.
            outputFRTObject = null;
            return false;
        }

                public bool ImportFailedRequestTraceFile(string sPathToFile)
        {
            if (initialRequestFiles.ContainsKey(sPathToFile))
                return false;

            FailedRequestTraceFile frtTraceFile;
            if (GenerateFRTObjectFromFile(sPathToFile, out frtTraceFile))
            {
                lock (lockAddingFRTFiles)
                {
                    // double-check if file is already there, once in the lock
                    if (initialRequestFiles.ContainsKey(sPathToFile))
                        return false;
                    initialRequestFiles.Add(sPathToFile, frtTraceFile);
                    DateTime creationtime = frtTraceFile.sTime;//DateTime.Parse(frtTraceFile.sTime);

                    // Resolve conflict if two different requests have the same time stamp
                    // Note that this is only important during initial import... 
                    //  once historical files are imported, new files will be added sequentially.
                    while (filesSortedByDate.ContainsKey(creationtime))
                    {
                        creationtime += new TimeSpan(0, 0, 0,0,1);
                    }
                    filesSortedByDate.Add(creationtime, frtTraceFile.sFilePath);
                    return true;
                }
            }

            return false;
        }
        public bool ImportFailedRequestTraceFilesFromLocation(string sPath, bool fRecurse)
        {
            bool fRet = true;

            var xmlFiles = Directory.EnumerateFiles(sPath, "*.xml", fRecurse?SearchOption.AllDirectories:SearchOption.TopDirectoryOnly);
            foreach (string currentFile in xmlFiles)
            {
                ImportFailedRequestTraceFile(currentFile);
            }

            return fRet;
        }
        public bool ImportAllTraceFiles()
        {
            bool fRet = true;

            foreach (string sPath in frtIISFolderPaths)
            {
                fRet = fRet && ImportFailedRequestTraceFilesFromLocation(sPath, true);
            }
            foreach (string sPath in frtCustomFolderPaths)
            {
                fRet = fRet && ImportFailedRequestTraceFilesFromLocation(sPath, false);
            }

            return fRet;
        }
    }
}
