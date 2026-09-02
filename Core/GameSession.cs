using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace EternalRingCompanion.Core;

/// <summary>
/// Owns the connection to a running PCSX2 process and every memory read/write the app makes.
/// All game addresses in this app are expressed as offsets from PCSX2's "EEmem" export (the
/// base of emulated PS2 RAM), which is re-resolved live so nothing is hardcoded across
/// emulator relaunches. A single shared instance (<see cref="Instance"/>) is used app-wide.
/// </summary>
public sealed class GameSession : IDisposable
{
    public static GameSession Instance { get; } = new();

    private const string EememExport = "EEmem";

    private readonly object _lock = new();
    private IntPtr _handle;
    private int _pid;
    private string? _processName;
    private long? _eememBase;

    private readonly Timer _freezeTimer;
    private readonly Timer _watchdogTimer;
    private readonly ConcurrentDictionary<string, FrozenValue> _frozen = new();

    /// <summary>Raised (possibly off the UI thread) whenever the connection state changes.</summary>
    public event Action? StateChanged;

    private GameSession()
    {
        _freezeTimer = new Timer(_ => ApplyFrozen(), null, 120, 120);
        _watchdogTimer = new Timer(_ => CheckAlive(), null, 1000, 1000);
    }

    public bool IsAttached
    {
        get { lock (_lock) return _handle != IntPtr.Zero; }
    }

    public int AttachedPid { get { lock (_lock) return _pid; } }
    public string? AttachedProcessName { get { lock (_lock) return _processName; } }

    public readonly record struct ProcessInfo(int Pid, string Name, string? WindowTitle);

    /// <summary>PCSX2 processes if any are running, otherwise every visible process.</summary>
    public static List<ProcessInfo> ListCandidateProcesses()
    {
        var all = Process.GetProcesses();
        var pcsx2 = all.Where(p => p.ProcessName.Contains("pcsx2", StringComparison.OrdinalIgnoreCase)).ToList();
        var chosen = pcsx2.Count > 0 ? pcsx2 : all.ToList();

        var result = new List<ProcessInfo>();
        foreach (var p in chosen.OrderBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                string? title = null;
                try { title = string.IsNullOrWhiteSpace(p.MainWindowTitle) ? null : p.MainWindowTitle; }
                catch { /* access denied for some system processes */ }
                result.Add(new ProcessInfo(p.Id, p.ProcessName, title));
            }
            catch { /* process exited between enumeration and access */ }
        }
        return result;
    }

    /// <summary>Attach to the single running PCSX2 process if there is exactly one. Returns it, or null.</summary>
    public ProcessInfo? TryAutoAttach()
    {
        var pcsx2 = ListCandidateProcesses()
            .Where(p => p.Name.Contains("pcsx2", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (pcsx2.Count != 1) return null;
        return Attach(pcsx2[0].Pid) ? pcsx2[0] : null;
    }

    public bool Attach(int pid)
    {
        string name;
        try { using var p = Process.GetProcessById(pid); name = p.ProcessName; }
        catch { return false; }

        var handle = NativeMethods.OpenProcess(NativeMethods.PROCESS_ALL_ACCESS, false, pid);
        if (handle == IntPtr.Zero)
            return false;

        lock (_lock)
        {
            if (_handle != IntPtr.Zero) NativeMethods.CloseHandle(_handle);
            _handle = handle;
            _pid = pid;
            _processName = name;
            _eememBase = null;
        }
        RaiseStateChanged();
        return true;
    }

    public void Detach()
    {
        lock (_lock)
        {
            if (_handle != IntPtr.Zero) NativeMethods.CloseHandle(_handle);
            _handle = IntPtr.Zero;
            _pid = 0;
            _processName = null;
            _eememBase = null;
        }
        _frozen.Clear();
        RaiseStateChanged();
    }

    private void CheckAlive()
    {
        int pid;
        lock (_lock)
        {
            if (_handle == IntPtr.Zero) return;
            pid = _pid;
        }
        try
        {
            using var p = Process.GetProcessById(pid);
            if (p.HasExited) Detach();
        }
        catch { Detach(); }
    }

    // ---- Raw memory ---------------------------------------------------------

    public byte[]? ReadAbsolute(long address, int size)
    {
        IntPtr handle;
        lock (_lock) handle = _handle;
        if (handle == IntPtr.Zero) return null;

        var buffer = new byte[size];
        if (!NativeMethods.ReadProcessMemory(handle, (IntPtr)address, buffer, (IntPtr)size, out var read) || (long)read != size)
            return null;
        return buffer;
    }

    public bool WriteAbsolute(long address, byte[] data)
    {
        IntPtr handle;
        lock (_lock) handle = _handle;
        if (handle == IntPtr.Zero) return false;

        bool ok = NativeMethods.WriteProcessMemory(handle, (IntPtr)address, data, (IntPtr)data.Length, out var written);
        return ok && (long)written == data.Length;
    }

    // ---- EEmem resolution -------------------------------------------------

    /// <summary>Live-resolves PCSX2's EEmem base (base of emulated PS2 RAM). Cached per attach.</summary>
    public long? ResolveEememBase()
    {
        lock (_lock)
        {
            if (_handle == IntPtr.Zero) return null;
            if (_eememBase is { } cached && cached != 0) return cached;
        }

        var moduleInfo = GetMainModuleInfo();
        if (moduleInfo == null) return null;
        var (moduleBase, modulePath) = moduleInfo.Value;

        long? rva;
        try { rva = ExportResolver.ResolveRva(modulePath, EememExport); }
        catch { return null; }
        if (rva == null) return null;

        var variableAddress = (long)moduleBase + rva.Value;
        var raw = ReadAbsolute(variableAddress, 8);
        if (raw == null) return null;

        long value = BitConverter.ToInt64(raw, 0);
        if (value == 0) return null;

        lock (_lock) _eememBase = value;
        return value;
    }

    private (IntPtr ModuleBase, string ModulePath)? GetMainModuleInfo()
    {
        int pid;
        lock (_lock)
        {
            if (_handle == IntPtr.Zero) return null;
            pid = _pid;
        }
        try
        {
            using var proc = Process.GetProcessById(pid);
            var mm = proc.MainModule;
            if (mm?.FileName == null) return null;
            return (mm.BaseAddress, mm.FileName);
        }
        catch { return null; }
    }

    // ---- EEmem-relative helpers ----------------------------------------

    public byte[]? Read(long eememOffset, int size)
    {
        var b = ResolveEememBase();
        return b == null ? null : ReadAbsolute(b.Value + eememOffset, size);
    }

    public bool Write(long eememOffset, byte[] data)
    {
        var b = ResolveEememBase();
        return b != null && WriteAbsolute(b.Value + eememOffset, data);
    }

    public long? ReadValue(long eememOffset, FieldType type)
    {
        var raw = Read(eememOffset, ValueCodec.SizeOf(type));
        return raw == null ? null : ValueCodec.ToInt64(type, raw);
    }

    // ---- Freeze loop --------------------------------------------------

    private sealed record FrozenValue(long EememOffset, byte[] Data);

    public void SetFrozen(string key, long eememOffset, byte[] data)
        => _frozen[key] = new FrozenValue(eememOffset, (byte[])data.Clone());

    public void ClearFrozen(string key) => _frozen.TryRemove(key, out _);

    public bool IsFrozen(string key) => _frozen.ContainsKey(key);

    private void ApplyFrozen()
    {
        if (_frozen.IsEmpty) return;
        var b = ResolveEememBase();
        if (b == null) return;
        foreach (var fv in _frozen.Values)
        {
            try { WriteAbsolute(b.Value + fv.EememOffset, fv.Data); }
            catch { /* transient */ }
        }
    }

    private void RaiseStateChanged()
    {
        var handler = StateChanged;
        if (handler == null) return;
        foreach (var d in handler.GetInvocationList())
        {
            try { ((Action)d).Invoke(); }
            catch { /* subscriber threw */ }
        }
    }

    public void Dispose()
    {
        _freezeTimer.Dispose();
        _watchdogTimer.Dispose();
        Detach();
    }
}
