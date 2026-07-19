// <copyright file="OpenBaoProtectedFileSystem.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.AppHost;

using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Versioning;
using System.Text;

/// <summary>Creates AppHost-owned files with fail-closed owner-only protection.</summary>
internal static class OpenBaoProtectedFileSystem
{
    private const UnixFileMode DirectoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private const UnixFileMode FileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    /// <summary>Creates an owner-only directory or fails closed.</summary>
    /// <param name="path">The process-unique AppHost-owned path.</param>
    internal static void CreateDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _ = Directory.CreateDirectory(path);
        if (OperatingSystem.IsWindows())
        {
            ApplyWindowsDirectoryAcl(path);
            return;
        }

        File.SetUnixFileMode(path, DirectoryMode);
        if (File.GetUnixFileMode(path) != DirectoryMode)
        {
            throw new InvalidOperationException("The OpenBao run directory could not be restricted to its owner.");
        }
    }

    /// <summary>Atomically installs an owner-only UTF-8 file in an owned directory.</summary>
    /// <param name="path">The final file path.</param>
    /// <param name="content">The protected content.</param>
    internal static void WriteAllTextAtomically(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);
        string directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("The protected file must have an owning directory.");
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            ProtectFile(temporaryPath);
            File.Move(temporaryPath, path, overwrite: true);
            ProtectFile(path);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>Restricts an existing file to its owner or fails closed.</summary>
    /// <param name="path">The owned file path.</param>
    internal static void ProtectFile(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            ApplyWindowsFileAcl(path);
            return;
        }

        File.SetUnixFileMode(path, FileMode);
        if (File.GetUnixFileMode(path) != FileMode)
        {
            throw new InvalidOperationException("An OpenBao run file could not be restricted to its owner.");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void ApplyWindowsDirectoryAcl(string path)
    {
        SecurityIdentifier owner = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("The current Windows identity has no security identifier.");
        var security = new DirectorySecurity();
        security.SetOwner(owner);
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(
            owner,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));
        new DirectoryInfo(path).SetAccessControl(security);
    }

    [SupportedOSPlatform("windows")]
    private static void ApplyWindowsFileAcl(string path)
    {
        SecurityIdentifier owner = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("The current Windows identity has no security identifier.");
        var security = new FileSecurity();
        security.SetOwner(owner);
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(owner, FileSystemRights.FullControl, AccessControlType.Allow));
        new FileInfo(path).SetAccessControl(security);
    }
}
