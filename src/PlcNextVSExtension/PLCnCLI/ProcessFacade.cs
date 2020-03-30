#region Copyright
///////////////////////////////////////////////////////////////////////////////
//
//  Copyright (c) Phoenix Contact GmbH & Co KG
//  This software is licensed under Apache-2.0
//
///////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Threading;

namespace PlcNextVSExtension.PLCnCLI
{
    class ProcessFacade : IDisposable
    {
        #region private members
        private readonly Process _internalProcess;
        private readonly bool _killOnDispose;
        private readonly string _processName;
        private readonly bool _outputReadStarted;
        private readonly bool _errorReadStarted;
        private bool _disposed;
        private readonly IOutputReceiver _outputReceiver;
        #endregion

        public ProcessFacade(string fileName, string arguments, IOutputReceiver outputReceiver, CancellationToken cancellationToken)
        {

            _killOnDispose = true;
            _outputReceiver = outputReceiver;
            ProcessStartInfo processInfo = new ProcessStartInfo(fileName, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Debug.WriteLine($"Starting process {processInfo.FileName} {processInfo.Arguments} in {processInfo.WorkingDirectory}.");
            _internalProcess = Process.Start(processInfo);
            if (_internalProcess != null && !_internalProcess.HasExited)
            {
                try
                {
                    _processName = _internalProcess.ProcessName;
                    _internalProcess.OutputDataReceived += InternalProcessOnOutputDataReceived;
                    _internalProcess.ErrorDataReceived += InternalProcessOnErrorDataReceived;
                    _internalProcess.EnableRaisingEvents = true;
                    _internalProcess.Exited += InternalProcessOnExited;
                    _internalProcess.BeginOutputReadLine();
                    _outputReadStarted = true;
                    _internalProcess.BeginErrorReadLine();
                    _errorReadStarted = true;
                }
                catch (Exception e)
                {
                    _processName = "unknown process";
                    Debug.WriteLine($"Error while starting process: {e}");
                    //this happens when the process exits somewhere in this if clause
                }

                cancellationToken.Register(Dispose);
            }
            else
            {
                _processName = "unknown process";
            }
        }


        private void InternalProcessOnExited(object sender, EventArgs e)
        {
        }

        public void WaitForExit()
        {
            if (!_internalProcess.HasExited)
            {
                _internalProcess.WaitForExit();
            }
        }

        private void InternalProcessOnOutputDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            if (dataReceivedEventArgs.Data != null)
            {
                _outputReceiver.WriteLine(dataReceivedEventArgs.Data);
            }
        }

        private void InternalProcessOnErrorDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            if (dataReceivedEventArgs.Data != null)
            {
                _outputReceiver.WriteLine($"[{_processName}]: {dataReceivedEventArgs.Data}");
            }
        }

        public int ExitCode
        {
            get { return _internalProcess.ExitCode; }
        }

        private void KillProcessAndChildren(int pid)
        {
            using (ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
                    ("Select * From Win32_Process Where ParentProcessID=" + pid))
            {
                ManagementObjectCollection processCollection = processSearcher.Get();

                // We must kill child processes first!
                foreach (ManagementObject mo in processCollection.OfType<ManagementObject>())
                {
                    KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"], CultureInfo.InvariantCulture)); //kill child processes(also kills childrens of childrens etc.)
                }
            }


            // Then kill parents.
            try
            {
                System.Diagnostics.Process proc = System.Diagnostics.Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            try
            {
                if (_internalProcess != null)
                {
                    _internalProcess.OutputDataReceived -= InternalProcessOnOutputDataReceived;
                    _internalProcess.ErrorDataReceived -= InternalProcessOnErrorDataReceived;
                    _internalProcess.Exited -= InternalProcessOnExited;
                    _internalProcess.EnableRaisingEvents = false;
                    if (_errorReadStarted)
                    {
                        _internalProcess.CancelErrorRead();
                    }
                    if (_outputReadStarted)
                    {
                        _internalProcess.CancelOutputRead();
                    }
                    if (_killOnDispose && !_internalProcess.HasExited)
                    {
                        _internalProcess.StandardInput.WriteLine();
                        KillProcessAndChildren(_internalProcess.Id);
                    }
                    _internalProcess.Dispose();
                }
            }
            catch (Exception e)
            {
                string additionalInformation = string.Empty;
                try
                {
                    if (_internalProcess != null)
                    {
                        additionalInformation = $"{_internalProcess.MainWindowTitle} - {_internalProcess.Id} : {_internalProcess.HasExited} - {_internalProcess.ExitTime}";
                    }
                }
                catch (Exception)
                {
                    //do nothing only for information gathering
                }
                Debug.WriteLine($"Error while closing process {e}{Environment.NewLine}{additionalInformation}");
            }
        }
    }
}
