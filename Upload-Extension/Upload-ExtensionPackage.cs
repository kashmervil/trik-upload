using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;
using Tasks = System.Threading.Tasks;

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
    [Guid(GuidList.GuidUploadExtensionPkgString)]
    public sealed class UploadExtensionPackage : Package
    {
        private Uploader Uploader { get; set; }
#if DEBUG
        private ObservableCollection<string> _ips = new ObservableCollection<string>{"10.0.40.127", "10.0.40.161"};
#else   
        private ObservableCollection<string> _ips = new ObservableCollection<string>{"192.168.1.1"};
#endif
        private UploadToolbar _uploadToolbar;
        private IDE _visualStudio;
        public UploadExtensionPackage()
        {
            Debug.WriteLine("Entering constructor for: {0}", ToString());
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        #region Package Members

        protected override void Initialize()
        {
            base.Initialize();

            var mcs = GetService(typeof (IMenuCommandService)) as OleMenuCommandService;
            if (null == mcs) return;

            //Toolbar initialization
            _uploadToolbar = new UploadToolbar { DropDownListMessage = "Enter TRIK Address", OptionsMessage = "*Manage TRIK profiles" };

            var reconnectId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int) PkgCmdIDList.ReconnectToTarget);
            _uploadToolbar.Reconnect = new MenuCommand(Reconnect, reconnectId) {Enabled = true};
            mcs.AddCommand(_uploadToolbar.Reconnect);

            var disconnectId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int) PkgCmdIDList.Disconnect);
            _uploadToolbar.Disconnect = new MenuCommand(UploadToTargetCallback, disconnectId) {Enabled = false};
            mcs.AddCommand(_uploadToolbar.Disconnect);

            var uploadId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int) PkgCmdIDList.UploadToTarget);
            _uploadToolbar.Upload = new MenuCommand(UploadToTargetCallback, uploadId) {Enabled = false};
            mcs.AddCommand(_uploadToolbar.Upload);

            var runProgramId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int) PkgCmdIDList.RunOnTarget);
            _uploadToolbar.RunProgram = new MenuCommand(RunProgramCallback, runProgramId) {Enabled = false};
            mcs.AddCommand(_uploadToolbar.RunProgram);

            var stopProgramId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int) PkgCmdIDList.StopEvaluating);
            _uploadToolbar.StopProgram = new MenuCommand(StopProgramCallback, stopProgramId) {Enabled = false};
            mcs.AddCommand(_uploadToolbar.StopProgram);

            var propertiesId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int)PkgCmdIDList.Properties);
            _uploadToolbar.Properties = new MenuCommand(PropertiesCallback, propertiesId) { Enabled = false };
            mcs.AddCommand(_uploadToolbar.Properties);

            var comboBoxCommandId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int)PkgCmdIDList.TargetIp);
            var comboBoxCommand = new OleMenuCommand(HandleInvokeCombo, comboBoxCommandId);
            mcs.AddCommand(comboBoxCommand);

            // This is the special command to get the list of drop down items
            var comboBoxGetListCommandId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int)PkgCmdIDList.GetIpList);
            var comboBoxGetListCommand = new OleMenuCommand(HandleInvokeComboGetList, comboBoxGetListCommandId);
            mcs.AddCommand(comboBoxGetListCommand);

            //Visual Studio wrapper class initialization
            var statusbar = GetService(typeof (SVsStatusbar)) as IVsStatusbar;
            var pane = GetService(typeof (SVsGeneralOutputWindowPane)) as IVsOutputWindowPane;
            if (statusbar == null || pane == null) return;
            _visualStudio = new IDE(statusbar, pane);

            _visualStudio.WindowPane.SetName("TRIK-Controller");
        }
        #endregion

        private void HandleInvokeComboGetList(object sender, EventArgs e)
        {
            var args = e as OleMenuCmdEventArgs;
            if (args == null) return;
            if (args.OutValue != IntPtr.Zero)
            {
                var allOptions = _ips.ToList();
                allOptions.Add(_uploadToolbar.OptionsMessage);
                Marshal.GetNativeVariantForObject(allOptions.ToArray(), args.OutValue);
            }
        }

        private void HandleInvokeCombo(object sender, EventArgs e)
        {
            var args = e as OleMenuCmdEventArgs;
            if (args == null) return;
            if (args.OutValue != IntPtr.Zero)
            {
                Marshal.GetNativeVariantForObject(_uploadToolbar.DropDownListMessage, args.OutValue);
            }
            else
            {
                var inValue = args.InValue as string;
                if (inValue == null) return;
                if (inValue != _uploadToolbar.OptionsMessage)
                {
                    _uploadToolbar.DropDownListMessage = inValue;
                    if (Uploader == null || Uploader.Ip != inValue)
                        ConnectToTargetCallback(inValue);
                }
                else
                {
                    var window = new Targets {ListBoxTargets = {ItemsSource = _ips}};
                    window.ShowDialog();
                }
            }
        }

        private void PropertiesCallback(object sender, EventArgs e)
        {
            var solution = ((DTE2)GetService(typeof(DTE))).Solution;
            var solutionProjects = _visualStudio.GetSolutionProjects(solution.Projects);

            if (Uploader.SolutionManager == null ||
                Uploader.SolutionManager.FullName != solution.FullName)
            {
                Uploader.SolutionManager = new SolutionManager(solution.FullName,
                solutionProjects);
                Uploader.SolutionManager.ActiveProject = Uploader.SolutionManager.Projects[0];
            }
            Uploader.SolutionManager.UpdateProjects(solutionProjects);
            var currentProject = Uploader.SolutionManager.ActiveProject.ProjectName;
            var propertiesWindow = new PropertiesWindow
            {
                DataContext = Uploader.SolutionManager,
                ComboBox =
                {
                    ItemsSource = Uploader.SolutionManager.Projects.Select(x => x.ProjectName),
                    SelectedItem = Uploader.SolutionManager.ActiveProject.ProjectName
                }
            };
            propertiesWindow.ShowDialog();
            if (Uploader.SolutionManager.ActiveProject.ProjectName == currentProject) return;

            _visualStudio.Statusbar.SetText("Switched to " + Uploader.SolutionManager.ActiveProject.ProjectName);
            if (Uploader.SolutionManager.ActiveProject.UploadedFiles.Count == 0)
            {
                _uploadToolbar.RunProgram.Enabled = false;
            }
        }

        private void StopProgramCallback(object sender, EventArgs e)
        {
            _visualStudio.WindowPane.SetText("========== Killing TRIK application ==========\n");
            Uploader.StopProgram();
            _uploadToolbar.StopProgram.Enabled = false;
        }



        private void RunProgramCallback(object sender, EventArgs e)
        {
            _visualStudio.WindowPane.SetText("========== Starting an Application on TRIK ==========\n");
            _uploadToolbar.StopProgram.Enabled = true;
            Tasks.Task.Run(() =>
            {
                _visualStudio.Statusbar.SetText("Running application on TRIK. See output pane for more information");
                try
                {
                    Uploader.RunProgram();
                }
                catch (Exception exception)
                {
                    _visualStudio.Statusbar.SetText(
                        "Network error occurred while running an application. Trying to reconnect");
                    _uploadToolbar.RunProgram.Enabled = false;
                    _uploadToolbar.Upload.Enabled = false;
                    _visualStudio.WindowPane.AppendText(exception.Message);

                    Reconnect();
                }
            });
        }

        private async void UploadToTargetCallback(object sender, EventArgs e)
        {
            _uploadToolbar.Upload.Enabled = false;
            _uploadToolbar.RunProgram.Enabled = false;
            var solution = ((DTE2) GetService(typeof (DTE))).Solution;
            var buildConfiguration = solution.SolutionBuild.ActiveConfiguration.Name;

            if ("Release" != buildConfiguration)
            {
                const string message = "Use Release build for better performance";
                _visualStudio.WindowPane.SetText(message);
                _visualStudio.Statusbar.SetText("Please change Solution Configuration option. " + message);
                _uploadToolbar.Upload.Enabled = true;
                return;
            }
            if (Uploader.SolutionManager == null ||
                Uploader.SolutionManager.FullName != solution.FullName)
            {
                Uploader.SolutionManager = new SolutionManager(solution.FullName,
                    _visualStudio.GetSolutionProjects(solution.Projects));
                if (Uploader.SolutionManager.Projects.Count == 1)
                    Uploader.SolutionManager.ActiveProject = Uploader.SolutionManager.Projects[0];
                else
                {
                    const string message = "Please select a project to upload in Properties";
                    _visualStudio.Statusbar.SetText(message);
                    _visualStudio.WindowPane.SetText("You opened a new solution. Your solution has more than one project " + message);
                    return;
                }
            }
            else
            {
                var projects = _visualStudio.GetSolutionProjects(solution);
                await Tasks.Task.Run(() => Uploader.SolutionManager.UpdateProjects(projects));
            }
            var activeProjectName = Uploader.SolutionManager.ActiveProject.ProjectName;
            _visualStudio.Statusbar.Progress(8000, "Uploading " + activeProjectName);
            var error = await Uploader.UploadActiveProjectAsync();
            await _visualStudio.Statusbar.StopProgressAsync();
            if (error.Length != 0)
            {
                _visualStudio.WindowPane.SetText(error + "\n\n Trying to reconnect...");
                Reconnect();
            }
            else
            {
                var message = activeProjectName + " Uploaded!";
                _visualStudio.Statusbar.SetText(message);
                _visualStudio.WindowPane.SetText(message);
                _uploadToolbar.RunProgram.Enabled = true;
                _uploadToolbar.Upload.Enabled = true;
                _uploadToolbar.Properties.Enabled = true;
            }

        }

        private async void ConnectToTargetCallback(string ip)
        {
            _uploadToolbar.RunProgram.Enabled = false;
            _visualStudio.WindowPane.SetName("TRIK(" + ip + ")");
            _visualStudio.WindowPane.SetText("Connecting to " + ip + "...");
            _uploadToolbar.Upload.Enabled = false;
            const int dueTime = 11000; //Usual time is taken for connection with a controller
            _visualStudio.Statusbar.Progress(dueTime, "Connecting to " + ip);
            var error = "";
            Uploader = new Uploader(ip) { OutputAction = _visualStudio.WindowPane.AppendText };
            await Tasks.Task.Run(async () =>
            {
                try
                {
                    Uploader.Connect();
                }
                catch (Exception exeption)
                {
                    error = exeption.Message;
                }
                await _visualStudio.Statusbar.StopProgressAsync();
                if (error == "")
                {
                    _visualStudio.Statusbar.SetText("Connected!");
                    _visualStudio.WindowPane.SetText("Connected to " + ip);

                    _uploadToolbar.Upload.Enabled = true;
                    _uploadToolbar.Properties.Enabled = true;
                }
                else
                {
                    _visualStudio.Statusbar.SetText("Connection attempt failed. See Output pane for details");
                    _visualStudio.WindowPane.SetText(error);
                }
            });
        }

        private void Reconnect(object sender, EventArgs eventArgs)
        {
            Reconnect();
        }

        private async void Reconnect()
        {
            var error = "";
            try
            {
                _visualStudio.Statusbar.Progress(8000, "Trying to reconnect");
                await Uploader.ReconnectAsync();
            }
            catch (Exception)
            {
                error = "Can't connect to TRIK. Check connection and try again";
                Uploader = null;
                //_uploadToolbar.Connect.Enabled = true;
                _uploadToolbar.RunProgram.Enabled = false;
                _uploadToolbar.Upload.Enabled = false;
            }
            await _visualStudio.Statusbar.StopProgressAsync();
            _visualStudio.Statusbar.SetText(error == "" ? "Connected!" : error);
            _uploadToolbar.Upload.Enabled = true;
            _uploadToolbar.Properties.Enabled = true;
        }
    }
}