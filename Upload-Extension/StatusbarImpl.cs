using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Trik.Upload_Extension
{
    internal class StatusbarImpl
    {
        private readonly IVsStatusbar _statusbar;
        private readonly SynchronizationContext _context;
        private CancellationTokenSource _cancellationTokenSource;
        private uint _statusbarCookie;

        internal StatusbarImpl(SynchronizationContext context, IVsStatusbar statusbar)
        {
            _statusbar = statusbar;
            _context = context;
            _cancellationTokenSource = new CancellationTokenSource();
        }
        internal void SetText(string text)
        {
            _context.Post( x => _statusbar.SetText(text), null);
        }

        internal async void Progress(int period, string text)
        {
            if (!_cancellationTokenSource.IsCancellationRequested) _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            //Action action = () =>
            //{
                var messageTail = "";
                const int iterations = 10;
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    for (var i = (uint) 0; i < iterations; i++)
                    {
                        _statusbar.Progress(ref _statusbarCookie, 1, text + messageTail, i, iterations);
                        messageTail = "." + ((messageTail.Length < 3) ? messageTail : "");
                        await Task.Delay(period/iterations);
                    }
                }
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
            _cancellationTokenSource.Cancel();
            _statusbar.Progress(ref _statusbarCookie, 1, "", 0, 0);
        }
    }
}
