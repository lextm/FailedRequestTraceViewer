using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using Microsoft.Web.Administration;

namespace FailedRequestTraceViewer2
{
    class WatchFolders
    {
        private List<string> _foldersToWatch;
        private Dictionary<string, FileSystemWatcher> _watchedFolders = new Dictionary<string, FileSystemWatcher>();
        public FileSystemEventHandler WatchCallback { set; get; }

        public List<string> GetWatchedFolders()
        {
            List<string> folders = new List<string>();

            foreach (string s in _watchedFolders.Keys)
            {
                folders.Add(_watchedFolders[s].Path);
            }
            return folders;
        }
        // used for passing in function - may not be necessary
        //public delegate void WatchCallback(object source, FileSystemEventArgs e);
        private void WatchThisPath(string sPath, bool fWatch)
        {
            if (!fWatch)
            {
                if (_watchedFolders.ContainsKey(sPath))
                {
                    _watchedFolders.Remove(sPath);
                }
                // otherwise do nothing
                return;
            }
            if (fWatch)
            {
                if (_watchedFolders.ContainsKey(sPath))
                    return;
                FileSystemWatcher watch = new FileSystemWatcher();

                watch.Path = sPath;
                // Watch for changes in LastAccess and LastWrite times, and
                // the renaming of files or directories.
                watch.NotifyFilter = NotifyFilters.LastAccess
                                        | NotifyFilters.LastWrite
                                        | NotifyFilters.FileName
                                        | NotifyFilters.DirectoryName;

                // Only watch text files.
                watch.Filter = "*.xml";

                // Add event handlers.
                //watch.Changed += OnChanged;
                watch.Created += WatchCallback;
                //watch.Deleted += OnChanged;
                //watch.Renamed += OnRenamed;

                // Begin watching.
                watch.EnableRaisingEvents = true;

                _watchedFolders.Add(sPath, watch);

                // if we don't have a freb.xsl in the user's temp folder,
                    // and if one exists in this path,
                        // copy to user's temp folder
                if (!File.Exists(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"freb.xsl")))
                {
                    if (File.Exists(System.IO.Path.Combine(sPath, "freb.xsl")))
                    {
                        // copy freb.xsl over
                        File.Copy(System.IO.Path.Combine(sPath, "freb.xsl"), System.IO.Path.Combine(System.IO.Path.GetTempPath(), "freb.xsl"));
                    }
                }
            }
            return;
        }

        public void RefreshIISFolders()
        {
            if (WatchCallback == null)
                return;

            List<string> newfolderlist = GetIISFailedRequestLocations();

            // unwatch existing folders
            if (_foldersToWatch != null)
                foreach (string spath in _foldersToWatch)
                {
                    WatchThisPath(spath, false);
                    // also unwatch ONLY immediate subfolders where the files actually get written
                    var frqsubdirs = Directory.EnumerateDirectories(spath, "*", SearchOption.TopDirectoryOnly);
                    foreach (string currentDir in frqsubdirs)
                    {
                        WatchThisPath(currentDir, false);
                    }
                }

            _foldersToWatch = newfolderlist;

            // watch new folders
            foreach (string spath in _foldersToWatch)
            {
                WatchThisPath(spath, true);
                // also watch ONLY immediate subfolders where the files actually get written
                var frqsubdirs = Directory.EnumerateDirectories(spath, "*", SearchOption.TopDirectoryOnly);
                foreach (string currentDir in frqsubdirs)
                {
                    WatchThisPath(currentDir, true);
                }
            }


        }
        private List<string> GetIISFailedRequestLocations()
        {
            List<string> listToReturn = new List<string>();
            try
            {
                //ServerManager thisIIS = new ServerManager();
                //if (thisIIS.Sites.Count() != 0)
                //{
                //    foreach (Site site in thisIIS.Sites)
                //    {
                //        if ((site.TraceFailedRequestsLogging.Directory.Length > 0)
                //            && Directory.Exists(Environment.ExpandEnvironmentVariables(site.TraceFailedRequestsLogging.Directory)))
                //        {
                //            listToReturn.Add(Environment.ExpandEnvironmentVariables(site.TraceFailedRequestsLogging.Directory));
                //        }
                //    }
                //}
            }
            catch (System.UnauthorizedAccessException eeek)
            {
                MessageBox.Show("Insufficient permission to determine IIS Failed Request locations.  Please run as administrator.", "Warning");
            }
            catch (Exception eeek)
            {
                // ignore exceptions
                MessageBox.Show("Error retrieving IIS Failed Request locations.\n\nException: " + eeek.Message, "ERROR");

            }
            return listToReturn;

        }

    }
}
