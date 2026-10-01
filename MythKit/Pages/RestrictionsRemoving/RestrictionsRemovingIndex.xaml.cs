using Microsoft.Win32;
using MythKit.Properties;
using MythKit.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Modern = iNKORE.UI.WPF.Modern.Controls;

namespace MythKit.Pages.RestrictionsRemoving
{
    /// <summary>
    /// RestrictionsRemovingIndex.xaml 的交互逻辑
    /// </summary>
    public partial class RestrictionsRemovingIndex : UserControl
    {
        public RestrictionsRemovingIndex()
        {
            InitializeComponent();
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                AdminFlag.Visibility = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator) ? Visibility.Collapsed : Visibility.Visible;
            }
            JFGLZSPathBlock.Text = $"安装路径：{JFGLZSInstallPath ?? "未找到"}";
            OpenJFGLZSDirectoryButton.Visibility = (JFGLZSInstallPath == null) ? Visibility.Collapsed : Visibility.Visible;
            PRZSProcessNameBlock.Text = $"przs.exe 伪装进程名：{GetPRZSCopyProcessName()}.exe（此伪装进程名每日更新一次，用于保护机房管理助手运行）";
        }

        private void AdminFlag_Click(object sender, RoutedEventArgs e)
        {
            Home.Suggestions.AdministratorPermissionSuggestion.ButtonCallback();
        }
        #region Visual Basic 随机数生成器 API
        public static int VBMathRandomize(double Number)
        {
            int rndSeed = 327680;
            int num = ((!BitConverter.IsLittleEndian) ? BitConverter.ToInt32(BitConverter.GetBytes(Number), 0) : BitConverter.ToInt32(BitConverter.GetBytes(Number), 4));
            num = ((num & 0xFFFF) ^ (num >> 16)) << 8;
            rndSeed = (rndSeed & -16776961) | num;
            return rndSeed;
        }

        public static float VBMathRnd(int randomizedSeed)
        {
            return (int)((randomizedSeed * 1140671485L + 12820163) & 0xFFFFFF) / 16777216f;
        }
        #endregion
        #region 任务/服务 API
        private bool KillProcessByName(string processName)
        {
            bool killed = false;
            foreach (Process process in Process.GetProcessesByName(processName))
            {
                try
                {
                    process.Kill();
                    killed = true;
                }
                catch { /* Ignore exceptions */ }
            }
            return killed;
        }
        public static void StopService(string serviceName, int timeoutMs = 30000, bool throwWhenFail = true)
        {
            try
            {
                var sc = new ServiceController(serviceName);

                if (sc.Status == ServiceControllerStatus.Stopped ||
                    sc.Status == ServiceControllerStatus.StopPending)
                {
                    return;
                }

                if (!sc.CanStop)
                    throw new InvalidOperationException($"服务 {serviceName} 不支持停止。");

                sc.Stop();

                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(timeoutMs));
            }
            catch (Exception ex)
            {
                if (throwWhenFail)
                {
                    throw ex;
                }
            }
        }
        #endregion
        #region 机房管理助手
        public string GetPRZSCopyProcessName()
        {
            int seed = DateTime.Now.Month * DateTime.Now.Day;
            long randomValue = (long)Math.Round((double)VBMathRnd(VBMathRandomize(seed)) * 100000.0 * 3.0 + 1.0);
            string randomName = "";
            for (int digitIndex = 1; digitIndex <= 5; digitIndex++)
            {
                long charCode = randomValue % 10L + 105L;
                randomName = char.ToString(Convert.ToChar((int)charCode)) + randomName;
                randomValue /= 10L;
            }
            return randomName;
        }
        private string _jfglzsInstallPath = null; // 路径缓存值
        public string JFGLZSInstallPath
        {
            get
            {
                if (_jfglzsInstallPath != null)
                {
                    return _jfglzsInstallPath;
                }
                // 从注册表获取路径
                const string registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                _jfglzsInstallPath = Registry.LocalMachine.OpenSubKey(registryPath, false)?.GetValue("prozs", null)?.ToString() ??
                                     Registry.LocalMachine.OpenSubKey(registryPath, false)?.GetValue("jfglzsn", null)?.ToString() ?? null;
                _jfglzsInstallPath = Path.GetDirectoryName(_jfglzsInstallPath);
                return _jfglzsInstallPath;
            }
        }
        private void KillJFGLZS_Click(object sender, RoutedEventArgs e)
        {
            KillProcessByName("jfglzs");
            KillProcessByName("jfglzsn");
            KillProcessByName(GetPRZSCopyProcessName());
            string exceptionInfo = null;
            try
            {
                StopService("zmserv");
                KillProcessByName("zmserv");
            }
            catch (Exception ex)
            {
                exceptionInfo = ex.Message;
            }
            KillProcessByName("jfglzs");
            KillProcessByName("jfglzsn");
            KillProcessByName(GetPRZSCopyProcessName());
            Modern.ContentDialog successDialog = new Modern.ContentDialog()
            {
                Title = "操作完成",
                Content = "已尝试关闭机房管理助手" + (exceptionInfo == null ? "" : $"，期间遇到错误：{exceptionInfo}"),
                DefaultButton = Modern.ContentDialogButton.Close,
                CloseButtonText = "确认"
            };
            successDialog.ShowAsync();
        }
        private void ShellExecute(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + command,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                };
                Process.Start(psi);
            }
            catch { }
        }
        private void RecoverySystem_Click(object sender, RoutedEventArgs e)
        {
            RecoverySettingsDialog dialog = new RecoverySettingsDialog();
            dialog.ShowAsync();
            Task.Run(() =>
            {
                var results = RecoverySystem_Background();
                Dispatcher.Invoke(() => dialog.RecoveryResults = results);
            });
        }
        private Tuple<string, bool, string> RecoverySystem_TryRun(string actionName, Action action, string successPrompt = "")
        {
            try
            {
                action.Invoke();
                return Tuple.Create(actionName, true, successPrompt);
            }
            catch (Exception ex)
            {
                return Tuple.Create(actionName, false, ex.Message);
            }
        }

        // ref => https://GitHub.com/CreeperMPG/jfglzs
        private List<Tuple<string, bool, string>> RecoverySystem_Background()
            => new List<Tuple<string, bool, string>>
            {
                // 恢复命令提示符 (CMD)
                RecoverySystem_TryRun("恢复命令提示符", () => RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "Software\\Policies\\Microsoft\\Windows\\System",
                        "DisableCMD",
                        0,
                        RegistryValueKind.DWord
                    )),

                // 恢复注册表编辑器
                RecoverySystem_TryRun("恢复注册表编辑器", () => RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System",
                        "DisableRegistryTools",
                        0,
                        RegistryValueKind.DWord
                    )),

                // 恢复运行对话框 (Win+R)
                RecoverySystem_TryRun("恢复运行对话框", () => RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer",
                        "NoRun",
                        0,
                        RegistryValueKind.DWord
                    )),
                RecoverySystem_TryRun("删除运行框黑名单", () => RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer",
                        "DisallowRun",
                        0,
                        RegistryValueKind.DWord
                    )),

                // 恢复锁屏 (Win+L)
                RecoverySystem_TryRun("恢复锁屏", () => RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System",
                        "DisableLockWorkstation",
                        0,
                        RegistryValueKind.DWord
                    )),

                // 删除机房管理助手自启动项 (Run 键)
                RecoverySystem_TryRun("删除机房管理助手自启动项", () =>
                {
                    RegUtils.DeleteRegistryKey(Registry.LocalMachine, "SOFTWARE\\MICROSOFT\\WINDOWS\\CURRENTVERSION\\RUN", "prozs");
                    RegUtils.DeleteRegistryKey(Registry.LocalMachine, "SOFTWARE\\MICROSOFT\\WINDOWS\\CURRENTVERSION\\RUN", "jfglzsn");
                    RegUtils.DeleteRegistryKey(Registry.LocalMachine, "SOFTWARE\\WOW6432NODE\\MICROSOFT\\WINDOWS\\CURRENTVERSION\\RUN", "prozs");
                    RegUtils.DeleteRegistryKey(Registry.LocalMachine, "SOFTWARE\\WOW6432NODE\\MICROSOFT\\WINDOWS\\CURRENTVERSION\\RUN", "jfglzsn");
                }),

                // 启用 USB 存储
                RecoverySystem_TryRun("启用 USB 存储", () =>
                {
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SYSTEM\\CurrentControlSet\\Services\\usbstor",
                        "Start",
                        3,
                        RegistryValueKind.DWord
                    );
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SYSTEM\\ControlSet001\\Services\\usbstor",
                        "Start",
                        3,
                        RegistryValueKind.DWord
                    );
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SYSTEM\\ControlSet002\\Services\\usbstor",
                        "Start",
                        3,
                        RegistryValueKind.DWord
                    );
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SYSTEM\\ControlSet003\\Services\\usbstor",
                        "Start",
                        3,
                        RegistryValueKind.DWord
                    );
                }),

                // 恢复文件夹选项
                RecoverySystem_TryRun("恢复文件夹选项", () =>
                {
                    RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer",
                        "NoFolderOptions",
                        0,
                        RegistryValueKind.DWord
                    );

                    RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
                        "HideFileExt",
                        0,
                        RegistryValueKind.DWord
                    );

                    RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
                        "Hidden",
                        1,
                        RegistryValueKind.DWord
                    );
                }),

                // 恢复"显示隐藏文件"选项
                RecoverySystem_TryRun("恢复显示隐藏文件选项", () =>
                {
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\Folder\\Hidden\\SHOWALL",
                        "CheckedValue",
                        1,
                        RegistryValueKind.DWord
                    );

                    RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
                        "Hidden",
                        1,
                        RegistryValueKind.DWord
                    );
                }),

                // 恢复任务栏右键菜单
                RecoverySystem_TryRun("恢复任务栏右键菜单", () => RegUtils.WriteRegistryValue(
                    Registry.CurrentUser,
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer",
                    "NoTrayContextMenu",
                    0,
                    RegistryValueKind.DWord
                )),

                // 恢复任务视图按钮 (Win+Tab)
                RecoverySystem_TryRun("恢复任务视图按钮", () => RegUtils.WriteRegistryValue(
                    Registry.CurrentUser,
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced",
                    "ShowTaskViewButton",
                    1,
                    RegistryValueKind.DWord
                )),

                // ============================================================
                // 恢复浏览器功能
                // ============================================================

                // 恢复浏览器功能 - Internet Explorer
                RecoverySystem_TryRun("恢复 Internet Explorer 设置", () =>
                {
                    // 12. IE 恢复文件下载 (1803=0 允许下载)
                    RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Zones\\3",
                        "1803",
                        0,
                        RegistryValueKind.DWord
                    );

                    // 13. IE 恢复文件打开 (2200=0 允许打开)
                    RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Zones\\3",
                        "2200",
                        0,
                        RegistryValueKind.DWord
                    );

                    // 14. IE 恢复"另存为"
                    RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "Software\\Policies\\Microsoft\\Internet Explorer\\Restrictions",
                        "NoBrowserSaveAs",
                        0,
                        RegistryValueKind.DWord
                    );
                }),

                // 恢复浏览器功能 - Microsoft Edge
                RecoverySystem_TryRun("恢复 Microsoft Edge 设置", () =>
                {
                    // Edge 恢复下载
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SOFTWARE\\Policies\\Microsoft\\Edge",
                        "DownloadRestrictions",
                        0,
                        RegistryValueKind.DWord
                    );

                    // Edge 恢复另存为
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SOFTWARE\\Policies\\Microsoft\\Edge",
                        "SaveAs",
                        1,
                        RegistryValueKind.DWord
                    );

                    // Edge 恢复开发者工具 (F12)
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SOFTWARE\\Policies\\Microsoft\\Edge",
                        "DeveloperToolsAvailability",
                        0,
                        RegistryValueKind.DWord
                    );
                }),

                // 恢复浏览器功能 - Google Chrome
                RecoverySystem_TryRun("恢复 Google Chrome 设置", () =>
                {
                    // Chrome 恢复下载
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SOFTWARE\\Policies\\Google\\Chrome",
                        "DownloadRestrictions",
                        0,
                        RegistryValueKind.DWord
                    );

                    // Chrome 恢复另存为
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SOFTWARE\\Policies\\Google\\Chrome",
                        "SaveAs",
                        1,
                        RegistryValueKind.DWord
                    );

                    // Chrome 恢复开发者工具 (F12)
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SOFTWARE\\Policies\\Google\\Chrome",
                        "DeveloperToolsAvailability",
                        0,
                        RegistryValueKind.DWord
                    );
                }),

                // 恢复浏览器功能 - Mozilla Firefox
                RecoverySystem_TryRun("恢复 Mozilla Firefox 设置", () =>
                {
                    // Firefox 恢复下载
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SOFTWARE\\Policies\\Mozilla\\Firefox",
                        "DisableDownloads",
                        0,
                        RegistryValueKind.DWord
                    );

                    // Firefox 恢复 about:downloads 页面
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SOFTWARE\\Policies\\Mozilla\\Firefox",
                        "BlockAboutDownloads",
                        0,
                        RegistryValueKind.DWord
                    );

                    // Firefox 恢复开发者工具
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SOFTWARE\\Policies\\Mozilla\\Firefox",
                        "DeveloperToolsAvailability",
                        0,
                        RegistryValueKind.DWord
                    );
                }),

                // 删除浏览器组策略 (完整清除)
                RecoverySystem_TryRun("删除浏览器组策略", () =>
                {
                    // 删除 Chrome 组策略
                    RegUtils.DeleteRegistryKey(Registry.LocalMachine, "Software\\Policies\\Google", "Chrome");

                    // 删除 Edge 组策略
                    RegUtils.DeleteRegistryKey(Registry.LocalMachine, "Software\\Policies\\Microsoft", "Edge");
                }),

                // 恢复网络功能
                RecoverySystem_TryRun("恢复网络功能", () =>
                {
                    // 恢复 IPv6
                    RegUtils.WriteRegistryValue(
                        Registry.LocalMachine,
                        "SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters",
                        "DisabledComponents",
                        0,
                        RegistryValueKind.DWord
                    );

                    // 恢复 hosts 文件
                    string hostsPath = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\drivers\\etc\\hosts";
                    string hostsBackupPath = hostsPath + "-bak";

                    if (File.Exists(hostsBackupPath))
                    {
                        File.SetAttributes(hostsPath, FileAttributes.Normal);
                        File.SetAttributes(hostsBackupPath, FileAttributes.Normal);
                        File.SetAttributes(Path.GetDirectoryName(hostsPath), FileAttributes.Normal);
                        File.Copy(hostsBackupPath, hostsPath, true);
                    }

                    // 刷新 DNS 缓存
                    ShellExecute("ipconfig /flushdns");
                }),


                // 删除所有被劫持的工具
                RecoverySystem_TryRun("删除被劫持的系统工具/内置游戏", () =>
                {
                    string ifeoPath = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\";
                    string[] ifeoKeys = new string[]
                    {
                        "taskkill.exe", "ntsd.exe", "sidebar.exe", "Chess.exe",
                        "FreeCell.exe", "Hearts.exe", "Minesweeper.exe", "PurblePlace.exe",
                        "Mahjong.exe", "SpiderSolitaire.exe", "bckgzm.exe", "chkrzm.exe",
                        "shvlzm.exe", "Solitaire.exe", "winmine", "Magnify.exe",
                        "sethc.exe", "tasklist.exe", "route.exe"
                    };

                    foreach (string keyName in ifeoKeys)
                    {
                        RegUtils.DeleteRegistryKey(Registry.LocalMachine, ifeoPath, keyName);
                    }
                }),

                // 删除强制结束任务设置
                RecoverySystem_TryRun("删除强制结束任务设置", () =>
                {
                    RegUtils.DeleteRegistryKey(
                        Registry.CurrentUser,
                        "Control Panel\\Desktop",
                        "AutoEndTasks"
                    );
                }),

                // 恢复应用程序超时 (3000ms)
                RecoverySystem_TryRun("恢复应用程序超时", () =>
                {
                    RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "Control Panel\\Desktop",
                        "HungAppTimeout",
                        "3000",
                        RegistryValueKind.String
                    );
                }),

                // 恢复进程结束超时 (10000ms)
                RecoverySystem_TryRun("恢复进程结束超时", () =>
                {
                    RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "Control Panel\\Desktop",
                        "WaitToKillAppTimeout",
                        "10000",
                        RegistryValueKind.String
                    );
                }),

                // 删除启动延迟设置
                RecoverySystem_TryRun("删除启动延迟设置", () =>
                {
                    RegUtils.DeleteRegistryKey(
                        Registry.CurrentUser,
                        "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize",
                        "StartupDelayInMSec",
                        throwWhenNotExists: false
                    );
                }),

                // 恢复触摸键盘
                RecoverySystem_TryRun("恢复触摸键盘", () =>
                {
                    RegUtils.WriteRegistryValue(
                        Registry.CurrentUser,
                        "SOFTWARE\\Microsoft\\TabletTip\\1.7",
                        "TipbandDesiredVisibility",
                        1,
                        RegistryValueKind.DWord
                    );
                }),

                // 恢复文件/文件夹属性
                RecoverySystem_TryRun("恢复游戏/.NET文件夹隐藏属性", () =>
                {
                    // 恢复 Microsoft Games 文件夹
                    string gamesPath = "C:\\Program Files\\Microsoft Games";
                    if (Directory.Exists(gamesPath))
                    {
                        File.SetAttributes(gamesPath, FileAttributes.Normal);
                    }

                    // 恢复 Windows.NET 文件夹
                    string netPath = "C:\\Windows\\Microsoft.NET";
                    if (Directory.Exists(netPath))
                    {
                        File.SetAttributes(netPath, FileAttributes.Normal);
                    }
                }),
                RecoverySystem_TryRun("恢复 Microsoft Store", () =>
                {
                    // 恢复 Microsoft Store
                    RegUtils.DeleteRegistryKey(
                        Registry.LocalMachine,
                        "SOFTWARE\\Policies\\Microsoft\\WindowsStore",
                        "RemoveWindowsStore",
                        throwWhenNotExists: false
                    );
                }, "修改后可能需要重新登录 Windows"),

                // 移除 Scancode Map
                RecoverySystem_TryRun("移除键盘映射", () =>
                {
                    RegUtils.DeleteRegistryKey(
                        Registry.LocalMachine,
                        "SYSTEM\\CurrentControlSet\\Control\\Keyboard Layout",
                        "Scancode Map",
                        throwWhenNotExists: false
                    );
                }, "修改后可能需要重新登录 Windows")
            };

        private void OpenJFGLZSDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (JFGLZSInstallPath != null)
            {
                Process.Start("explorer.exe", JFGLZSInstallPath);
            }
        }

        private void IFEOJFGLZS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ifeoPath = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\";
                RegUtils.WriteRegistryValue(Registry.LocalMachine, ifeoPath + "jfglzsn.exe", "Debugger", "null");
                RegUtils.WriteRegistryValue(Registry.LocalMachine, ifeoPath + "jfglzs.exe", "Debugger", "null");
                Modern.ContentDialog successDialog = new Modern.ContentDialog()
                {
                    Title = "操作完成",
                    Content = "已尝试映像劫持机房管理助手。现在机房管理助手无法运行。",
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                };
                successDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Modern.ContentDialog successDialog = new Modern.ContentDialog()
                {
                    Title = "遇到错误",
                    Content = $"映像劫持机房管理助手时遇到错误：\n{ex.Message})",
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                };
                successDialog.ShowAsync();
            }
        }

        private void DelIFEOJFGLZS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ifeoPath = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\";
                RegUtils.DeleteRegistryKey(Registry.LocalMachine, ifeoPath, "jfglzsn.exe");
                RegUtils.DeleteRegistryKey(Registry.LocalMachine, ifeoPath, "jfglzs.exe");
                Modern.ContentDialog successDialog = new Modern.ContentDialog()
                {
                    Title = "操作完成",
                    Content = "已尝试移除对机房管理助手映像劫持",
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                };
                successDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Modern.ContentDialog successDialog = new Modern.ContentDialog()
                {
                    Title = "遇到错误",
                    Content = $"移除对机房管理助手映像劫持时遇到错误：\n{ex.Message})",
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                };
                successDialog.ShowAsync();
            }
        }

        private void RestartExplorer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                KillProcessByName("explorer");
                Process.Start("explorer");
                Modern.ContentDialog successDialog = new Modern.ContentDialog()
                {
                    Title = "操作完成",
                    Content = "成功重启 Windows 资源管理器",
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                };
                successDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Modern.ContentDialog successDialog = new Modern.ContentDialog()
                {
                    Title = "遇到错误",
                    Content = $"重启 Windows 资源管理器时遇到错误：\n{ex.Message})",
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                };
                successDialog.ShowAsync();
            }
        }

        private void ClearPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegUtils.WriteRegistryValue(Registry.CurrentUser, "Software\\", "n", "92bf4bdc8277b626e73074ea254f02bcabb989ab7b970d00829dc7ff89533e60"); // SHA256('114514' + 'bfdshgs')
                Modern.ContentDialog successDialog = new Modern.ContentDialog()
                {
                    Title = "操作完成",
                    Content = "成功重置助手密码为 114514",
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                };
                successDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Modern.ContentDialog successDialog = new Modern.ContentDialog()
                {
                    Title = "遇到错误",
                    Content = $"重置助手密码时遇到错误：\n{ex.Message})",
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                };
                successDialog.ShowAsync();
            }
        }
        #endregion
        #region 增霸卡密码
        private string ZBKPassword = null;
        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const uint FILE_SHARE_READ = 0x00000001;
        const uint FILE_SHARE_WRITE = 0x00000002;
        const uint OPEN_EXISTING = 3;
        static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private string GetZBKPassword()
        {
            // ref => https://www.luogu.com.cn/article/bzu60i4t
            IntPtr hDevice = INVALID_HANDLE_VALUE;
            ulong baseLBA = 0;
            uint dwBytesReturned;

            // 1. 遍历物理磁盘，找到存储增霸卡信息的磁盘并获取基地址(LBA)
            for (int diskIndex = 0; diskIndex < 20; ++diskIndex)
            {
                string deviceName = string.Format("\\\\.\\PhysicalDrive{0}", diskIndex);
                hDevice = NativeMethods.CreateFile(deviceName,
                                     GENERIC_READ | GENERIC_WRITE,
                                     FILE_SHARE_READ | FILE_SHARE_WRITE,
                                     IntPtr.Zero,
                                     OPEN_EXISTING,
                                     0,
                                     IntPtr.Zero);

                if (hDevice == INVALID_HANDLE_VALUE)
                    continue;

                // 发送 IOCTL_ZENGBA_GET_LBA 控制码 (0x72054) 获取基地址
                byte[] inBuffer = new byte[8]; // 8字节，初始化为0
                bool bResult = NativeMethods.DeviceIoControl(hDevice, 0x72054, inBuffer, 8, inBuffer, 8, out dwBytesReturned, IntPtr.Zero);

                if (bResult)
                {
                    ulong value = BitConverter.ToUInt64(inBuffer, 0);
                    if (value != 0)
                    {
                        baseLBA = value;
                        Console.WriteLine("Found Zengba Card data on PhysicalDrive{0}. Base LBA: {1}", diskIndex, baseLBA);
                        break; // 找到后退出循环，保持hDevice打开
                    }
                }

                // 未找到有效数据，关闭句柄继续下一个磁盘
                NativeMethods.CloseHandle(hDevice);
                hDevice = INVALID_HANDLE_VALUE;
            }

            if (baseLBA == 0)
            {
                throw new Exception("Error: Could not find a drive with Zengba Card data.");
            }

            // 2. 读取存储密码的扇区
            byte[] sectorBuffer = new byte[512];
            ulong targetLba = baseLBA + 6410;

            // 填充输入缓冲区：前8字节为目标LBA，后4字节为扇区数1
            byte[] lbaBytes = BitConverter.GetBytes(targetLba);
            byte[] countBytes = BitConverter.GetBytes((uint)1);
            Buffer.BlockCopy(lbaBytes, 0, sectorBuffer, 0, 8);
            Buffer.BlockCopy(countBytes, 0, sectorBuffer, 8, 4);

            // 发送 IOCTL_ZENGBA_READ_SECTOR 控制码 (0x7201C) 读取扇区
            bool readResult = NativeMethods.DeviceIoControl(hDevice, 0x7201C, sectorBuffer, 512, sectorBuffer, 512, out dwBytesReturned, IntPtr.Zero);

            if (!readResult)
            {
                int error = Marshal.GetLastWin32Error();
                NativeMethods.CloseHandle(hDevice);
                throw new Exception(string.Format("Error: Failed to read sector. LastError: {0}", error));
            }

            // 3. 解密数据
            // 密钥1: 0x48414947 的小端字节序列
            byte[] key1 = { 0x47, 0x49, 0x41, 0x48 };
            // 密钥2: 0x55414E47 的小端字节序列
            byte[] key2 = { 0x47, 0x4E, 0x41, 0x55 };
            for (int i = 0; i < 4; i++)
            {
                sectorBuffer[i] ^= key1[i];
                sectorBuffer[i + 4] ^= key2[i];
            }

            // 4. 输出密码

            // 将字节数组转换为字符串，截断至第一个空字符
            int nullIndex = Array.IndexOf(sectorBuffer, (byte)0, 0, 512);
            string passwordString = nullIndex >= 0
                ? Encoding.ASCII.GetString(sectorBuffer, 0, nullIndex)
                : Encoding.ASCII.GetString(sectorBuffer);

            NativeMethods.CloseHandle(hDevice);
            return passwordString;
        }

        private void GetZBKPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string password = GetZBKPassword();
                ZBKPasswordBlock.Text = $"密码: {password}";
                ZBKPassword = password;
                ZBKPasswordCopyButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ZBKPasswordBlock.Text = $"获取密码失败: {ex.Message}";
            }
        }

        private void ZBKPasswordCopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ZBKPassword != null)
            {
                Clipboard.SetText(ZBKPassword);
            }
        }
        #endregion
        #region 基础限制解除
        private void BasicRR_Settings_ControlPanel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegUtils.DeleteRegistryKey(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoControlPanel", false);
                RegUtils.DeleteRegistryKey(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoSettingsPage", false);
                new Modern.ContentDialog()
                {
                    Title = "操作完成",
                    Content = "已尝试启用设置和控制面板",
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                }.ShowAsync();
            }
            catch (Exception ex)
            {
                new Modern.ContentDialog()
                {
                    Title = "操作失败",
                    Content = ex.Message,
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                }.ShowAsync();
            }
        }
        private void BasicRR_TDNet_File_Filter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                KillProcessByName("MasterHelper");
                StopService("TDFileFilter", 3000, false);
                StopService("TDNetFilter", 3000, false);
                new Modern.ContentDialog()
                {
                    Title = "操作完成",
                    Content = "已尝试关闭极域U盘/网络过滤器",
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                }.ShowAsync();
            }
            catch (Exception ex)
            {
                new Modern.ContentDialog()
                {
                    Title = "操作失败",
                    Content = ex.Message,
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                }.ShowAsync();
            }
        }
        private void BasicRR_Windows_MMC_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RegUtils.DeleteRegistryKey(Registry.CurrentUser, "Software\\Policies\\Microsoft", "MMC", false);
                new Modern.ContentDialog()
                {
                    Title = "操作完成",
                    Content = "已尝试解禁 Windows 管理控制台",
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                }.ShowAsync();
            }
            catch (Exception ex)
            {
                new Modern.ContentDialog()
                {
                    Title = "操作失败",
                    Content = ex.Message,
                    DefaultButton = Modern.ContentDialogButton.Close,
                    CloseButtonText = "确认"
                }.ShowAsync();
            }
        }
        #endregion
    }
}
