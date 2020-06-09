using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;

namespace FailedRequestTraceViewer2
{
    class WatchFolders
    {
        private List<string> _foldersToWatch;
        private Dictionary<string, FileSystemWatcher> _watchedFolders = new Dictionary<string, FileSystemWatcher>();
        public FileSystemEventHandler WatchCallback { set; get; }

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
                ServerManager thisIIS = new ServerManager();
                if (thisIIS.Sites.Count() != 0)
                {
                    foreach (Site site in thisIIS.Sites)
                    {
                        if ((site.TraceFailedRequestsLogging.Directory.Length > 0)
                            && Directory.Exists(Environment.ExpandEnvironmentVariables(site.TraceFailedRequestsLogging.Directory)))
                        {
                            listToReturn.Add(Environment.ExpandEnvironmentVariables(site.TraceFailedRequestsLogging.Directory));
                        }
                    }
                }
            }
            catch (Exception eeek)
            {
                // ignore exceptions
            }
            return listToReturn;

        }

    }
}
