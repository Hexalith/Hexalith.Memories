param(
    [string]$RepositoryRoot,
    [string]$ResultsRoot,
    [string[]]$Components,
    [scriptblock]$RunProjects
)

function Invoke-StagedTestProjects([string]$Staging, [scriptblock]$Publish) {
    $succeeded = $false
    try {
        & $RunProjects $Staging
        $succeeded = $true
    }
    finally {
        try {
            & $Publish
        }
        catch {
            if ($succeeded) { throw }
            Write-Error -ErrorAction Continue -ErrorRecord $_
        }
    }
}

if (-not $IsWindows) {
    $python = Get-Command python3 -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $python) {
        throw '-ResultsDirectory requires python3 for descriptor-relative cleanup on Linux and macOS.'
    }
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $python.Source
    $startInfo.WorkingDirectory = $RepositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    foreach ($argument in @(
        (Join-Path $PSScriptRoot 'prepare-test-results.py'), $RepositoryRoot, $ResultsRoot, '-ResultsDirectory'
    )) {
        [void]$startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    try {
        $ready = $process.StandardOutput.ReadLine()
        if ($null -eq $ready) {
            throw '-ResultsDirectory could not be prepared safely.'
        }
        $staging = ConvertFrom-Json -InputObject $ready
        Invoke-StagedTestProjects $staging {
            $process.StandardInput.WriteLine('publish')
            $process.StandardInput.Close()
            $output = $process.StandardOutput.ReadToEnd()
            $process.WaitForExit()
            if ($process.ExitCode -ne 0) {
                throw '-ResultsDirectory could not publish test reports safely.'
            }
            if ($output) { Write-Host ($output.TrimEnd()) }
        }
    }
    finally {
        try {
            $process.StandardInput.Close()
            $process.WaitForExit()
        }
        finally {
            $process.Dispose()
        }
    }
    return
}

if (-not ('WindowsTestResultsCleanup' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

public sealed class WindowsTestResultsCleanup : IDisposable
{
    private const string Marker = ".hexalith-memories-test-results";
    private static readonly byte[] Signature = Encoding.UTF8.GetBytes("Hexalith.Memories test results v1\n");
    private const uint ListDirectory = 0x1;
    private const uint ReadData = 0x1;
    private const uint WriteData = 0x2;
    private const uint ReadAttributes = 0x80;
    private const uint WriteAttributes = 0x100;
    private const uint DeleteAccess = 0x10000;
    private const uint Synchronize = 0x100000;
    private const uint DirectoryFile = 0x1;
    private const uint NonDirectoryFile = 0x40;
    private const uint Open = 1;
    private const uint Create = 2;
    private const uint OpenIf = 3;
    private const int FileBasicInfo = 0;
    private const int FileDispositionInfo = 4;
    private const int FileAttributeTagInfo = 9;
    private const int FileFullDirectoryInfo = 14;
    private const int FileFullDirectoryRestartInfo = 15;
    private const int FileDispositionInfoEx = 21;

    private readonly List<SafeFileHandle> handles = new List<SafeFileHandle>();
    private FileStream marker;

    private WindowsTestResultsCleanup() { }

    public static WindowsTestResultsCleanup Prepare(string repository, string results, string[] components)
    {
        var owner = new WindowsTestResultsCleanup();
        var handles = owner.handles;
        try
        {
            // Accept the checkout as the starting directory, as the wrapper does for dotnet.
            var root = CreateFileW(repository, ListDirectory | ReadAttributes,
                FileShare.ReadWrite, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero);
            handles.Add(root);
            if (root.IsInvalid)
            {
                throw Failure("open the checkout", repository, Marshal.GetLastWin32Error());
            }
            RejectWslRedirector(root, repository);

            string current = repository;
            foreach (string component in components)
            {
                current = Path.Combine(current, component);
                var directory = OpenRelative(handles[handles.Count - 1], component,
                    ListDirectory, OpenIf, DirectoryFile);
                handles.Add(directory);
                if (Attributes(directory, current).HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new IOException("-ResultsDirectory must not traverse a symbolic link or reparse point at '" + current + "'.");
                }
            }

            var destination = handles[handles.Count - 1];
            var entries = ReadEntries(destination, results);
            if (entries.Count == 0)
            {
                owner.marker = OpenMarker(destination, results, create: true);
                owner.marker.Write(Signature, 0, Signature.Length);
                owner.marker.Flush();
                return owner;
            }

            if (!entries.Exists(entry => entry.Key == Marker &&
                (entry.Value & (FileAttributes.ReparsePoint | FileAttributes.Directory)) == 0))
            {
                throw Unowned(results);
            }
            owner.marker = OpenMarker(destination, results, create: false);
            if (!HasOwnershipMarker(owner.marker))
            {
                throw Unowned(results);
            }

            // A different invocation can finish between the initial listing
            // and our marker open. Clean the entries seen under this lease.
            entries = ReadEntries(destination, results);
            // Keep the ownership marker and address each stale entry by its parent handle.
            CleanEntries(destination, entries, keepMarker: true);
            return owner;
        }
        catch
        {
            owner.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            marker?.Dispose();
        }
        finally
        {
            for (int index = handles.Count - 1; index >= 0; index--)
            {
                handles[index].Dispose();
            }
        }
    }

    public void PublishReports(string staging)
    {
        try
        {
            CopyReports(new DirectoryInfo(staging), handles[handles.Count - 1]);
        }
        catch (IOException error)
        {
            throw new IOException("-ResultsDirectory could not publish test reports: " + error.Message, error);
        }
    }

    private static void CopyReports(DirectoryInfo source, SafeFileHandle destination)
    {
        foreach (var entry in source.EnumerateFileSystemInfos())
        {
            if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                throw new IOException("Staged report '" + entry.FullName + "' is a reparse point.");
            }

            // Create each child relative to the held parent. A name collision
            // fails without opening, replacing, or merging into a prior entry.
            if (entry is DirectoryInfo directory)
            {
                using (var child = OpenRelative(destination, entry.Name, ListDirectory, Create, DirectoryFile))
                {
                    CopyReports(directory, child);
                }
            }
            else
            {
                using (var input = ((FileInfo)entry).OpenRead())
                using (var child = OpenRelative(destination, entry.Name, WriteData, Create, NonDirectoryFile))
                using (var output = new FileStream(child, FileAccess.Write))
                {
                    input.CopyTo(output);
                }
            }
        }
    }

    private static FileStream OpenMarker(SafeFileHandle directory, string results, bool create)
    {
        // An exclusive open blocks other Windows processes and WSL processes
        // opening this marker. The OS releases the handle on abnormal exit.
        SafeFileHandle handle;
        try
        {
            handle = OpenRelative(directory, Marker, create ? WriteData : ReadData,
                create ? Create : Open, NonDirectoryFile, FileShare.None);
        }
        catch (IOException error) when (error.InnerException is Win32Exception native &&
            (native.NativeErrorCode == 32 || native.NativeErrorCode == 80 || native.NativeErrorCode == 183))
        {
            throw new IOException("-ResultsDirectory is in use by another test run. Refusing to use '" + results + "'.", error);
        }
        try
        {
            if (Attributes(handle, Marker).HasFlag(FileAttributes.ReparsePoint))
            {
                throw Unowned(results);
            }
            return new FileStream(handle, create ? FileAccess.Write : FileAccess.Read);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    private static IOException Unowned(string results)
    {
        return new IOException("-ResultsDirectory must name an empty or runner-owned directory. Refusing to clean '" + results + "'.");
    }

    private static bool HasOwnershipMarker(FileStream stream)
    {
        if (stream.Length != Signature.Length)
        {
            return false;
        }
        foreach (byte expected in Signature)
        {
            if (stream.ReadByte() != expected)
            {
                return false;
            }
        }
        return stream.ReadByte() == -1;
    }

    private static void CleanEntries(SafeFileHandle parent, List<KeyValuePair<string, FileAttributes>> entries, bool keepMarker)
    {
        foreach (var entry in entries)
        {
            if (keepMarker && entry.Key == Marker)
            {
                continue;
            }

            bool isDirectory = entry.Value.HasFlag(FileAttributes.Directory);
            uint access = DeleteAccess | (isDirectory ? ListDirectory : 0);
            if (entry.Value.HasFlag(FileAttributes.ReadOnly))
            {
                access |= WriteAttributes;
            }
            using (var child = OpenRelative(parent, entry.Key, access,
                Open, isDirectory ? DirectoryFile : NonDirectoryFile))
            {
                FileAttributes attributes = Attributes(child, entry.Key);
                if (isDirectory && !attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    CleanEntries(child, ReadEntries(child, entry.Key), keepMarker: false);
                }
                Delete(child, entry.Key, attributes);
            }
        }
    }

    private static void Delete(SafeFileHandle child, string name, FileAttributes attributes)
    {
        // FILE_DISPOSITION_DELETE | FILE_DISPOSITION_IGNORE_READONLY_ATTRIBUTE
        uint disposition = 0x11;
        if (SetFileInformationByHandle(child, FileDispositionInfoEx, ref disposition, sizeof(uint)))
        {
            return;
        }

        int error = Marshal.GetLastWin32Error();
        // Older Windows builds and some filesystems support only the original disposition class.
        if (error != 1 && error != 50 && error != 87 && error != 124)
        {
            throw Failure("remove", name, error);
        }
        if (attributes.HasFlag(FileAttributes.ReadOnly))
        {
            FileAttributes writable = attributes & ~FileAttributes.ReadOnly;
            var basic = new BasicInfo { Attributes = writable == 0 ? FileAttributes.Normal : writable };
            if (!SetFileInformationByHandle(child, FileBasicInfo, ref basic, Marshal.SizeOf<BasicInfo>()))
            {
                throw Failure("clear the read-only attribute of", name, Marshal.GetLastWin32Error());
            }
        }
        byte delete = 1;
        if (!SetFileInformationByHandle(child, FileDispositionInfo, ref delete, sizeof(byte)))
        {
            throw Failure("remove", name, Marshal.GetLastWin32Error());
        }
    }

    private static List<KeyValuePair<string, FileAttributes>> ReadEntries(SafeFileHandle directory, string name)
    {
        const int BufferSize = 8192;
        const int NameOffset = 68; // FILE_FULL_DIR_INFO.FileName
        IntPtr buffer = Marshal.AllocHGlobal(BufferSize);
        var entries = new List<KeyValuePair<string, FileAttributes>>();
        try
        {
            int info = FileFullDirectoryRestartInfo;
            while (GetFileInformationByHandleEx(directory, info, buffer, BufferSize))
            {
                int next;
                IntPtr record = buffer;
                do
                {
                    var attrs = (FileAttributes)Marshal.ReadInt32(record, 56);
                    int nameLength = Marshal.ReadInt32(record, 60) / sizeof(char);
                    string entry = Marshal.PtrToStringUni(IntPtr.Add(record, NameOffset), nameLength);
                    if (entry != "." && entry != "..")
                    {
                        entries.Add(new KeyValuePair<string, FileAttributes>(entry, attrs));
                    }
                    next = Marshal.ReadInt32(record);
                    record = IntPtr.Add(record, next);
                }
                while (next != 0);
                info = FileFullDirectoryInfo;
            }

            int error = Marshal.GetLastWin32Error();
            if (error != 18 && error != 2) // ERROR_NO_MORE_FILES or ERROR_FILE_NOT_FOUND on an empty directory
            {
                throw Failure("list", name, error);
            }
            return entries;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static FileAttributes Attributes(SafeFileHandle handle, string name)
    {
        if (!GetFileInformationByHandleEx(handle, FileAttributeTagInfo, out ulong info, sizeof(ulong)))
        {
            throw Failure("inspect", name, Marshal.GetLastWin32Error());
        }
        return (FileAttributes)(uint)info;
    }

    private static SafeFileHandle OpenRelative(SafeFileHandle parent, string name, uint access, uint disposition,
        uint kind, FileShare share = FileShare.ReadWrite)
    {
        IntPtr text = Marshal.StringToHGlobalUni(name);
        IntPtr textDescriptor = Marshal.AllocHGlobal(Marshal.SizeOf<UnicodeString>());
        try
        {
            var nativeName = new UnicodeString { Length = checked((ushort)(name.Length * sizeof(char))), Buffer = text };
            nativeName.MaximumLength = nativeName.Length;
            Marshal.StructureToPtr(nativeName, textDescriptor, false);
            var attributes = new ObjectAttributes
            {
                Length = Marshal.SizeOf<ObjectAttributes>(),
                RootDirectory = parent.DangerousGetHandle(),
                ObjectName = textDescriptor,
                Attributes = 0x40 // OBJ_CASE_INSENSITIVE
            };
            // Open the reparse point itself so no lookup can leave the parent handle.
            // Deny delete sharing to pin Windows bindings until cleanup finishes.
            const uint SynchronousNoReparse = 0x00200020;
            int status = NtCreateFile(out SafeFileHandle handle, access | ReadAttributes | Synchronize,
                ref attributes, out _, IntPtr.Zero, 0, share, disposition,
                kind | SynchronousNoReparse, IntPtr.Zero, 0);
            GC.KeepAlive(parent);
            if (status < 0)
            {
                handle.Dispose();
                uint error = RtlNtStatusToDosError(status);
                if (kind == DirectoryFile && error == 267) // ERROR_DIRECTORY
                {
                    throw new IOException("-ResultsDirectory component '" + name + "' is not a directory or is a symbolic link.");
                }
                throw Failure("open", name, (int)error);
            }
            return handle;
        }
        finally
        {
            Marshal.FreeHGlobal(textDescriptor);
            Marshal.FreeHGlobal(text);
        }
    }

    private static IOException Failure(string operation, string path, int error)
    {
        var native = new Win32Exception(error);
        return new IOException("-ResultsDirectory could not " + operation + " '" + path + "': " + native.Message, native);
    }

    private static void RejectWslRedirector(SafeFileHandle root, string repository)
    {
        const uint FileNameOpened = 0x8;
        var path = new StringBuilder(512);
        uint length = GetFinalPathNameByHandleW(root, path, path.Capacity, FileNameOpened);
        if (length >= path.Capacity)
        {
            path.EnsureCapacity(checked((int)length + 1));
            length = GetFinalPathNameByHandleW(root, path, path.Capacity, FileNameOpened);
        }
        if (length == 0 || length >= path.Capacity)
        {
            throw Failure("identify the checkout filesystem for", repository, Marshal.GetLastWin32Error());
        }
        string name = path.ToString();
        // The WSL redirector can enumerate the parent when queried through a relative directory handle.
        if (name.StartsWith(@"\\?\UNC\wsl.localhost\", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith(@"\\?\UNC\wsl$\", StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("-ResultsDirectory cannot safely prepare a WSL UNC checkout from Windows PowerShell. Run the wrapper from within WSL.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UnicodeString { public ushort Length, MaximumLength; public IntPtr Buffer; }

    [StructLayout(LayoutKind.Sequential)]
    private struct ObjectAttributes
    {
        public int Length;
        public IntPtr RootDirectory, ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor, SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoStatusBlock { public IntPtr Status, Information; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BasicInfo
    {
        public long CreationTime, LastAccessTime, LastWriteTime, ChangeTime;
        public FileAttributes Attributes;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, FileShare share, IntPtr security,
        uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle handle, StringBuilder path, int capacity, uint flags);

    [DllImport("ntdll.dll")]
    private static extern int NtCreateFile(out SafeFileHandle handle, uint access, ref ObjectAttributes attributes,
        out IoStatusBlock status, IntPtr allocationSize, uint fileAttributes, FileShare share, uint disposition,
        uint options, IntPtr eaBuffer, uint eaLength);

    [DllImport("ntdll.dll")]
    private static extern uint RtlNtStatusToDosError(int status);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle handle, int info, IntPtr buffer, int bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle handle, int info, out ulong buffer, int bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int info, ref uint buffer, int bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int info, ref BasicInfo buffer, int bytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int info, ref byte buffer, int bytes);
}
'@
}

$staging = $null
if ($RunProjects) {
    $temporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    $staging = Join-Path $temporaryRoot ('memories-test-reports-' + [guid]::NewGuid().ToString('N'))
    if ($staging.StartsWith($RepositoryRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw '-ResultsDirectory requires a temporary directory outside the repository.'
    }
    [void][System.IO.Directory]::CreateDirectory($staging)
}

try {
    $owner = [WindowsTestResultsCleanup]::Prepare($RepositoryRoot, $ResultsRoot, $Components)
    try {
        if ($RunProjects) {
            Invoke-StagedTestProjects $staging {
                $owner.PublishReports($staging)
            }
        }
    }
    finally {
        $owner.Dispose()
    }
}
finally {
    if ($staging) { [System.IO.Directory]::Delete($staging, $true) }
}
