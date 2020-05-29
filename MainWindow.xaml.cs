using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
//using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FailedRequestTraceViewer2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SynchronizationContext _syncContext;

        bool fFinishedImporting = false;
        public int iD = 1;

        Object lockAddToObservableCollection;
        FRTUtil frtfolders = new FRTUtil();
        public ObservableCollection<FailedRequestTraceFile> FailedRequestTracesInGrid { get; set; }
        Dictionary<string, FileSystemWatcher> watchedFolders = new Dictionary<string, FileSystemWatcher>();
        //= new ObservableCollection<FailedRequestTraceFile>();

        // Define the event handlers.
        private void WatchOnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            lock (lockAddToObservableCollection)
            {
                if (!fFinishedImporting)
                {
                    frtfolders.ImportFailedRequestTraceFile(e.FullPath);
                }
                else
                {
                    if (AddFailedRequestTraceFileToGrid(e.FullPath))
                    {
                    }
                    // add to observable collection :)
                }
            }
            //Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
        }

        // returns true if an item was added
        // caller is responsible for locking the array.
        public void update_size(object sender, RoutedEventArgs e)
        {
            var a = this.ActualHeight;
            var b = this.ActualWidth;
            var c = this.Height;
            var d = this.Width;
            //stacker_panell.Height = a;
            wpfGrid.Height = a / 2;
            wbSample.Height = a / 2;
        }
        public bool AddFailedRequestTraceFileToGrid(string sPathToFile)
        {
            FailedRequestTraceFile frtTraceFile;
            if (frtfolders.GenerateFRTObjectFromFile(sPathToFile, out frtTraceFile))
            {

                // this needs to be done on the UI thread!
                frtTraceFile.sID = iD.ToString();
                iD++;

                _syncContext.Post(o => FailedRequestTracesInGrid.Add(frtTraceFile), null);

                //Application.Current.Dispatcher.Invoke(new Action(() =>
                //{
                //    FailedRequestTracesInGrid.Add(frtTraceFile);
                //    // item was added... need to refresh
                //    //this.frtdata.Items.Refresh();

                //}));
                return true;
            }
            return false;

        }

        public void WatchThisPath(string sPath, bool fWatch)
        {
            if (!fWatch)
            {
                if (watchedFolders.ContainsKey(sPath))
                {
                    watchedFolders.Remove(sPath);
                }
                // otherwise do nothing
                return;
            }
            if (fWatch)
            {
                if (watchedFolders.ContainsKey(sPath))
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
                watch.Created += WatchOnChanged;
                //watch.Deleted += OnChanged;
                //watch.Renamed += OnRenamed;

                // Begin watching.
                watch.EnableRaisingEvents = true;

                watchedFolders.Add(sPath, watch);

            }
            return;
        }

        private void Row_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FailedRequestTraceFile item = (FailedRequestTraceFile) e.AddedItems[0];
            
            string sTempFile = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".xml";
            File.WriteAllText(sTempFile, item.sFileContents);
            try
            {
                File.Copy(System.IO.Path.GetDirectoryName(item.sFilePath) + "\\freb.xsl", System.IO.Path.GetTempPath() + "\\freb.xsl");
            }
            catch (Exception eek)
            {
                // ignore all exceptions -- whether source file doesn't exist, or target file already exists.
            }
            wbSample.Navigate(sTempFile);
        }


        private void Row_Select(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            FailedRequestTraceFile item = (FailedRequestTraceFile)row.Item;
            string sTempFile = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".xml";
            File.WriteAllText(sTempFile, item.sFileContents);
            try
            {
                File.Copy(System.IO.Path.GetDirectoryName(item.sFilePath) + "\\freb.xsl", System.IO.Path.GetTempPath() + "\\freb.xsl");
            }
            catch (Exception eek)
            {
                // ignore all exceptions -- whether source file doesn't exist, or target file already exists.
            }
            wbSample.Navigate(sTempFile);
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            FailedRequestTraceFile item = (FailedRequestTraceFile)row.Item;

            string sTempFile = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".xml";
            File.WriteAllText(sTempFile, item.sFileContents);
            try
            {
                File.Copy(System.IO.Path.GetDirectoryName(item.sFilePath) + "\\freb.xsl", System.IO.Path.GetTempPath() + "\\freb.xsl");
            }
            catch (Exception eek)
            {
                // ignore all exceptions -- whether source file doesn't exist, or target file already exists.
            }
            System.Diagnostics.Process.Start(sTempFile);
            // Some operations with this row
        }
        public MainWindow()
        {
            _syncContext = SynchronizationContext.Current;

            lockAddToObservableCollection = new Object();
            InitializeComponent();
            bool fBreakPoint = true;

            
            // [begin] turn off script errors on web browser
            //dynamic activeX = this.wbSample.GetType().InvokeMember("ActiveXInstance",
            //        System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            //        null, this.wbSample, new object[] { });

            //activeX.Silent = true;
            // [end] turn off script errors on web browser

            //HideScriptErrors(wbSample, true);
            //wbSample.Navigate("http://www.wpf-tutorial.com");

            FailedRequestTracesInGrid = new ObservableCollection<FailedRequestTraceFile>();
            wpfGrid.DataContext = this;
            //wpfGrid.SetBinding()
            if (!frtfolders.GetFailedRequestLocations())
            {
                Close();
                return;
            }

            // start watching for changes
            // Create a new FileSystemWatcher and set its properties.

            foreach (string spath in frtfolders.frtIISFolderPaths)
            {
                WatchThisPath(spath, true);
                // also watch immediate subfolders where the files actually get written
                var frqsubdirs = Directory.EnumerateDirectories(spath, "*", SearchOption.TopDirectoryOnly);
                foreach (string currentDir in frqsubdirs)
                {
                    WatchThisPath(currentDir, true);
                }
            }

            frtfolders.ImportAllTraceFiles();

            lock (lockAddToObservableCollection)
            {
                fFinishedImporting = true;
                foreach (DateTime abc in frtfolders.filesSortedByDate.Keys)
                {
                    FailedRequestTraceFile item = frtfolders.initialRequestFiles[frtfolders.filesSortedByDate[abc]];
                    item.sID = iD.ToString();
                    FailedRequestTracesInGrid.Add(item);
                    iD++;
                }
            }

            //frtdata.ItemsSource = null;
            //frtdata.ItemsSource = FailedRequestTracesInGrid;

            fBreakPoint = false;
            //frtfolders.ImportFailedRequestTraceFile("c:\\Users\\brianc\\Desktop\\15.xml");
        }
        public void HideScriptErrors(WebBrowser wb, bool Hide)
        {
            System.Reflection.FieldInfo fiComWebBrowser = typeof(WebBrowser)
                .GetField("_axIWebBrowser2",
                          System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;
            object objComWebBrowser = fiComWebBrowser.GetValue(wb);
            if (objComWebBrowser == null) return;
            objComWebBrowser.GetType().InvokeMember(
                "Silent", System.Reflection.BindingFlags.SetProperty, null, objComWebBrowser,
                new object[] { Hide });
        }
    }

}