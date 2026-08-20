using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Modern = iNKORE.UI.WPF.Modern.Controls;

namespace MythKit.Pages.StudentManager
{
    /// <summary>
    /// StudentManagerIndex.xaml 的交互逻辑
    /// </summary>
    public partial class StudentManagerIndex : UserControl
    {
        private void KillProcess(string processName)
        {
            foreach (Process process in Process.GetProcessesByName(processName))
            {
                try
                {
                    process.Kill();
                }
                catch { /* Ignore exceptions */ }
            }
        }
        private void DeleteRegistryKey(RegistryKey registryKey, string path, string subKeyName, bool showError = true)
        {
            try
            {
                RegistryKey subKey = registryKey.OpenSubKey(path, writable: true);
                subKey.DeleteSubKeyTree(subKeyName, throwOnMissingSubKey: false);
                subKey.DeleteValue(subKeyName, throwOnMissingValue: false);
            }
            catch (UnauthorizedAccessException ex)
            {
                if (showError)
                {
                    Modern.MessageBox.Show($"权限不足，无法删除注册表项: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                if (showError)
                {
                    Modern.MessageBox.Show($"发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        public StudentManagerIndex()
        {
            InitializeComponent();
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                AdminFlag.Visibility = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        public static readonly List<string> IFEOList = new List<string>() { "taskkill.exe", "ntsd.exe", "sidebar.exe", "Chess.exe", "FreeCell.exe", "Hearts.exe", "Minesweeper.exe", "PurblePlace.exe", "Majhong.exe", "SpiderSplitaire.exe", "bckgzm.exe", "chkrzm.exe", "shvlzm.exe", "Solitaire.exe", "winmine.exe", "PurplePlace.exe", "Magnify.exe", "sethc.exe", "QQPCTray.exe" };

        private void RemoveIFEO_Click(object sender, RoutedEventArgs e)
        {
            const string IFEORegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";
            foreach (var item in IFEOList)
            {
                DeleteRegistryKey(Registry.LocalMachine, IFEORegistryPath, item, false);
            }
            Modern.MessageBox.Show("已尝试解除所有禁用。请自行验证是否解除成功。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RecoveryKeyboard_Click(object sender, RoutedEventArgs e)
        {
            const string KeyboardLayoutRegistryPath = @"SYSTEM\CurrentControlSet\Control\Keyboard Layout";
            const string ScancodeMapValueName = "Scancode Map";

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(KeyboardLayoutRegistryPath, writable: true))
                {
                    if (key != null)
                    {
                        // 设置 "Scancode Map" 的值为指定的字节数组
                        byte[] scancodeMap = new byte[]
                        {
                            0x00, 0x00, 0x00, 0x00, // Header
                            0x00, 0x00, 0x00, 0x00, // Header
                            0x00, 0x00, 0x00, 0x00, // No mappings
                            0x00, 0x00, 0x00, 0x00, // Terminator
                            0x00, 0x00, 0x00, 0x00  // Terminator
                        };

                        key.SetValue(ScancodeMapValueName, scancodeMap, RegistryValueKind.Binary);
                    }
                    else
                    {
                        Modern.MessageBox.Show("无法找到注册表路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                Modern.MessageBox.Show("键盘已恢复", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException)
            {
                Modern.MessageBox.Show("权限不足，无法修改注册表", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Modern.MessageBox.Show($"发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RecoveryTaskManager_Click(object sender, RoutedEventArgs e)
        {
            DeleteRegistryKey(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableTaskMgr");
            Modern.MessageBox.Show("任务管理器已恢复", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RecoveryRegistryEditor_Click(object sender, RoutedEventArgs e)
        {
            DeleteRegistryKey(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableRegistryTools");
            Modern.MessageBox.Show("注册表编辑器已恢复", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RecoveryCMD_Click(object sender, RoutedEventArgs e)
        {
            DeleteRegistryKey(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableCMD");
            Modern.MessageBox.Show("CMD 已恢复", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void EndJFGLZS()
        {
            KillProcess("srvany");
            KillProcess("zmserv");
            KillProcess("jfglzs");
            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    // 获取主模块的文件路径
                    FileVersionInfo fileInfo = process.MainModule.FileVersionInfo;
                    // Modern.MessageBox.Show($"{fileInfo.OriginalFilename}, {fileInfo.FileDescription}", process.MainModule.FileName);
                    // 检查原始文件名是否匹配
                    if ("przs.exe".Equals(fileInfo.OriginalFilename, StringComparison.OrdinalIgnoreCase))
                    {
                        process.Kill();
                    }
                }
                catch 
                {
                }
            }
        }
        private void DisableManager_Click(object sender, RoutedEventArgs e)
        {
            const string IFEORegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";
            const string TargetExecutable = "jfglzs.exe";
            const string DebuggerValueName = "Debugger";
            const string DebuggerValueData = "null";

            try
            {
                EndJFGLZS();
                using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(IFEORegistryPath, writable: true))
                {
                    if (baseKey != null)
                    {
                        // 创建或打开目标子项
                        using (RegistryKey targetKey = baseKey.CreateSubKey(TargetExecutable))
                        {
                            if (targetKey != null)
                            {
                                // 设置 "Debugger" 值为 "null"
                                targetKey.SetValue(DebuggerValueName, DebuggerValueData, RegistryValueKind.String);

                                EndJFGLZS();
                                Modern.MessageBox.Show($"已结束并禁用机房管理助手", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                Modern.MessageBox.Show("无法创建或打开目标注册表项", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    else
                    {
                        Modern.MessageBox.Show("无法找到注册表路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Modern.MessageBox.Show("权限不足，无法修改注册表", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Modern.MessageBox.Show($"发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableManager_Click(object sender, RoutedEventArgs e)
        {
            const string IFEORegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

            DeleteRegistryKey(Registry.LocalMachine, IFEORegistryPath, "jfglzs.exe");
        }

        private void AdminFlag_Click(object sender, RoutedEventArgs e)
        {
            Home.Suggestions.AdministratorPermissionSuggestion.ButtonCallback();
        }

        private void RecoveryTrayContext_Click(object sender, RoutedEventArgs e)
        {
            DeleteRegistryKey(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "NoTrayContextMenu");
            Modern.MessageBox.Show("任务栏右键菜单已恢复。重启 Windows 资源管理器后生效", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void KillManager_Click(object sender, RoutedEventArgs e)
        {
            EndJFGLZS();
        }
    }
}
