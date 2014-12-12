using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;

namespace Trik.Upload_Extension
{
    internal class WindowPaneImpl
    {
        private readonly IVsOutputWindowPane _pane;
        private readonly SynchronizationContext _context;
        internal WindowPaneImpl(SynchronizationContext context, IVsOutputWindowPane pane)
        {
            _pane = pane;
            _context = context;
        }

        internal void AppendText(string text)
        {
            SendOrPostCallback postCallback = _ =>
            {
                _pane.Activate();
                _pane.OutputStringThreadSafe(text);
            };
            _context.Post(postCallback, null);
        }

        internal void Activate()
        {
            _context.Post(x => _pane.Activate(), null);
        }

        internal void SetText(string text)
        {
            SendOrPostCallback postCallback = _ =>
            {
                _pane.Activate();
                _pane.Clear();
                _pane.OutputStringThreadSafe(text);

            };
            _context.Post(postCallback, null);
        }

        internal void SetName(string text)
        {
            _context.Post(x => _pane.SetName(text), null);
        }

        

    }
}