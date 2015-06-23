using Microsoft.VisualStudio.Shell.Interop;
using UploadExtension.IDE;

namespace UploadExtension
{
    internal class VsWindowPane : IWindowPane
    {
        private readonly IVsOutputWindowPane _pane;

        internal VsWindowPane(IVsOutputWindowPane pane)
        {
            _pane = pane;
        }

        public void WriteLine(string message)
        {
            _pane.Activate();
            _pane.OutputStringThreadSafe(message + "\n");
        }

        public void Write(string message)
        {
            _pane.Activate();
            _pane.OutputStringThreadSafe(message);
        }

        public void Activate()
        {
            _pane.Activate();
        }

        public void SetName(string text)
        {
            _pane.SetName(text);
        }
    }
}