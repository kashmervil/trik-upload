using Microsoft.VisualStudio.Shell.Interop;

namespace Trik.Upload_Extension
{
    internal class WindowPaneImpl
    {
        private readonly IVsOutputWindowPane _pane;

        internal WindowPaneImpl(IVsOutputWindowPane pane)
        {
            _pane = pane;
        }

        internal void WriteLine(string message)
        {
            _pane.Activate();
            _pane.OutputStringThreadSafe(message + "\n");
        }
        internal void Write(string message)
        {
            _pane.Activate();
            _pane.OutputStringThreadSafe(message);
        }

        internal void Activate()
        {
            _pane.Activate();
        }

        internal void SetName(string text)
        {
            _pane.SetName(text);
        }
    }
}