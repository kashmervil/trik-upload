using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Trik.Upload_Extension
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.GuidUploadExtensionPkgString)]
    public sealed class UploadExtensionPackage : Package
    {
        private Uploader Uploader { get; set; }
#if DEBUG
        private ObservableCollection<string> _ips = new ObservableCollection<string>{"10.0.40.126", "10.0.40.161"};
#else
        private readonly ObservableCollection<string> _ips = new ObservableCollection<string> {"192.168.1.1"};
#endif
        private UploadToolbar _uploadToolbar;
        private IDE VS { get; set; }
        private SolutionManager SolutionManager { get; set; }
        private bool IsConnecting { get; set; }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        #region Package Members

        protected override void Initialize()
        {
            base.Initialize();

            var mcs = GetService(typeof (IMenuCommandService)) as OleMenuCommandService;
            if (null == mcs) return;

            //Toolbar initialization
            _uploadToolbar = new UploadToolbar
            {
                DropDownListMessage = "Enter TRIK Address",
                OptionsMessage = "*Manage TRIK profiles"
            };


            var uploadId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int) PkgCmdIDList.UploadToTarget);
            _uploadToolbar.Upload = new MenuCommand(UploadToTargetCallback, uploadId) {Enabled = false};
            mcs.AddCommand(_uploadToolbar.Upload);

            var runProgramId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int) PkgCmdIDList.RunOnTarget);
            _uploadToolbar.RunProgram = new MenuCommand(RunProgramCallback, runProgramId) {Enabled = false};
            mcs.AddCommand(_uploadToolbar.RunProgram);

            var stopProgramId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int) PkgCmdIDList.StopEvaluating);
            _uploadToolbar.StopProgram = new MenuCommand(StopProgramCallback, stopProgramId) {Enabled = false};
            mcs.AddCommand(_uploadToolbar.StopProgram);

            var propertiesId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int) PkgCmdIDList.Properties);
            _uploadToolbar.Properties = new MenuCommand(PropertiesCallback, propertiesId) {Enabled = false};
            mcs.AddCommand(_uploadToolbar.Properties);

            var comboBoxCommandId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int) PkgCmdIDList.TargetIp);
            var comboBoxCommand = new OleMenuCommand(HandleInvokeCombo, comboBoxCommandId);
            mcs.AddCommand(comboBoxCommand);

            // This is the special command to get the list of drop down items
            var comboBoxGetListCommandId = new CommandID(GuidList.GuidUploadExtensionCmdSet,
                (int) PkgCmdIDList.GetIpList);
            var comboBoxGetListCommand = new OleMenuCommand(HandleInvokeComboGetList, comboBoxGetListCommandId);
            mcs.AddCommand(comboBoxGetListCommand);

            //Visual Studio wrapper class initialization
            var statusbar = GetService(typeof (SVsStatusbar)) as IVsStatusbar;
            var pane = GetService(typeof (SVsGeneralOutputWindowPane)) as IVsOutputWindowPane;
            if (statusbar == null || pane == null) return;
            VS = new IDE(statusbar, pane);

            VS.WindowPane.SetName("TRIK-Controller");
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
                return;
            }

            var inValue = args.InValue as string;
            if (inValue == null) return;
            if (inValue == _uploadToolbar.OptionsMessage)
            {
                var window = new Targets {ListBoxTargets = {ItemsSource = _ips}};
                window.ShowDialog();
                return;
            }

            if (IsConnecting || (Uploader != null && Uploader.Ip == inValue)) return;
            _uploadToolbar.DropDownListMessage = inValue;
            ConnectToTargetCallback(inValue);
        }

        private void PropertiesCallback(object sender, EventArgs e)
        {
            var solution = ((DTE2) GetService(typeof (DTE))).Solution;
            var solutionProjects = VS.GetSolutionProjects(solution.Projects);

            if (SolutionManager == null ||
                SolutionManager.FullName != solution.FullName)
            {
                SolutionManager = new SolutionManager(solution.FullName,
                    solutionProjects, Uploader);
                SolutionManager.ActiveProject = SolutionManager.Projects[0];
            }
            SolutionManager.UpdateProjects(solutionProjects);
            var currentProject = SolutionManager.ActiveProject ?? SolutionManager.Projects[0];
            var propertiesWindow = new PropertiesWindow
            {
                DataContext = SolutionManager,
                ComboBox =
                {
                    ItemsSource = SolutionManager.Projects.Select(x => x.ProjectName),
                    SelectedItem = currentProject.ProjectName
                }
            };
            propertiesWindow.ShowDialog();
            if (SolutionManager.ActiveProject == currentProject) return;
            if (SolutionManager.ActiveProject == null)
                throw new Exception("Properties window did not work properly");
            var message = "Active project changed to " + SolutionManager.ActiveProject.ProjectName;
            VS.Statusbar.SetText(message);
            VS.WindowPane.WriteLine(message);
            if (SolutionManager.ActiveProject.UploadedFiles.Count == 0)
            {
                _uploadToolbar.RunProgram.Enabled = false;
            }
        }

        private void StopProgramCallback(object sender, EventArgs e)
        {
            VS.WindowPane.WriteLine("========== Killing TRIK application ==========\n");
            SolutionManager.StopProgram();
            _uploadToolbar.StopProgram.Enabled = false;
        }


        private void RunProgramCallback(object sender, EventArgs e)
        {
            VS.WindowPane.WriteLine("========== Starting an Application on TRIK ==========\n");
            _uploadToolbar.StopProgram.Enabled = true;
            VS.Statusbar.SetText("Running application on TRIK. See output pane for more information");
            SolutionManager.RunProgram();
        }

        private async void UploadToTargetCallback(object sender, EventArgs e)
        {
            _uploadToolbar.Upload.Enabled = false;
            _uploadToolbar.RunProgram.Enabled = false;
            _uploadToolbar.Properties.Enabled = false;
            var solution = ((DTE2) GetService(typeof (DTE))).Solution;
            var buildConfiguration = solution.SolutionBuild.ActiveConfiguration.Name;

            if ("Release" != buildConfiguration)
            {
                const string message = "Use Release build for better performance";
                VS.WindowPane.WriteLine(message);
                VS.Statusbar.SetText("Please change Solution Configuration option. " + message);
                _uploadToolbar.Upload.Enabled = true;
                return;
            }
            if (SolutionManager == null ||
                SolutionManager.FullName != solution.FullName)
            {
                SolutionManager = new SolutionManager(solution.FullName,
                    VS.GetSolutionProjects(solution.Projects), Uploader);
                SolutionManager.ActiveProject = SolutionManager.Projects[0];
            }
            
            switch (solution.SolutionBuild.BuildState)
            {
                case vsBuildState.vsBuildStateNotStarted:
                    await Task.Run(() => solution.SolutionBuild.Build(true));
                    break;
                case vsBuildState.vsBuildStateInProgress:
                    VS.WindowPane.WriteLine("Wait Until Build is finished");
                    return;
            }
            var activeProject = SolutionManager.ActiveProject;
            var uploadedFiles = activeProject.UploadedFiles.Count;
            var text = "Uploading " + activeProject.ProjectName;
            VS.Statusbar.Progress(8000, text);
            VS.WindowPane.Activate();
            VS.WindowPane.WriteLine(text);

            var error = await SolutionManager.UploadActiveProjectAsync();
            await VS.Statusbar.StopProgressAsync();
            if (error.Length != 0)
            {

                VS.WindowPane.WriteLine("\nNetwork error is occured: " + error);
                Reconnect();
                if (Uploader != null)
                {
                    VS.WindowPane.Write("Resume ");                    
                    UploadToTargetCallback(sender, e);
                }
            }
            else
            {
                if (activeProject.UploadedFiles.Count == 0 && uploadedFiles == 0)
                {
                    var expandedMessage = activeProject.ProjectName + "'s Release folder is empty, the same as corresponding remote folder.\n";
                    const string shortMessage = "Please Build Solution before uploading!";
                    VS.Statusbar.SetText(shortMessage);
                    VS.WindowPane.WriteLine(expandedMessage + shortMessage);
                    _uploadToolbar.Upload.Enabled = true;
                    _uploadToolbar.Properties.Enabled = true;
                    return;
                }
                var message = activeProject.ProjectName + " Uploaded!";
                VS.Statusbar.SetText(message);
                VS.WindowPane.WriteLine(message);
                _uploadToolbar.RunProgram.Enabled = true;
                _uploadToolbar.Upload.Enabled = true;
                _uploadToolbar.Properties.Enabled = true;
            }
        }

        private async void ConnectToTargetCallback(string ip)
        {
            _uploadToolbar.Upload.Enabled = false;
            _uploadToolbar.RunProgram.Enabled = false;
            IsConnecting = true;
            VS.WindowPane.SetName("TRIK Controller " + ip);
            VS.WindowPane.WriteLine("Connecting to " + ip);
            const int dueTime = 11000; //Usual time is taken for connection with a controller
            VS.Statusbar.Progress(dueTime, "Connecting to " + ip);
            Uploader = new Uploader(ip) {OutputAction = VS.WindowPane.WriteLine};
            await Task.Run(async () =>
            {
                try
                {
                    Uploader.Connect();
                    await VS.Statusbar.StopProgressAsync();
                    VS.Statusbar.SetText("Connected!");
                    VS.WindowPane.WriteLine("Connected to " + ip);
                    _uploadToolbar.Upload.Enabled = true;
                    _uploadToolbar.Properties.Enabled = true;
                }
                catch (Exception exeption)
                {
                    Task.WaitAny(VS.Statusbar.StopProgressAsync());
                    VS.Statusbar.SetText("Can't connect to TRIK. Check connection and try again. See Output pane for details");
                    VS.WindowPane.WriteLine(exeption.Message);
                    Uploader = null;
                }
                IsConnecting = false;
            });
        }

        private async void Reconnect()
        {
            if (IsConnecting) return;
            IsConnecting = true;
            const string tryingToReconnect = "Trying to reconnect";
            _uploadToolbar.RunProgram.Enabled = false;
            _uploadToolbar.Upload.Enabled = false;
            _uploadToolbar.StopProgram.Enabled = false;
            VS.WindowPane.WriteLine(tryingToReconnect);
            VS.Statusbar.Progress(8000, tryingToReconnect);
            var connected = await Uploader.ReconnectAsync();
            await VS.Statusbar.StopProgressAsync();
            var message = "Connected Successfully!";
            var flag = true;
            if (!connected)
            {
                message = "Can't connect to TRIK. Check connection and try again";
                flag = false;
                Uploader = null;
            }
            _uploadToolbar.Upload.Enabled = flag;
            VS.WindowPane.WriteLine(message);
            VS.Statusbar.SetText(message);
            IsConnecting = false;
        }
    }
}