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

        internal void AppendText(string text)
        {
            _pane.Activate();
            _pane.OutputStringThreadSafe(text);
        }

        internal void Activate()
        {
            _pane.Activate();
        }

        internal void SetText(string text)
        {
            _pane.Activate();
            _pane.Clear();
            _pane.OutputStringThreadSafe(text);
        }

        internal void SetName(string text)
        {
            _pane.SetName(text);
        }
    }
}