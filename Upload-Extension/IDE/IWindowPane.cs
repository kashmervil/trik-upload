namespace UploadExtension.IDE
{
    internal interface IWindowPane
    {
        void WriteLine(string message);
        void Write(string message);
        void Activate();
        void SetName(string text);
    }
}