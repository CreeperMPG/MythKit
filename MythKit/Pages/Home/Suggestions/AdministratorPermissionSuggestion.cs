using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MythKit.Pages.Home.Suggestions
{
    [Suggestion("管理员权限", "部分操作需要使用管理员权限，建议使用管理员权限运行。", nameof(IsEnabled), "以管理员权限重新启动程序", nameof(ButtonCallback))]
    internal static class AdministratorPermissionSuggestion
    {
        public static bool IsEnabled()
        {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return !principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
        }
        public static void ButtonCallback()
        {
            // 创建启动程序信息
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = AppDomain.CurrentDomain.FriendlyName,
                Arguments = Environment.CommandLine,
                UseShellExecute = true,
                Verb = "runas"  // 请求管理员权限
            };

            try
            {
                // 以管理员权限启动新实例
                System.Diagnostics.Process.Start(startInfo);
                
                // 关闭当前实例
                System.Windows.Application.Current.Shutdown();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("管理员权限授权失败", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                // 用户取消了 UAC 提示
                return;
            }
        }
    }
}
