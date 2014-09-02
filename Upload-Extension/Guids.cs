// Guids.cs
// MUST match guids.h
using System;

namespace TRIK.Upload_Extension
{
    static class GuidList
    {
        public const string guidUpload_ExtensionPkgString = "cc3f99c8-ffac-4cf9-8b9d-71a68baf8fbe";
        public const string guidUpload_ExtensionCmdSetString = "22fbb7ff-b0a9-47e1-bf8e-fa85d9138417";
        public const string guidToolWindowPersistanceString = "af9616b9-95b2-4ad3-8d2c-009771389822";

        public static readonly Guid guidUpload_ExtensionCmdSet = new Guid(guidUpload_ExtensionCmdSetString);
    };
}