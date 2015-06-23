// PkgCmdID.cs
// MUST match PkgCmdID.h

namespace UploadExtension
{
    internal static class PkgCmdIDList
    {
        public const uint ConnectToTarget = 0x300;
        public const uint ReconnectToTarget = 0x301;
        public const uint Disconnect = 0x302;
        public const uint UploadToTarget = 0x303;
        public const uint RunOnTarget = 0x304;
        public const uint StopEvaluating = 0x305;
        public const uint Properties = 0x306;
        public const uint GetIpList = 0x200;
        public const uint TargetIp = 0x201;
    };
}