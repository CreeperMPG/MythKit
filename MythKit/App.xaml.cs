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
        public const string VERSION = "1.2.1";
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
        public static TaskManager TaskManagerInstance { get; } = new TaskManager(); private string _activatedUri = null;
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

            string[] args = Environment.GetCommandLineArgs();

            foreach (string arg in args)
            {
                if (arg.StartsWith("mythkit://", StringComparison.OrdinalIgnoreCase))
                {
                    _activatedUri = arg;
                    break;
                }
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();

            if (!string.IsNullOrEmpty(_activatedUri))
            {
                try
                {
                    Uri uri = new Uri(_activatedUri);
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
                                Modern.ContentDialog dialog = new Modern.ContentDialog
                                {
                                    Title = "外部应用提供的攻击参数",
                                    Content = new AttackIndex(attackType, attackParams),
                                    CloseButtonText = "取消",
                                    MinWidth = 720
                                };
                                dialog.ShowAsync(mainWindow);
                            }
                            break;
                    }
                }
                catch { }
            }
        }
    }
}
