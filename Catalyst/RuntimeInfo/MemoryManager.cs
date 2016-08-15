﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Catalyst.Unmanaged;

namespace Catalyst.RuntimeInfo
{
    /// <summary>
    /// A class to manage process memory.
    /// </summary>
    public class MemoryManager : IDisposable
    {
        #region static

        /// <summary>
        /// The standard flags for a MemoryManager object. Includes query of information and memory modification.
        /// </summary>
        public static ProcessAccessFlags DefaultAccessFlags =
            ProcessAccessFlags.QueryInformation |
            ProcessAccessFlags.VmWrite |
            ProcessAccessFlags.VmRead;

        /// <summary>
        /// Gets the base address of a module in a specific process as a pointer.
        /// </summary>
        /// <param name="proc">The process containing the module.</param>
        /// <param name="modulname">The module name.</param>
        /// <returns></returns>
        public static IntPtr GetModuleAddressPtr(Process proc, string modulname)
        {
            return (from module in proc.Modules.Cast<ProcessModule>()
                    where module.ModuleName == modulname
                    select module.BaseAddress).FirstOrDefault();
        }

        /// <summary>
        /// Gets the base address of a module in a specific process.
        /// </summary>
        /// <param name="proc">The process containing the module.</param>
        /// <param name="modulname">The module name.</param>
        /// <returns></returns>
        public static long GetModuleAddress(Process proc, string modulname)
        {
            return GetModuleAddressPtr(proc, modulname).ToInt64();
        }

        /// <summary>
        /// Reads a byte array of arbitrary length from the specified address.
        /// </summary>
        /// <param name="hProcess">The handle of the process to read from.</param>
        /// <param name="memaddress">The memory address we start reading from.</param>
        /// <param name="nBytes">The number of bytes to read.</param>
        /// <returns>If the read worked, return the bytes; otherwise null.</returns>
        public static byte[] ReadByteArray(IntPtr hProcess, long memaddress, int nBytes)
        {
            // Create a 64bit buffer to hold the data
            byte[] buffer = new byte[nBytes];

            // Read memory to the buffer and get the amount of read bytes
            var numRead = IntPtr.Zero;
            WinAPI.ReadProcessMemory(hProcess, memaddress, buffer, nBytes, out numRead);

            // Unsuccessful read, we don't have access or the address is wrong
            if (numRead.ToInt32() != nBytes)
                throw new UnauthorizedAccessException("Could not read memory");

            // Everything went okay, so return
            return buffer;
        }

        /// <summary>
        /// Reads a single byte from the specified address.
        /// </summary>
        /// <param name="hProcess">The handle of the process to read from.</param>
        /// <param name="memaddress">The memory address we start reading from.</param>
        /// <returns>If the read worked, return the byte; otherwise null.</returns>
        public static byte ReadSingleByte(IntPtr hProcess, long memaddress)
        {
            return ReadByteArray(hProcess, memaddress, 1)[0];
        }

        /// <summary>
        /// Reads a 64-bit int from the specified address.
        /// </summary>
        /// <param name="hProcess">The handle of the process to read from.</param>
        /// <param name="memaddress">The memory address we start reading from.</param>
        /// <returns></returns>
        public static long ReadInt64(IntPtr hProcess, long memaddress)
        {
            // Read the bytes using the other function
            byte[] bytes = ReadByteArray(hProcess, memaddress, 8);
            return BitConverter.ToInt64(bytes, 0);
        }

        #endregion static

        /// <summary>
        /// The loaded process.
        /// </summary>
        public Process Process { get; private set; }
        /// <summary>
        /// The handle of the loaded process.
        /// </summary>
        public IntPtr ProcHandle { get; private set; }
        /// <summary>
        /// True if the process has exited.
        /// </summary>
        public bool HasExited => Process.HasExited;

        /// <summary>
        /// Create a new MemoryManager.
        /// </summary>
        public MemoryManager()
        {
            Process = null;
            ProcHandle = IntPtr.Zero;
        }

        /// <summary>
        /// Load a process by name.
        /// </summary>
        /// <param name="exeName">The name of the executable.</param>
        public void OpenProcess(string exeName)
        {
            Process = Process.GetProcessesByName(exeName)[0];
            ProcHandle = WinAPI.OpenProcess(DefaultAccessFlags, false, Process.Id);
        }

        /// <summary>
        /// Load a process by name.
        /// </summary>
        /// <param name="exeName">The name of the executable.</param>
        /// <param name="access">The required access rights.</param>
        public void OpenProcess(string exeName, ProcessAccessFlags access)
        {
            Process = Process.GetProcessesByName(exeName)[0];
            ProcHandle = WinAPI.OpenProcess(access, false, Process.Id);
        }

        /// <summary>
        /// Load a process from an existing instance.
        /// </summary>
        /// <param name="proc">The process to open.</param>
        public void OpenProcess(Process proc)
        {
            Process = proc;
            ProcHandle = WinAPI.OpenProcess(DefaultAccessFlags, false, proc.Id);
        }

        /// <summary>
        /// Load a process from an existing instance.
        /// </summary>
        /// <param name="proc">The process to open.</param>
        /// <param name="access">The required access rights.</param>
        public void OpenProcess(Process proc, ProcessAccessFlags access)
        {
            Process = proc;
            ProcHandle = WinAPI.OpenProcess(access, false, proc.Id);
        }

        /// <summary>
        /// Close the link between the memory manager and the process.
        /// </summary>
        public void ReleaseProcess()
        {
            Process.Dispose();
            Process = null;
            WinAPI.CloseHandle(ProcHandle);
            ProcHandle = IntPtr.Zero;
        }

        /// <summary>
        /// Get a specific module from its name.
        /// </summary>
        /// <param name="moduleName">The name of the module to get.</param>
        /// <returns>The module itself.</returns>
        public ProcessModule GetProcessModule(string moduleName)
        {
            return (from module in Process.Modules.Cast<ProcessModule>()
                    where module.ModuleName == moduleName
                    select module).FirstOrDefault();
        }

        /// <summary>
        /// Get the address of a specific module from its name.
        /// </summary>
        /// <param name="moduleName">The name of the module in question.</param>
        /// <returns>A 64 bit integer containing the address of the module.</returns>
        public long GetModuleAddress(string moduleName)
        {
            return GetModuleAddress(Process, moduleName);
        }

        /// <summary>
        /// Reads a byte array of arbitrary length from the specified address.
        /// </summary>
        /// <param name="memaddress">The memory address we start reading from.</param>
        /// <param name="nBytes">The number of bytes to read.</param>
        /// <returns>If the read worked, return the bytes; otherwise null.</returns>
        public byte[] ReadByteArray(long memaddress, int nBytes)
        {
            if (HasExited) // Process is out, cannot read
                throw new InvalidOperationException("Process has exited, cannot read");

            // Create a 64bit buffer to hold the data
            byte[] buffer = new byte[nBytes];

            // Read memory to the buffer and get the amount of read bytes
            var numRead = IntPtr.Zero;
            WinAPI.ReadProcessMemory(ProcHandle, memaddress, buffer, nBytes, out numRead);

            // Unsuccessful read, we don't have access or the address is wrong
            if (numRead.ToInt32() != nBytes)
                throw new UnauthorizedAccessException("Could not read memory");

            // Everything went okay, so return
            return buffer;
        }

        /// <summary>
        /// Reads a single byte from the specified address.
        /// </summary>
        /// <param name="memaddress">The memory address we start reading from.</param>
        /// <returns>If the read worked, return the byte; otherwise null.</returns>
        public byte ReadSingleByte(long memaddress)
        {
            return ReadByteArray(ProcHandle, memaddress, 1)[0];
        }

        /// <summary>
        /// Reads a 64-bit int from the specified address.
        /// </summary>
        /// <param name="memaddress">The memory address we start reading from.</param>
        /// <returns></returns>
        public long ReadInt64(long memaddress)
        {
            // Read the long using the other function
            byte[] bytes = ReadByteArray(ProcHandle, memaddress, 8);
            return BitConverter.ToInt64(bytes, 0);
        }

        private bool disposed = false;

        /// <summary>
        /// Free resources used by this object.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    WinAPI.CloseHandle(ProcHandle);
                }
                Process.Dispose();

                disposed = true;
            }
        }

        /// <summary>
        /// Free resources used by this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
