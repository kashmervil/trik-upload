using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet;
using System.IO;
using System.Timers;
using Microsoft.VisualStudio.Shell;

namespace Trik.Upload_Extension
{
    public class Uploader : IDisposable
    {
        readonly Dictionary<string, DateTime> _lastUploaded = new Dictionary<string, DateTime>();
        readonly ScpClient _scpClient;
        readonly SshClient _sshClient;
        readonly Timer _timer = new Timer(5000.0);
        string _projectPath = "";
        string _projectName = "";
        
        public Uploader(string ip)
        {
            _scpClient = new ScpClient(ip, "root", "");
            _scpClient.Connect();
            _scpClient.KeepAliveInterval = TimeSpan.FromSeconds(10.0);

            _sshClient = new SshClient(ip, "root", "");
            _sshClient.Connect();
            _sshClient.KeepAliveInterval = TimeSpan.FromSeconds(10.0);
            
            _timer.Start();
            _timer.Elapsed += KeepAlive;
            _sshClient.RunCommand("mkdir /home/root/trik-sharp; mkdir /home/root/trik-sharp/uploads");
        }

        private void KeepAlive(object sender, ElapsedEventArgs e)
        {
 	        _scpClient.SendKeepAlive();
            _sshClient.SendKeepAlive();
        }

        private string getName(string fullName)
        {
            return fullName.Substring(fullName.LastIndexOfAny(new[] { '\\', '/' }) + 1);
        }

        private string GetUploadPath(string hostPath)
        {
            return @"/home/root/trik-sharp/uploads/" + _projectName + "/" + getName(hostPath);
        }



        private void UpdateScript()
        {
            var fullRemoteName = "/home/root/trik-sharp/" + _projectName;
            var executables = from file in Directory.GetFiles(_projectPath) 
                     where file.EndsWith(".exe") 
                     select getName(file);

            var script =
                   "echo \"#!/bin/sh\n"
                + "#killall trikGui\n"
                + "mono " + GetUploadPath(executables.First()) + " $* \n"
                + "cd ~/trik/\n"
                + "#./trikGui -qws &> /dev/null &"
                + "#some other commands\""
                + " > " + fullRemoteName
                + "; chmod +x " + fullRemoteName;
            _sshClient.RunCommand(script);
        }

        public void Update() 
        {
        var newFiles = Directory.GetFiles(ProjectPath);
        foreach (var file in newFiles)
            {
                if (!_lastUploaded.ContainsKey(file))
                {
                        _lastUploaded.Add(file, DateTime.MinValue);
                }
                var info = new FileInfo(file);
                if (info.LastWriteTime <= _lastUploaded[file]) continue;
                _lastUploaded[file] = info.LastWriteTime;
                _scpClient.Upload(info, GetUploadPath(file));
            }
        }

        public string RunProgram()
        {
            var program = _sshClient.RunCommand(@"sh ~/trik-sharp/" + _projectName);
            var msg = new StringBuilder("========== Run: ");
            if (program.Error != "")
                msg.Append("program failed with this Error ==========\n")
                    .Append(program.Error);
            else msg.Append("program succeeded with this Output ==========\n").Append(program.Result);
            return msg.ToString();
        }

        public string ProjectPath
        {
            get {return _projectPath;}
            set
            {
                var newProjectName = getName(value.TrimSuffix(".fsproj").TrimSuffix(".csproj"));
                var newProjectPath = (value.Substring(0, value.LastIndexOfAny(new[] { '\\', '/' }) + 1) + @"bin\Release\");

                if (_projectPath == newProjectPath) return;
                _projectPath = newProjectPath;
                _projectName = newProjectName;
                _lastUploaded.Clear();
                _sshClient.RunCommand("mkdir " + GetUploadPath(""));
                UpdateScript();
                var resources = Path.GetDirectoryName(typeof(Uploader).Assembly.Location);
                if (resources == null) return;
                var libconwrap = resources + @"\Resources\libconWrap.so.1.0.0";
                _scpClient.Upload(new FileInfo(libconwrap), GetUploadPath(libconwrap));
            }

        }

        public void Dispose()
        {
            _sshClient.Dispose();
            _scpClient.Dispose();
            _timer.Dispose();
        }
    }
}
