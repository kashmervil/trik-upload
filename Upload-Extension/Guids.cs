// Guids.cs
// MUST match guids.h

using System;

namespace Trik.Upload_Extension
{
    internal static class GuidList
    {
        public const string GuidUploadExtensionPkgString = "cc3f99c8-ffac-4cf9-8b9d-71a68baf8fbe";
        public const string GuidUploadExtensionCmdSetString = "22fbb7ff-b0a9-47e1-bf8e-fa85d9138417";
        public static readonly Guid GuidUploadExtensionCmdSet = new Guid(GuidUploadExtensionCmdSetString);
    };
}