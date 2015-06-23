using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace UploadExtension
{
    internal sealed class Uploader : IDisposable
    {
        private readonly string _libconwrapPath = Path.GetDirectoryName(typeof (Uploader).Assembly.Location) +
                                                  @"\Resources\libconWrap.so.1.0.0";

        private readonly ScpClient _scpClient;
        private readonly SshClient _sshClient;
        private readonly Timer _timer = new Timer(5000.0);
        private Action<string> _logger;
        private ShellStream _shellStream;
        private EventHandler<ShellDataEventArgs> _shellStreamHandler;
        private StreamWriter _shellWriterStream;

        public Uploader(TargetProfile p)
        {
            Ip = p.IpAddress.ToString();
            UserName = p.Login;
            var password = p.Pass.ToString(); //TODO: Find better ssh library and remove vulnerability breach 
            _scpClient = new ScpClient(Ip, UserName, password);
            _sshClient = new SshClient(Ip, UserName, password);
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
        public string UserName { get; private set; }

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
            _shellStream = _sshClient.CreateShellStream("CONTROLLER-SHELL", 80, 24, 800, 600, 1024);
            _shellWriterStream = new StreamWriter(_shellStream) {AutoFlush = true};

            _timer.Start();
            _timer.Elapsed += KeepAlive;
            _sshClient.RunCommand(
                "mkdir -p /home/root/trik-sharp /home/root/trik-sharp/uploads /home/root/trik/scripts/trik-sharp");
            Task.Run(() => _scpClient.Upload(new FileInfo(_libconwrapPath),
                "/home/root/trik-sharp/uploads/" + Path.GetFileName(_libconwrapPath)));
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
                try
                {
                    _scpClient.Disconnect();
                    _scpClient.Connect();
                    _sshClient.Disconnect();
                    _sshClient.Connect();
                }
                catch (SshConnectionException)
                {
                    return false;
                }
                _shellStream = _sshClient.CreateShellStream("CONTROLLER-SHELL", 80, 24, 800, 600, 1024);
                _shellWriterStream = new StreamWriter(_shellStream) {AutoFlush = true};
                _timer.Elapsed += KeepAlive;
                return true;
            });
        }

        public void UploadFile(FileInfo localFileInfo, string remotePath)
        {
            _logger("Uploading " + Path.GetFileName(localFileInfo.Name) + "\n");
            _scpClient.Upload(localFileInfo, remotePath);
        }

        /// <summary>
        ///     Executes shell command without any output
        /// </summary>
        /// <param name="command"></param>
        public void ExecuteCommand(string command)
        {
#if LOG
            var d = _sshClient.RunCommand(command);
            _logger(String.Format("input: {0} \noutput: {1}\nerror: {2}", command, d.Result, d.Error));
#else
            _sshClient.RunCommand(command);
#endif
        }

        public void SendIdle()
            //Renci.SSH doesn't seem to set sshClient.IsConnected correctly (according to connection state). This hack helps to get the state
        {
            ExecuteCommand(" "); //TODO: Write Tests. Replace ssh backend if needed
        }

        /// <summary>
        ///     Sends command to ssh stream. So output can be seen through OutputAction callback
        /// </summary>
        public void SendCommandToStream(string command)
        {
            _shellStream.DataReceived -= _shellStreamHandler;
            _shellWriterStream.WriteLine(command);
            _shellStream.DataReceived += _shellStreamHandler;
        }

        public void RemoveFiles(string[] files, string remoteFolder)
        {
            if (files.Length == 0) return;
            var fileNames = files.Select(Path.GetFileName);
            ExecuteCommand("rm " + String.Join(" ", files.Select(s => "\"" + remoteFolder + s + "\"")));
            foreach (var s in fileNames)
            {
                _logger("Removing " + s + " from target");
            }
        }
    }
}