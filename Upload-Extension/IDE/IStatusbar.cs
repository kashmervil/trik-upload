using System;
using System.Threading.Tasks;

namespace UploadExtension.IDE
{
    internal interface IStatusbar : IDisposable
    {
        bool InProgress { get; }
        void SetText(string text);
        void Progress(int period, string text);
        Task<bool> StopProgressAsync();
    }
}