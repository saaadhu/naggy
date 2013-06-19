// Guids.cs
// MUST match guids.h
using System;

namespace Microsoft.Naggy_vspackage
{
    static class GuidList
    {
        public const string guidNaggy_vspackagePkgString = "7d67f3ba-816b-4f22-bb52-844e84b2f723";
        public const string guidNaggy_vspackageCmdSetString = "212b0bc0-4b23-43ad-bdcd-050d0079719a";

        public static readonly Guid guidNaggy_vspackageCmdSet = new Guid(guidNaggy_vspackageCmdSetString);
    };
}