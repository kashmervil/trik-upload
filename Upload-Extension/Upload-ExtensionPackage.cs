using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Text;
using System.Threading;
using System.Windows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;
using Thread = System.Threading.Thread;
using Timer = System.Threading.Timer;

namespace Trik.Upload_Extension
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    //[ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidUpload_ExtensionPkgString)]
    public sealed class UploadExtensionPackage : Package
    {
        private Uploader Uploader { get; set; }
        private Window1 _connectionWindow;
#if DEBUG 
        private string _ip = "10.0.40.125";
#else   
        private string _ip = "192.168.1.1";
#endif
        private bool _isFirstUpload = true;
        private bool _isTrikAplicationRunning;
        private bool _isFirstRun = true;

        //Visual Studio communication constants 
        private bool _isProgressRunning;
        private uint _statusbarCookie;
        private IVsStatusbar _statusbar;
        private IVsOutputWindowPane _pane;

        public UploadExtensionPackage()
        {
            Debug.WriteLine("Entering constructor for: {0}", ToString());
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        protected override void Initialize()
        {
            //var dte = (DTE2)GetService(typeof(DTE));
            
            Debug.WriteLine ("Entering Initialize() of: {0}", ToString());
            base.Initialize();
            
            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null == mcs) return;
            // Create the command for the menu item.
            var connectId = new CommandID(GuidList.guidUpload_ExtensionCmdSet, (int)PkgCmdIDList.ConnectToTarget);
            var connectItem = new MenuCommand(MenuItemCallback, connectId );
            mcs.AddCommand(connectItem);
                
            var uploadId = new CommandID(GuidList.guidUpload_ExtensionCmdSet, (int)PkgCmdIDList.UploadToTarget);
            var uploadItem = new MenuCommand(UploadToTrik_Click, uploadId);
            uploadItem.Enabled = false;
            mcs.AddCommand(uploadItem);
        }
        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            _connectionWindow = new Window1 {IpAddress = {Text = _ip}};
            if (null == Uploader)
            {
                _connectionWindow.UploadToTrik.IsEnabled = false;
                _connectionWindow.RunProgram.IsEnabled = false;
            }

            _connectionWindow.ConnectToTrik.Click += ConnectToTrik_Click;
            _connectionWindow.UploadToTrik.Click += UploadToTrik_Click;
            _connectionWindow.RunProgram.Click += RunProgram_Click;
            WindowPane.SetName("TRIK-Controller");
            _connectionWindow.ShowModal();

        }

        private void RunProgram_Click(object sender, RoutedEventArgs e)
        {
            if (_isTrikAplicationRunning) return;
            _isTrikAplicationRunning = true;
            //ReportProgress(10000, "Starting an application on a controller");
            var scnt = SynchronizationContext.Current;
            _connectionWindow.Close();
            
            //WindowPane.Hide();
            WindowPane.Clear();
            WindowPane.Activate();
            WindowPane.FlushToTaskList();
            WindowPane.OutputString("========== Starting an Application on TRIK ==========\n");

            System.Threading.Tasks.Task.Run(() =>
            {
                StopProgress();
                StatusBar.SetText("Running application on TRIK. See output pane for more information");
                try
                {
                    var programOutput = Uploader.RunProgram();
                    
                    if (!_isFirstRun) return;
                    
                    programOutput.DataReceived += programOutput_DataReceived;
                    _isFirstRun = false;

                    //WindowPane.OutputStringThreadSafe(programOutput + "\n");
                    }
                catch (Exception exception)
                {
                    scnt.Post(x =>
                    {
                        _connectionWindow.MessageLabel.Content =
                            "Network error occurred while running an application. Trying to reconnect";
                        _connectionWindow.RunProgram.IsEnabled = false;
                        _connectionWindow.UploadToTrik.IsEnabled = false;
                    }, null);
                    WindowPane.OutputString(exception.Message);

                    Reconnect(scnt);
                }
                finally
                {
                    _isTrikAplicationRunning = false;
                }
            });
        }

        void programOutput_DataReceived(object sender, Renci.SshNet.Common.ShellDataEventArgs e)
        {
            WindowPane.OutputStringThreadSafe(Encoding.UTF8.GetString(e.Data));
        }

        void UploadToTrik_Click(object sender, EventArgs e)
        {
            if (Uploader == null) return;

            _connectionWindow.MessageLabel.Content = "Uploading...";
            StatusBar.SetText("Uploading...");
            _connectionWindow.UploadToTrik.IsEnabled = false;
            _connectionWindow.RunProgram.IsEnabled = false;
            var dte = (DTE2)GetService(typeof(DTE));
            var buildConfiguration = dte.Solution.SolutionBuild.ActiveConfiguration.Name;

            if ("Release" != buildConfiguration)
            {
                const string message = "Use Release build for better performance";
                _connectionWindow.MessageLabel.Content = message;
                StatusBar.SetText(message);
                _connectionWindow.UploadToTrik.IsEnabled = true;
                return;
            }

            var scnt = SynchronizationContext.Current;
            System.Threading.Tasks.Task.Run(() =>
            {
                var projects = GetSolutionProjects(dte.Solution);               
                Uploader.SolutionManager.UpdateProjects(projects);

                try
                { 
                    ReportProgress(8000, "Uploading");
                    Uploader.UploadActiveProject();
                    scnt.Post(x =>
                    {
                        _connectionWindow.MessageLabel.Content = "Uploaded!";
                        //connectionWindow.UploadToTrik.IsEnabled = true;
                        _connectionWindow.Close();
                    }, null);
                    StopProgress();
                    StatusBar.SetText("Uploaded!");
                }
                catch (Exception)
                {
                    StopProgress();
                    scnt.Post(x =>
                    {
                        _connectionWindow.MessageLabel.Content = "Error is occurred. Trying to reconnect...";
                    }, null);
                    //StatusBar.SetText("Error is occurred. Trying to reconnect...");
                    Reconnect(scnt);

                }                
            });
        }

        void ConnectToTrik_Click(object sender, RoutedEventArgs e)
        {
            if (_ip == _connectionWindow.IpAddress.Text && !_isFirstUpload)
            {
                _connectionWindow.MessageLabel.Content = "Already connected to this host!";
                StatusBar.SetText("Already connected to this host!");
                return;
            }
            _connectionWindow.ConnectToTrik.IsEnabled = false;
            _connectionWindow.RunProgram.IsEnabled = false;

            _connectionWindow.MessageLabel.Content = "Connecting...";
            _ip = _connectionWindow.IpAddress.Text;
            var scnt = SynchronizationContext.Current;
            _connectionWindow.UploadToTrik.IsEnabled = false;
            System.Threading.Tasks.Task.Run(() =>
            {
                const int dueTime = 11000; //Usual time is taken for connection with a controller
                const string message = "A connection is taking longer than usual";
                var timeoutTimer = new Timer(x =>
                {
                    StatusBar.SetText(message);
                    scnt.Post(y =>
                    {
                        _connectionWindow.MessageLabel.Content = message;
                    }, null);
                }, null, dueTime, -1);

                ReportProgress(dueTime, "Connecting");

                try
                {
                    Uploader = new Uploader(_ip);
                    scnt.Post(x =>
                    {
                        _connectionWindow.MessageLabel.Content = "Connected!";
                        _connectionWindow.UploadToTrik.IsEnabled = true;
                        _isFirstUpload = false;

                    }
                    , null);
                    var dte = (DTE2)GetService(typeof(DTE));
                    var solution = dte.Solution;
                    Uploader.SolutionManager = new SolutionManager(solution.FullName, GetSolutionProjects(solution.Projects));
                    Uploader.SolutionManager.ActiveProject = Uploader.SolutionManager.Projects.First();
                    StopProgress();
                    StatusBar.SetText("Connected!");
                }
                catch (Exception exeption)
                {
                    StopProgress();
                    StatusBar.SetText("Connection attempt failed. See Output pane for details");
                    WindowPane.Clear();
                    WindowPane.Activate();
                    WindowPane.OutputString(exeption.Message);
                    scnt.Post(x => _connectionWindow.MessageLabel.Content = "Connection attempt failed", null);
                }
                finally
                {
                    scnt.Post(x => _connectionWindow.ConnectToTrik.IsEnabled = true , null);
                    timeoutTimer.Dispose();
                }
            });
        }

        private void ReportProgress(int period, String message)
        {
            
            StopProgress();
            _isProgressRunning = true;
            
            System.Threading.Tasks.Task.Run(() =>
            {
                StatusBar.SetText("");
                var messageTail = "";
                const int iterations = 10;
                while (_isProgressRunning)
                {
                    for (var i = (uint) 0; i < iterations; i++)
                    {
                        StatusBar.Progress(ref _statusbarCookie, _isProgressRunning?1:0, message + messageTail, i, iterations);
                        messageTail = "." + ((messageTail.Length < 3) ? messageTail : "");
                        Thread.Sleep(period/iterations);
                    }
                }
            });
        }
        private void Reconnect(SynchronizationContext scnt)
        {
            try
            {
                ReportProgress(8000, "Network error is occurred. Trying to reconnect");
                Uploader.Reconnect();
                StopProgress();
                StatusBar.SetText("Connected!");
                scnt.Post(x =>
                {
                    _connectionWindow.MessageLabel.Content = "Connected!";
                    _connectionWindow.UploadToTrik.IsEnabled = true;
                    _isFirstUpload = false;
                }
                    , null);
            }
            catch (Exception)
            {
                StopProgress();
                const string message = "Can't connect to TRIK. Check connection and try again";
                StatusBar.SetText(message);
                scnt.Post(x =>
                {
                    _connectionWindow.MessageLabel.Content = message;
                    _connectionWindow.UploadToTrik.IsEnabled = true;
                    _isFirstUpload = false;
                }
                    , null);
                Uploader = null;
                _isFirstUpload = true;
            }
        }

        private static List<string> GetSolutionProjects(IEnumerable solution)
        {
            var list = new List<string>();
            foreach (var project in solution.OfType<Project>())
            {
                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    list.AddRange(GetSolutionFolderProjects(project));
                else 
                    list.Add(project.FullName);
            }
            return list.Where(x => !String.IsNullOrWhiteSpace(x)).ToList();
            //{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC} -- C#
            //{f2a71f9b-5d33-465a-a702-920d77279786} -- F#
        }

        private static List<string> GetSolutionFolderProjects(Project projectFolder)
        {
            var list = new List<string>();
            foreach (var project in projectFolder.ProjectItems.OfType<ProjectItem>()
                                    .Select(item => item.SubProject)
                                    .Where(project => project != null))
            {
                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    list.AddRange(GetSolutionFolderProjects(project));
                else
                {
                    list.Add(project.FullName);
                }
            }
            return list;
        }

        private void StopProgress()
        {
            _isProgressRunning = false;
            StatusBar.Progress(ref _statusbarCookie, 0, "", 0, 0);
        }

        private IVsStatusbar StatusBar
        {
            get { return _statusbar ?? (_statusbar = GetService(typeof (SVsStatusbar)) as IVsStatusbar); }
        }
        private IVsOutputWindowPane WindowPane
        {
            get { return _pane ?? (_pane = GetService(typeof (SVsGeneralOutputWindowPane)) as IVsOutputWindowPane); }
        }
    }
}
