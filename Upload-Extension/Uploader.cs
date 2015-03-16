using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Trik.Upload_Extension
{
    internal sealed class Uploader : IDisposable
    {
        private readonly ScpClient _scpClient;
        private readonly SshClient _sshClient;
        private readonly Timer _timer = new Timer(5000.0);
        private ShellStream _shellStream;
        private EventHandler<ShellDataEventArgs> _shellStreamHandler;
        private StreamWriter _shellWriterStream;
        private Action<string> _logger;

        public Uploader(string ip)
        {
            Ip = ip;
            _scpClient = new ScpClient(Ip, "root", "");
            _sshClient = new SshClient(Ip, "root", "");
        }

        public Action<string> OutputAction
        {
            set
            {
                if (_shellStream != null) _shellStream.DataReceived -= _shellStreamHandler;
                _logger = value;
                _shellStreamHandler = (sender, args) => value(Encoding.UTF8.GetString(args.Data));
            }
        }

        public string Ip { get; private set; }

        public void Dispose()
        {
            _shellWriterStream.Dispose();
            _shellStream.Dispose();
            _sshClient.Dispose();
            _scpClient.Dispose();
            _timer.Dispose();
        }

        public void Connect()
        {
            _scpClient.Connect();
            _scpClient.KeepAliveInterval = TimeSpan.FromSeconds(10.0);

            _sshClient.Connect();
            _sshClient.KeepAliveInterval = TimeSpan.FromSeconds(10.0);
            _shellStream = _sshClient.CreateShellStream("TRIK-SHELL", 80, 24, 800, 600, 1024);
            _shellWriterStream = new StreamWriter(_shellStream) {AutoFlush = true};

            _timer.Start();
            _timer.Elapsed += KeepAlive;
            _sshClient.RunCommand(
                "mkdir /home/root/trik-sharp /home/root/trik-sharp/uploads /home/root/trik/scripts/trik-sharp");
        }

        private void KeepAlive(object sender, ElapsedEventArgs e)
        {
            _scpClient.SendKeepAlive();
            _sshClient.SendKeepAlive();
        }

        public async Task<bool> ReconnectAsync()
        {
            return await Task.Run(() =>
            {
                _timer.Elapsed -= KeepAlive;
                _scpClient.Disconnect();
                _scpClient.Connect();
                _sshClient.Disconnect();
                _sshClient.Connect();
                _shellStream = _sshClient.CreateShellStream("TRIK-SHELL", 80, 24, 800, 600, 1024);
                _shellWriterStream = new StreamWriter(_shellStream) {AutoFlush = true};
                _timer.Elapsed += KeepAlive;
                return true;
            });
        }

        public void UploadFile(FileInfo localFileInfo, string remotePath)
        {
            _logger("Uploading " + localFileInfo.FullName); 
            _scpClient.Upload(localFileInfo, remotePath);
            _logger( localFileInfo.Name + " Uploaded"); 
        }
        /// <summary>
        /// Executes shell command without any output
        /// </summary>
        /// <param name="command"></param>
        public void ExecuteCommand(string command)
        {
            _sshClient.RunCommand(command);
        }
        
        /// <summary>
        /// Sends command to ssh stream. So output can be seen through OutputAction callback
        /// </summary>
        public void SendCommandToStream(string command)
        {
            _shellStream.DataReceived -= _shellStreamHandler;
            _shellWriterStream.WriteLine(command);
            _shellStream.DataReceived += _shellStreamHandler;
        }

    }
}