using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GitHelper
{
    /// <summary>One entry in Windows Credential Manager.</summary>
    public sealed class StoredCredential
    {
        /// <summary>The target name, e.g. <c>git:https://github.com</c>.</summary>
        public string Target { get; set; }

        public string UserName { get; set; }

        /// <summary>CRED_TYPE_*, needed to delete the entry again.</summary>
        public uint Type { get; set; }

        /// <summary>
        /// False for entries git never wrote - Visual Studio's GitHub
        /// extension keeps its own, and deleting one signs VS out.
        /// </summary>
        public bool WrittenByGit { get; set; }

        public override string ToString()
        {
            var who = string.IsNullOrEmpty(UserName) ? string.Empty : "   (" + UserName + ")";
            return Target + who + (WrittenByGit ? string.Empty : "   [not git's - Visual Studio]");
        }
    }

    /// <summary>
    /// Reads and deletes entries in Windows Credential Manager.
    ///
    /// <c>git credential reject</c> only reaches what git's *configured helper*
    /// owns. Anything written by another tool - Visual Studio's GitHub
    /// extension, an old helper that is no longer configured - survives it and
    /// is still listed under "Windows Credentials", which is exactly what makes
    /// a clear look like it did nothing.
    /// </summary>
    public static class WindowsCredentials
    {
        private const uint CRED_TYPE_GENERIC = 1;

        /// <summary>
        /// Entries that mention the host, or that are recognisably git's.
        /// Matching on the host keeps this away from the rest of the user's
        /// credentials - mail, network shares, everything else in there.
        /// </summary>
        public static List<StoredCredential> ForHost(string host)
        {
            var found = new List<StoredCredential>();
            if (string.IsNullOrEmpty(host)) return found;

            IntPtr credentials;
            uint count;
            if (!CredEnumerate(null, 0, out count, out credentials)) return found;

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var ptr = Marshal.ReadIntPtr(credentials, i * IntPtr.Size);
                    var cred = (CREDENTIAL)Marshal.PtrToStructure(ptr, typeof(CREDENTIAL));

                    var target = cred.TargetName ?? string.Empty;
                    if (target.IndexOf(host, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    found.Add(new StoredCredential
                    {
                        Target = target,
                        UserName = cred.UserName,
                        Type = cred.Type,

                        // git's own helpers write 'git:<url>'; everything else
                        // belongs to another application.
                        WrittenByGit = target.StartsWith("git:", StringComparison.OrdinalIgnoreCase),
                    });
                }
            }
            finally
            {
                CredFree(credentials);
            }
            return found;
        }

        /// <summary>Deletes one entry. Returns false with the Win32 reason.</summary>
        public static bool Delete(StoredCredential credential, out string error)
        {
            error = null;
            if (CredDelete(credential.Target, credential.Type, 0)) return true;

            var code = Marshal.GetLastWin32Error();
            error = code == 1168                     // ERROR_NOT_FOUND
                ? "already gone"
                : new System.ComponentModel.Win32Exception(code).Message;
            return false;
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CredEnumerateW", SetLastError = true)]
        private static extern bool CredEnumerate(string filter, uint flags, out uint count, out IntPtr credentials);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "CredDeleteW", SetLastError = true)]
        private static extern bool CredDelete(string target, uint type, uint flags);

        [DllImport("advapi32.dll", EntryPoint = "CredFree")]
        private static extern void CredFree(IntPtr buffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public uint Flags;
            public uint Type;
            [MarshalAs(UnmanagedType.LPWStr)] public string TargetName;
            [MarshalAs(UnmanagedType.LPWStr)] public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;          // never read: the secret is not this tool's business
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            [MarshalAs(UnmanagedType.LPWStr)] public string TargetAlias;
            [MarshalAs(UnmanagedType.LPWStr)] public string UserName;
        }
    }
}
