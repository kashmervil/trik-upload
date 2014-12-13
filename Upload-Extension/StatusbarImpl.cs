using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;

namespace Trik.Upload_Extension
{
    internal class StatusbarImpl
    {
        private readonly IVsStatusbar _statusbar;
        private readonly SynchronizationContext _context;
        private CancellationTokenSource _cancellationTokenSource;
        private uint _statusbarCookie;
        private BackgroundWorker _worker;
        private AutoResetEvent _resetEvent = new AutoResetEvent(false);

        internal StatusbarImpl(SynchronizationContext context, IVsStatusbar statusbar)
        {
            _statusbar = statusbar;
            _context = context;
            _cancellationTokenSource = new CancellationTokenSource();
        }
        internal void SetText(string text)
        {
            _context.Post( x => _statusbar.SetText(text), null);//TODO: remove context
        }

        internal void Progress(int period, string text)
        {
            _resetEvent.Reset();
            _worker = new BackgroundWorker{WorkerSupportsCancellation = true};
            _worker.DoWork += (sender, args) =>
            {
                var worker = sender as BackgroundWorker;
                if (worker == null) return;

                var messageTail = "";
                const int iterations = 10;
                while (!worker.CancellationPending)
                {
                    for (var i = (uint) 0; i < iterations; i++)
                    {
                        _statusbar.Progress(ref _statusbarCookie, 1, text + messageTail, i, iterations);
                        messageTail = "." + ((messageTail.Length < 3) ? messageTail : "");
                        Thread.Sleep(period/iterations);
                    }
                }
                _resetEvent.Set();
            };
            _worker.RunWorkerAsync();
            /*}, _cancellationTokenSource.Token);
            _cancellationTokenSource.Token.Register(() =>
            {
                _statusbar.Clear();
                _statusbar.Progress(ref _statusbarCookie, 1, "", 0, 0);

            });
        */
        }

        internal void StopProgress()
        {
            _worker.RunWorkerCompleted += (sender, args) => _statusbar.Progress(ref _statusbarCookie, 1, "", 0, 0);
            _worker.CancelAsync();
            _resetEvent.WaitOne();
        }
    }
}
