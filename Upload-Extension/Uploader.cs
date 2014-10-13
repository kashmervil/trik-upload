using System;
using System.Collections.Generic;
using System.Linq;
using Renci.SshNet;
using System.IO;
using System.Timers;

namespace Trik.Upload_Extension
{
    public class Uploader : IDisposable
    {
        readonly Dictionary<string, DateTime> _lastUploaded = new Dictionary<string, DateTime>();
        readonly ScpClient _scpClient;
        readonly SshClient _sshClient;
        ShellStream _shellStream;
        private StreamWriter _shellWriterStream;
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
            _shellStream = _sshClient.CreateShellStream("TRIK-SHELL", 80, 24, 800, 600, 1024);
            _shellWriterStream = new StreamWriter(_shellStream) { AutoFlush = true };
            
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

        public void Reconnect()
        {
            _timer.Elapsed -= KeepAlive;
            _scpClient.Disconnect();
            _scpClient.Connect();
            _sshClient.Disconnect();
            _sshClient.Connect();
            _shellStream = _sshClient.CreateShellStream("TRIK-SHELL", 80, 24, 800, 600, 1024);
            _shellWriterStream = new StreamWriter(_shellStream);
            _timer.Elapsed += KeepAlive;
        }


        private void UpdateScript()
        {
            var fullRemoteName = "/home/root/trik-sharp/" + _projectName;
            var executables = from file in Directory.GetFiles(_projectPath) 
                     where file.EndsWith(".exe") 
                     select Path.GetFileName(file);

            var script =
                   "echo \"#!/bin/sh\n"
                + "#killall trikGui\n"
                + "mono " + GetUploadPath(executables.First()) + " $* \n"
                + "cd ~/trik/\n"
                + "#./trikGui -qws &> /dev/null &"
                + "#some other commands\""
                + " > " + fullRemoteName
                + "; chmod +x " + fullRemoteName;
            _shellWriterStream.Write(script);
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

        public ShellStream RunProgram()
        {
            _shellWriterStream.WriteLine(@"sh ~/trik-sharp/" + _projectName);    
            return _shellStream;
        }

        public string ProjectPath
        {
            get {return _projectPath;}
            set
            {
                var newProjectName = Path.GetFileNameWithoutExtension(value);
                var newProjectPath = Path.GetDirectoryName(value) + @"\bin\Release\";

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
