using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Documents;
using Renci.SshNet;
using System.IO;
using System.Timers;
using Microsoft.VisualStudio.Shell;

namespace Trik.Upload_Extension
{
    public class Uploader
    {
        Dictionary<string, DateTime> lastUploaded = new Dictionary<string, DateTime>();
        ScpClient scpClient;
        SshClient sshClient;
        Timer timer = new Timer(5000.0);
        string projectPath = "";
        string projectName = "";
        
        public Uploader(string ip)
        {
            scpClient = new Renci.SshNet.ScpClient(ip, "root", "");
            scpClient.Connect();
            scpClient.KeepAliveInterval = TimeSpan.FromSeconds(10.0);

            sshClient = new SshClient(ip, "root", "");
            sshClient.Connect();
            sshClient.KeepAliveInterval = TimeSpan.FromSeconds(10.0);
            
            timer.Start();
            timer.Elapsed += KeepAlive;
            sshClient.RunCommand("mkdir /home/root/trik-sharp; mkdir /home/root/trik-sharp/uploads");
        }

        private void KeepAlive(object sender, ElapsedEventArgs e)
        {
 	        scpClient.SendKeepAlive();
            sshClient.SendKeepAlive();
        }

        private string getName(string fullName)
        {
            return fullName.Substring(fullName.LastIndexOfAny(new char[] { '\\', '/' }) + 1);
        }

        private string GetUploadPath(string hostPath)
        {
            return @"/home/root/trik-sharp/uploads/" + projectName + "/" + getName(hostPath);
        }



        private void UpdateScript()
        {
            var fullName = "/home/root/trik-sharp/" + projectName;
            var pe = from file in Directory.GetFiles(projectPath) 
                     where file.EndsWith(".exe") 
                     select getName(file);

            var script =
                   "echo \"#!/bin/sh\n"
                + "#killall trikGui\n"
                + "mono " + GetUploadPath(pe.First()) + " $* \n"
                + "cd ~/trik/\n"
                + "#./trikGui -qws &> /dev/null &"
                + "#some other commands\""
                + " > " + fullName
                + "; chmod +x " + fullName;
            sshClient.RunCommand(script);
        }

        public void Update() 
        {
        var newFiles = Directory.GetFiles(ProjectPath);
        foreach (var file in newFiles)
            {
                if (!lastUploaded.ContainsKey(file))
                {
                        lastUploaded.Add(file, DateTime.MinValue);
                }
                var info = new FileInfo(file);
                if (info.LastWriteTime <= lastUploaded[file]) continue;
                lastUploaded[file] = info.LastWriteTime;
                scpClient.Upload(info, GetUploadPath(file));
            }
        }

        public string RunProgram()
        {
            var program = sshClient.RunCommand(@"sh ~/trik-sharp/" + projectName);
            var msg = new StringBuilder("========== Run: ");
            if (program.Error != "")
                msg.Append("program failed with this Error ==========\n")
                    .Append(program.Error);
            else msg.Append("program succeeded with this Output ==========\n").Append(program.Result);
            return msg.ToString();
        }

        public string ProjectPath
        {
            get {return projectPath;}
            set
            {
                var newProjectName = getName(value.TrimSuffix(".fsproj"));
                var newProjectPath = (value.TrimSuffix(newProjectName + ".fsproj") + @"bin\Release\");

                if (projectPath == newProjectPath) return;
                projectPath = newProjectPath;
                projectName = newProjectName;
                lastUploaded.Clear();
                sshClient.RunCommand("mkdir " + GetUploadPath(""));
                UpdateScript();
                var resources = Path.GetDirectoryName(typeof(Uploader).Assembly.Location);
                if (resources == null) return;
                var libconwrap = resources + @"\Resources\libconWrap.so.1.0.0";
                scpClient.Upload(new FileInfo(libconwrap), GetUploadPath(libconwrap));
            }

        }
    }
}
