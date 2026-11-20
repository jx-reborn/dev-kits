using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JXReborn.ClientDriver;

/// <summary>
/// Wraps a Win32 Job Object configured with <c>JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE</c> so
/// headless <c>game.exe</c> processes never orphan when the test runner or host terminates.
/// </summary>
public sealed partial class Win32JobObject : IDisposable
{
    private const uint JobObjectLimitKillOnJobClose = 0x00002000;
    private const int JobObjectExtendedLimitInformationClass = 9;

    private nint _handle;
    private bool _disposed;

    /// <summary>
    /// Creates a new anonymous Win32 Job Object with <c>KILL_ON_JOB_CLOSE</c> enabled.
    /// </summary>
    public unsafe Win32JobObject()
    {
        nint handle = CreateJobObjectW(0, null);
        if (handle == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create Win32 job object.");
        }

        try
        {
            JobObjectExtendedLimitInformation info = default;
            info.BasicLimitInformation.LimitFlags = JobObjectLimitKillOnJobClose;

            if (!SetInformationJobObject(
                    handle,
                    JobObjectExtendedLimitInformationClass,
                    &info,
                    (uint)sizeof(JobObjectExtendedLimitInformation)))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to configure KILL_ON_JOB_CLOSE on Win32 job object.");
            }

            _handle = handle;
        }
        catch
        {
            _ = CloseHandle(handle);
            throw;
        }
    }

    /// <summary>
    /// Assigns the target process to this job object.
    /// </summary>
    public void AssignProcess(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        ObjectDisposedException.ThrowIf(_disposed || _handle == 0, this);

        if (!AssignProcessToJobObject(_handle, process.Handle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to assign process {process.Id} to Win32 job object.");
        }
    }

    /// <summary>
    /// Closes the job object handle, immediately terminating all assigned processes.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        nint handle = Interlocked.Exchange(ref _handle, 0);
        if (handle != 0)
        {
            _ = CloseHandle(handle);
        }

        GC.SuppressFinalize(this);
    }

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateJobObjectW(nint lpJobAttributes, string? lpName);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool SetInformationJobObject(
        nint hJob,
        int jobObjectInformationClass,
        void* lpJobObjectInformation,
        uint cbJobObjectInformationLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AssignProcessToJobObject(nint hJob, nint hProcess);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint hObject);
}
