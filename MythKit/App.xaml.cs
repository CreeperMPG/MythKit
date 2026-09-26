using MythKit.Pages.UDPAttack;
using MythKit.Tasks;
using MythKit.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Markup;
using Modern = iNKORE.UI.WPF.Modern.Controls;

namespace MythKit
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public const string VERSION = "1.2.3";
        protected override void OnExit(ExitEventArgs e)
        {
            SingleInstanceManager.Cleanup();
            base.OnExit(e);
        }
        public static List<string> DecodePrefixedStrings(string data)
        {
            if (data == null)
            {
                return new List<string>();
            }
            var result = new List<string>();
            int index = 0;
            while (index < data.Length)
            {
                if (index + 4 > data.Length)
                    break;
                int length = int.Parse(data.Substring(index, 4));
                index += 4;
                if (index + length > data.Length)
                    break;
                length = Math.Min(length, data.Length - index);
                result.Add(data.Substring(index, length));
                index += length;
            }
            return result;
        }
        public static TaskManager TaskManagerInstance { get; } = new TaskManager();
        private string _activatedUri = null;
        public static List<string> GetPathSegments(Uri uri)
        {
            if (uri == null) return new List<string>();

            return uri.Segments
                      .Select(s => s.Trim('/'))
                      .Where(s => !string.IsNullOrEmpty(s))
                      .ToList();
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var first = SingleInstanceManager.Initialize();
            if (!first)
            {
                Shutdown();
            }

            string[] args = Environment.GetCommandLineArgs();
            HomePage.HomePageLoadedEvent += () => SingleInstanceManager.OtherInstanceStarted += SingleInstanceManager_OtherInstanceStarted;

            foreach (string arg in args)
            {
                if (arg.StartsWith("mythkit://", StringComparison.OrdinalIgnoreCase))
                {
                    _activatedUri = arg;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(_activatedUri))
            {
                try
                {
                    Uri uri = new Uri(_activatedUri);
                    HomePage.HomePageLoadedEvent += () => ProcessUri(uri);
                }
                catch { }
            }
            MainWindow = new MainWindow();
            MainWindow.Show();
        }
        public static void BringToFront(Window window)
        {
            if (window == null) return;

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }
            window.Activate();
            window.Topmost = true;
            window.Topmost = false;
            window.Focus();
        }

        private void SingleInstanceManager_OtherInstanceStarted(string[] args)
        {
            BringToFront(MainWindow);
            foreach (string arg in args)
            {
                if (arg.StartsWith("mythkit://", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Uri uri = new Uri(arg);
                        ProcessUri(uri);
                    }
                    catch { }
                    break;
                }
            }
        }

        public void ProcessUri(Uri uri)
        {
            var paramsCollection = QueryStringHelper.ParseQueryString(uri.Query);
            var segments = GetPathSegments(uri);
            switch (uri.Host.ToLower())
            {
                case "attack":
#if LITE
                            break;
#endif
                    // mythkit://attack/recommend
                    if (segments.Count > 0 && segments[0].ToLower() == "recommend")
                    {
                        string targetIPs = paramsCollection["target"] ?? "";
                        string attackType = paramsCollection["type"];
                        var attackParams = DecodePrefixedStrings(paramsCollection["params"]).ToArray();
                        var N = HomePage.Instance.NavView;
                        N.SelectedItem = N.MenuItems[1];
                        N.Header = "UDP 重放";
                        HomePage.Instance.AppFrameNavigate(typeof(AttackIndex), null);
                        HomePage.NavPages.TryGetValue(typeof(AttackIndex), out object _index);
                        var index = _index as AttackIndex;
                        index.SwitchToType(attackType, attackParams);
                    }
                    break;
            }
        }
    }
}
