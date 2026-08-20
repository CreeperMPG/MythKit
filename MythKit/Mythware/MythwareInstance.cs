using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MythKit.Utils;
using System.Windows;

namespace MythKit.Mythware
{
    public class MythwareInstance : INotifyPropertyChanged
    {
        public string MythwarePath
        {
            get
            {
                return _mythwarePath;
            }
            private set
            {
                if (_mythwarePath != value)
                {
                    _mythwarePath = value;
                    OnPropertyChanged(nameof(MythwarePath));
                }
            }
        }
        private string _mythwarePath;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void UpdateState()
        {
            MythwareState state = GetMythwareState();
            if (State != state)
            {
                State = state;
                OnPropertyChanged(nameof(State));
                OnPropertyChanged(nameof(Pid));
            }
        }
        public int Pid
        {
            get
            {
                if (State == MythwareState.Invalid || !File.Exists(MythwarePath))
                {
                    return -1;
                }
                var processName = Path.GetFileNameWithoutExtension(MythwarePath);
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    if (process.MainModule != null && process.MainModule.FileName.Equals(MythwarePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return process.Id;
                    }
                }
                return -1;
            }
        }

        public MythwareState State { get; private set; } = MythwareState.Invalid;

        public MythwareState GetMythwareState()
        {
            if (!File.Exists(MythwarePath))
            {
                return MythwareState.Invalid;
            }
            var processName = Path.GetFileNameWithoutExtension(MythwarePath);
            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                try
                {
                    if (process.MainModule != null && process.MainModule.FileName.Equals(MythwarePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return MythwareState.Running;
                    }
                }
                catch { }
            }
            return MythwareState.NotRunning;
        }
        public MythwareInstance(string mythwarePath)
        {
            MythwarePath = mythwarePath;
            UpdateState();
        }
        public static ObservableHashSet<MythwareInstance> GetInstances()
        {
            ObservableHashSet<MythwareInstance> result = new ObservableHashSet<MythwareInstance>();
            // Get the Mythware installation path from the registry
            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\TopDomain\\e-Learning Class Standard\\1.00"))
            {
                bool isRegistryKeyAvailable = registryKey != null;
                if (isRegistryKeyAvailable)
                {
                    object registryValue = registryKey.GetValue("TargetDirectory");
                    bool isRegistryValueAvailable = registryValue != null;
                    if (isRegistryValueAvailable)
                    {
                        string targetDirectoryPath = Path.Combine(registryValue.ToString(), "StudentMain.exe");
                        result.Add(new MythwareInstance(targetDirectoryPath));
                    }
                }
            }
            // With its running process
            var processes = Process.GetProcessesByName("StudentMain");
            foreach (var process in processes)
            {
                if (process.MainModule != null)
                {
                    result.Add(new MythwareInstance(process.MainModule.FileName));
                }
            }
            return result;
        }
        public override bool Equals(object obj)
        {
            return obj is MythwareInstance instance &&
                   MythwarePath == instance.MythwarePath;
        }
        public override int GetHashCode()
        {
            return MythwarePath.GetHashCode();
        }
        public bool Launch()
        {
            if (File.Exists(MythwarePath))
            {
                Process.Start(MythwarePath);
                UpdateState();
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool Kill()
        {
            var processName = Path.GetFileNameWithoutExtension(MythwarePath);
            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                if (process.MainModule != null && process.MainModule.FileName.Equals(MythwarePath, StringComparison.OrdinalIgnoreCase))
                {
                    process.Kill();
                    UpdateState();
                    return true;
                }
            }
            UpdateState();
            return false;
        }
    }
}
