namespace UploadExtension.IDE
{
    internal interface IWindowPane
    {
        void WriteLine(string message);
        void Write(string message);
        void Activate();
        void Clear();
        void SetName(string text);
    }
}