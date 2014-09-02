using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using System.IO;
using System.Reflection;
using System.Timers;

namespace TRIK.Upload_Extension
{
    public class Uploader
    {
        private Dictionary<string, DateTime> lastUploaded = new Dictionary<string, DateTime>();
        private ScpClient scpClient = new Renci.SshNet.ScpClient("10.0.40.42", "root", "");
        private SshClient sshClient = new SshClient("10.0.40.42", "root", "");
        private string folderPath = "";
        private string assembly = "";
        private Timer timer = new Timer(5000.0);
        
        public Uploader(string path)
        {
            this.folderPath = path;
            scpClient.Connect();
            scpClient.KeepAliveInterval = TimeSpan.FromSeconds(10.0);
            sshClient.Connect();
            sshClient.KeepAliveInterval = TimeSpan.FromSeconds(10.0);
            
            this.timer.Start();
            this.timer.Elapsed += keepAlive;
            var libconwrap =
            @"C:\Users\Alexander\Documents\GitHub\Trik-Observable\Source\BinaryComponents\libconWrap.so.1.0.0";
            sshClient.RunCommand("mkdir /home/root/trik-sharp; mkdir /home/root/trik-sharp/uploads");
            scpClient.Upload(new FileInfo(libconwrap), getUploadPath(libconwrap));
        }

        private void keepAlive(object sender, ElapsedEventArgs e)
        {
 	        scpClient.SendKeepAlive();
            sshClient.SendKeepAlive();

        }

        private string getUploadPath(string name)
        {
            return @"/home/root/trik-sharp/uploads/" + name.Substring(name.LastIndexOfAny(new char[] { '\\', '/' }) + 1);
        }

        private void UpdateScript()
        {
            var fullName = "/home/root/trik-sharp/" + assembly.ToLower();
            var helper =
                   "echo \"#!/bin/sh\n"
                + "killall trikGui\n"
                + "mono " + getUploadPath(assembly) + ".exe\n"
                + "cd ~/trik/\n"
                + "./trikGui -qws &> /dev/null &"
                + "#some other commands\""
                + " > " + fullName
                + "; chmod +x " + fullName;
            sshClient.RunCommand(helper);
        }

        public void Update() 
        {
        var newFiles = Directory.GetFiles(folderPath);
        foreach (var file in newFiles)
            {
                if (!this.lastUploaded.ContainsKey(file))
                {
                        this.lastUploaded.Add(file, DateTime.MinValue);
                }
                var info = new FileInfo(file);
                if (info.LastWriteTime > lastUploaded[file])
                {
                    lastUploaded[file] = info.LastWriteTime;
                    Console.WriteLine("uploading {0}", file);
                    scpClient.Upload(info, getUploadPath(file));
                }

            }
        }
        public string Assembly
        {
            get {return assembly;}
            set
            {
                assembly = value;//System.Reflection.AssemblyName(value).ToString();
                Console.WriteLine("Updating Assembly name to {0}", value);
                this.UpdateScript();
            }

        }
    }
}
