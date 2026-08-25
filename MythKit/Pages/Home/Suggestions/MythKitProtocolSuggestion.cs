using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using MythKit.Utils;
using System;
using System.Diagnostics;
using System.Windows;

namespace MythKit.Pages.Home.Suggestions
{
    public static class UriProtocolRegistrar
    {
        public static bool IsProtocolRegisteredForCurrentUser(string scheme)
        {
            using (var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{scheme}"))
            {
                return key != null;
            }
        }
        public static void RegisterForCurrentUser(string scheme, string appPath, string description = "")
        {
            string keyPath = $@"Software\Classes\{scheme}";
            using (var rootKey = Registry.CurrentUser.CreateSubKey(keyPath))
            {
                // 设置默认描述
                rootKey.SetValue("", string.IsNullOrEmpty(description) ? $"URL:{scheme}" : description);
                // 标记为 URL 协议（必须）
                rootKey.SetValue("URL Protocol", "");

                // 可选：设置默认图标（使用 EXE 第一个图标）
                using (var iconKey = rootKey.CreateSubKey("DefaultIcon"))
                {
                    iconKey.SetValue("", $"{appPath},0");
                }

                // 设置启动命令（必须带 "%1" 以接收完整 URI）
                using (var shellKey = rootKey.CreateSubKey("shell"))
                using (var openKey = shellKey.CreateSubKey("open"))
                using (var commandKey = openKey.CreateSubKey("command"))
                {
                    // 路径中含空格必须加引号
                    commandKey.SetValue("", $"\"{appPath}\" \"%1\"");
                }
            }
        }
    }
    [Suggestion(
        "启用协议链接",
        "当前系统未注册 mythkit:// 协议，点击后可一键注册，方便从浏览器或外部调用本应用。",
        nameof(IsEnabled),
        "立即注册",
        nameof(ButtonCallback)
    )]
    internal static class MythKitProtocolSuggestion
    {
        public static bool IsEnabled()
        {
            return !UriProtocolRegistrar.IsProtocolRegisteredForCurrentUser("mythkit");
        }

        public static void ButtonCallback()
        {
            try
            {
                // 1. 获取当前 EXE 完整路径
                string exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "错误",
                        Content = "无法获取当前程序路径，注册失败。",
                        CloseButtonText = "确定"
                    };
                    errorDialog.ShowAsync(); // 不等待
                    return;
                }
                string keyPath = @"Software\Classes\mythkit";
                using (var rootKey = Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    rootKey.SetValue("", "URL:mythkit");
                    rootKey.SetValue("URL Protocol", "");

                    using (var iconKey = rootKey.CreateSubKey("DefaultIcon"))
                    {
                        iconKey.SetValue("", $"{exePath},0");
                    }

                    using (var shellKey = rootKey.CreateSubKey("shell"))
                    using (var openKey = shellKey.CreateSubKey("open"))
                    using (var commandKey = openKey.CreateSubKey("command"))
                    {
                        commandKey.SetValue("", $"\"{exePath}\" \"%1\"");
                    }
                }

                var successDialog = new ContentDialog
                {
                    Title = "注册完成",
                    Content = "mythkit:// 协议注册成功！现在你可以通过链接启动本应用。",
                    CloseButtonText = "确定"
                };
                successDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "注册失败",
                    Content = $"注册过程中发生错误：{ex.Message}",
                    CloseButtonText = "确定"
                };
                errorDialog.ShowAsync();
            }
        }
    }
}