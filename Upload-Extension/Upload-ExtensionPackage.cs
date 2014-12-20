using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Windows.Forms;
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
        private IList<string> _ips = new List<string>{"10.0.40.118", "*Enter new TRIK IP"};
#else   
        private IList<string> _ips = new List<string>{"192.168.1.1", "*Enter new TRIK IP"};
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
            _uploadToolbar = new UploadToolbar();

            var connectId = new CommandID(GuidList.GuidUploadExtensionCmdSet, (int) PkgCmdIDList.ConnectToTarget);
            _uploadToolbar.Connect = new MenuCommand(NotImplemeted, connectId);
            mcs.AddCommand(_uploadToolbar.Connect);

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
            _uploadToolbar.Properties = new MenuCommand(PropertiesCallback, propertiesId) { Enabled = true };
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

        private void NotImplemeted(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion

        private void HandleInvokeComboGetList(object sender, EventArgs e)
        {
            var args = e as OleMenuCmdEventArgs;
            if (args == null) return;
            if (args.OutValue != IntPtr.Zero)
            {
                Marshal.GetNativeVariantForObject(_ips.ToArray(), args.OutValue);
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
                if (inValue != "*Enter new TRIK IP")
                    _uploadToolbar.DropDownListMessage = inValue;
                else
                {
                    var form = new Form();
                    var textbox = new TextBox();
                    form.Controls.Add(textbox);
                    form.Closed += (o, eventArgs) =>
                    {
                        IPAddress ip;
                        if (!IPAddress.TryParse(textbox.Text, out ip)) return;
                        if (!_ips.Contains(ip.ToString())) _ips.Insert(_ips.Count - 1, ip.ToString());
                        _uploadToolbar.DropDownListMessage = textbox.Text;
                    };
                    form.ShowDialog();
                }
                var newIp = _uploadToolbar.DropDownListMessage;
                if (newIp == "Enter TRIK ip") return; //Early first case (DropDownListMessage hadn't been modified and input in a textbox was incorrect)
                if (Uploader == null || Uploader.Ip != newIp)
                    ConnectToTargetCallback(_uploadToolbar.DropDownListMessage);
            }
        }

        private void PropertiesCallback(object sender, EventArgs e)
        {
            var projects = Uploader.SolutionManager.Projects.Select(x => x.ProjectName).ToList();
            var form = new Form();
            var list = new ComboBox();
            list.Items.AddRange(projects.ToArray());
            form.Controls.Add(list);
            form.ShowDialog();
            //list.
        }

        private void StopProgramCallback(object sender, EventArgs e)
        {
            _visualStudio.WindowPane.SetText("========== Killing TRIK application ==========\n");
            Uploader.StopProgram();
        }



        private void RunProgramCallback(object sender, EventArgs e)
        {
            _visualStudio.WindowPane.SetText("========== Starting an Application on TRIK ==========\n");

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
            _visualStudio.Statusbar.SetText("Uploading...");
            _uploadToolbar.Upload.Enabled = false;
            _uploadToolbar.RunProgram.Enabled = false;
            var dte = (DTE2) GetService(typeof (DTE));
            var buildConfiguration = dte.Solution.SolutionBuild.ActiveConfiguration.Name;

            if ("Release" != buildConfiguration)
            {
                const string message = "Use Release build for better performance";
                _visualStudio.WindowPane.SetText(message);
                _visualStudio.Statusbar.SetText(message);
                _uploadToolbar.Upload.Enabled = true;
                return;
            }
            if (Uploader.SolutionManager == null ||
                Uploader.SolutionManager.FullName != dte.Solution.FullName)
            {
                var solution = dte.Solution;
                Uploader.SolutionManager = new SolutionManager(solution.FullName,
                    _visualStudio.GetSolutionProjects(solution.Projects));
                Uploader.SolutionManager.ActiveProject = Uploader.SolutionManager.Projects.First();//TODO: replace with user choice
            }
            else
            {
                var projects = _visualStudio.GetSolutionProjects(dte.Solution);
                await Tasks.Task.Run(() => Uploader.SolutionManager.UpdateProjects(projects));
            }
            _visualStudio.Statusbar.Progress(8000, "Uploading");
            var error = await Uploader.UploadActiveProjectAsync();
            await _visualStudio.Statusbar.StopProgressAsync();
            if (error.Length != 0)
            {
                _visualStudio.WindowPane.SetText(error + "/n/n/ Trying to reconnect...");
                Reconnect();
            }
            else
            {
                _visualStudio.Statusbar.SetText("Uploaded!");
                _visualStudio.WindowPane.SetText("Uploaded!");
                _uploadToolbar.RunProgram.Enabled = true;
                _uploadToolbar.Upload.Enabled = true;
            }

        }

        private async void ConnectToTargetCallback(string ip)
        {
            _uploadToolbar.Connect.Enabled = false;
            _uploadToolbar.RunProgram.Enabled = false;

            _visualStudio.Statusbar.SetText("Connecting...");
            _uploadToolbar.Upload.Enabled = false;
            const int dueTime = 11000; //Usual time is taken for connection with a controller
            _visualStudio.Statusbar.Progress(dueTime, "Connecting");
            var error = "";
            Uploader = new Uploader(ip) { OutputAction = _visualStudio.WindowPane.AppendText };
            try
            {
                await Uploader.ConnectAsync();
                await _visualStudio.Statusbar.StopProgressAsync();
                _visualStudio.Statusbar.SetText("Connected!");
                _uploadToolbar.Upload.Enabled = true;
            }
            catch (Exception exeption)
            {
                error = exeption.Message;
            }
            if (error == "") return;

            await _visualStudio.Statusbar.StopProgressAsync();
            _visualStudio.Statusbar.SetText("Connection attempt failed. See Output pane for details");
            _visualStudio.WindowPane.SetText(error);
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
                _visualStudio.Statusbar.Progress(8000, "Network error is occurred. Trying to reconnect");
                await Uploader.ReconnectAsync();
            }
            catch (Exception)
            {
                error = "Can't connect to TRIK. Check connection and try again";
                Uploader = null;
                _uploadToolbar.Connect.Enabled = true;
                _uploadToolbar.RunProgram.Enabled = false;
                _uploadToolbar.Upload.Enabled = false;
            }
            await _visualStudio.Statusbar.StopProgressAsync();
            _visualStudio.Statusbar.SetText(error == "" ? "Connected!" : error);
            _uploadToolbar.Upload.Enabled = true;
        }
    }
}