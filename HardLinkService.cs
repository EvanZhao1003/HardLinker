using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace HardLinker;

internal static class HardLinkService
{
    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateHardLinkW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string newFileName, string existingFileName, IntPtr securityAttributes);

    public static void Create(string source, string destination, string existingTargetMessage)
    {
        if (File.Exists(destination) || Directory.Exists(destination))
            throw new IOException(existingTargetMessage);
        if (!CreateHardLink(destination, source, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
}
