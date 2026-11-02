using iNKORE.UI.WPF.Modern.Common.IconKeys;
using Microsoft.Win32;
using MythKit.Pages.ReplayAttack.AttackPatterns;
using MythKit.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Modern = iNKORE.UI.WPF.Modern.Controls;
using TeacherAttack = MythKit.Pages.ReplayAttack.AttackPatterns.TeacherAttack;

namespace MythKit.Pages.ReplayAttack
{
    /// <summary>
    /// AttackIndex.xaml 的交互逻辑
    /// </summary>
    public partial class AttackIndex : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public List<IAttackPattern> DefaultAttackTypes { get; set; } = new List<IAttackPattern>();
        private ObservableCollection<UIAttackOption> _uiAttackOptions = new ObservableCollection<UIAttackOption>();
        public ObservableCollection<UIAttackOption> UIAttackOptions
        {
            get => _uiAttackOptions;
            set
            {
                _uiAttackOptions = value;
                OnPropertyChanged();
            }
        }
        public AttackIndex()
        {
            InitializeComponent();
            InitializePage();
        }
        public void InjectParams(JsonElement args)
        {
            ObservableCollection<UIAttackOption> options = new ObservableCollection<UIAttackOption>();
            foreach (var element in args.EnumerateArray())
            {
                UIAttackOption opt = new UIAttackOption();
                int index = opt.AttackTypes.FindIndex(ati => ati.Source.AttackId.Equals(element.GetProperty("attackId").GetString(), StringComparison.OrdinalIgnoreCase));
                if (index == -1)
                {
                    throw new InvalidOperationException("未找到对应的攻击 ID。");
                }
                opt.PatternSelectedIndex = index;
                opt.DelayMs = element.TryGetProperty("delayMs", out JsonElement delayEl) && delayEl.ValueKind == JsonValueKind.Number ? delayEl.GetInt32() : 0;
                opt.Enabled = element.TryGetProperty("enabled", out JsonElement enabledEl) ? enabledEl.GetBoolean() : false;
                opt.AttackTypes[index].Source.Deserialize(JSONUtils.ToDictionary(element.GetProperty("content")));
                options.Add(opt);
            }
            UIAttackOptions = options;
        }
        private void InitializePage()
        {
            DataContext = this;
            UIAttackOptions.Add(new UIAttackOption());
        }
        public static List<IAttackPattern> GetNewAttackPatterns()
        {
            List<IAttackPattern> result = new List<IAttackPattern>
            {
                new MessageAttack(),
                new CommandAttack(),
                new PowerAttack(),
                new TeacherAttack.RaiseHandAttack(),
                new TeacherAttack.HeapOverflowAttack()
            };
            return result;
        }
        private void InternetIPCollectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string localIPAddress = GetLocalIPAddress().ToString();
                bool flag = string.IsNullOrEmpty(localIPAddress);
                if (flag)
                {
                    MessageBox.Show("无法获取本机 IP 地址", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
                }
                else
                {
                    string arg = string.Join(".", localIPAddress.Split(new char[]
                    {
                        '.'
                    }).Take(3));
                    List<string> list = new List<string>();
                    for (int i = 1; i <= 255; i++)
                    {
                        list.Add(string.Format("{0}.{1}", arg, i));
                    }
                    this.TargetIPAddress.Text = string.Join(",", list);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("发生错误: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
        }

        public static IPAddress GetLocalIPAddress()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP);
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint ipendPoint = socket.LocalEndPoint as IPEndPoint;
            return ipendPoint.Address;
        }

        private void TargetIP_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.TargetIPAddress.MaxWidth = e.NewSize.Width - 300.0;
        }
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> ipAddresses = new List<string>();
            foreach (string ip in TargetIPAddress.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                ipAddresses.Add(ip.Trim());
            }
            if (ipAddresses.Count == 0)
            {
                Modern.ContentDialog dialog = new Modern.ContentDialog()
                {
                    Title = "错误",
                    Content = "请至少输入一个有效目标 IP 地址",
                    CloseButtonText = "确定"
                };
                dialog.ShowAsync();
                return;
            }
            AttackConfig config = new AttackConfig()
            {
                TargetIPs = ipAddresses.Select(ip => IPAddress.Parse(ip)).ToList(),
                AttackContent = UIAttackOptions.Where(opt => opt.Enabled).Select(opt => opt.ToSingleAttackPack()).ToList(),
                CycleIntervalMilliseconds = (int)(IntervalSeconds.Value * 1000),
                TotalCycles = EnableInterval.IsChecked ?? false ? NotInfiniteSwitch.IsChecked ?? false ? (int)IntervalTimes.Value : (int?)null : 1
            };
            if (EnableGroupIP.IsChecked ?? false)
            {
                config.GroupConfig = new IPGroupConfig()
                {
                    SingleGroupSize = (int)GroupIPNumber.Value,
                    GroupIntevalMilliseconds = (int)(GroupInterval.Value * 1000)
                };
            }
            AttackManagedTask task = new AttackManagedTask(config);
            App.TaskManagerInstance.StartTask(task);
            var stateStackPanel = new iNKORE.UI.WPF.Controls.SimpleStackPanel()
            {
                Children =
                    {
                        new TextBlock()
                        {
                            Text = "攻击已开始。最小化后任务仍会继续运行，你可以在任务管理中查看。",
                            Margin = new Thickness(0, 0, 0, 10),
                            TextWrapping = TextWrapping.Wrap
                        },
                        task.DetailContentView
                    }
            };
            Modern.ContentDialog attackDialog = new Modern.ContentDialog()
            {
                Title = task.TaskName,
                Content = stateStackPanel,
                CloseButtonText = "最小化到后台"
            };
            attackDialog.Closed += (s, args) =>
            {
                stateStackPanel.Children.Remove(task.DetailContentView);
            };
            attackDialog.ShowAsync();
            return;
        }
        public static int MythwareDefaultPort = 4705;

        private void TDDefaultPort_ValueChanged(Modern.NumberBox sender, Modern.NumberBoxValueChangedEventArgs args)
        {
            MythwareDefaultPort = (int)args.NewValue;
        }

        private void AddAttackButton_Click(object sender, RoutedEventArgs e)
        {
            UIAttackOptions.Add(new UIAttackOption());
        }
        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe) || !(fe.DataContext is UIAttackOption option))
                return;

            int index = UIAttackOptions.IndexOf(option);
            if (index <= 0)
                return;

            UIAttackOptions.RemoveAt(index);
            UIAttackOptions.Insert(index - 1, option);
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe) || !(fe.DataContext is UIAttackOption option))
                return;

            int index = UIAttackOptions.IndexOf(option);
            if (index < 0 || index >= UIAttackOptions.Count - 1)
                return;

            UIAttackOptions.RemoveAt(index);
            UIAttackOptions.Insert(index + 1, option);
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe) || !(fe.DataContext is UIAttackOption option))
                return;

            if (UIAttackOptions.Count > 1)
            {
                UIAttackOptions.Remove(option);
            }
        }

        private void SaveAttackConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<object> attackConfigList = new List<object>();
                foreach (var option in UIAttackOptions)
                {
                    if (option.AttackTypes[option.PatternSelectedIndex].Source is IAttackPattern pattern)
                    {
                        Dictionary<string, object> serializedData = pattern.Serialize();
                        object data = new
                        {
                            attackId = option.AttackTypes[option.PatternSelectedIndex].Source.AttackId,
                            delayMs = option.DelayMs,
                            enabled = option.Enabled,
                            content = serializedData,
                        };
                        attackConfigList.Add(data);
                    }
                }
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var dialog = new SaveFileDialog
                {
                    Title = "保存攻击配置",
                    Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    DefaultExt = ".json",
                    FileName = "attack_config.json",
                    AddExtension = true,
                    OverwritePrompt = true
                };
                if (dialog.ShowDialog() != true) return;   // 用户取消
                var fs = new FileStream(
                       dialog.FileName,
                       FileMode.Create,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       useAsync: true);

                JsonSerializer.SerializeAsync(fs, attackConfigList, options).Wait();
                fs.Close();
                new Modern.ContentDialog
                {
                    Title = "保存成功",
                    Content = "攻击配置已成功保存。",
                    CloseButtonText = "确定"
                }.ShowAsync();
            }
            catch (Exception ex)
            {
                new Modern.ContentDialog
                {
                    Title = "保存时出现错误",
                    Content = ex.Message,
                    CloseButtonText = "确定"
                }.ShowAsync();
            }
        }
        private List<UIAttackOption> ParseLRCToAttackConfig(string lrcContent)
        {
            List<UIAttackOption> attackOptionList = new List<UIAttackOption>();
            var regex = new Regex(@"\[(\d{1,3}):(\d{1,2})(?:[.:](\d{1,3}))?\]");
            var lines = new List<(TimeSpan Time, string Text)>();
            foreach (var raw in lrcContent.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var matches = regex.Matches(line);
                if (matches.Count == 0) continue;
                var last = matches[matches.Count - 1];
                string text = line.Substring(last.Index + last.Length).Trim();
                if (text.Length == 0) continue;
                foreach (Match m in matches)
                {
                    int mm = int.Parse(m.Groups[1].Value);
                    int ss = int.Parse(m.Groups[2].Value);
                    string xx = m.Groups[3].Value;
                    int ms;
                    switch (xx.Length)
                    {
                        case 1: ms = int.Parse(xx) * 100; break;     // [mm:ss.x]
                        case 2: ms = int.Parse(xx) * 10; break;     // [mm:ss.xx]
                        case 0: ms = 0; break;      // [mm:ss]
                        default: ms = int.Parse(xx.Substring(0, 3)); break; // [mm:ss.xxx]
                    }
                    lines.Add((new TimeSpan(0, 0, mm, ss, ms), text));
                }
            }
            (TimeSpan Time, string Text) lastItem = (new TimeSpan(), "");
            (TimeSpan Time, string Text) currentItem = (new TimeSpan(), "");
            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                lastItem = currentItem;
                currentItem = lines.OrderBy(l => l.Time).ToArray()[lineIndex];
                var attack = new MessageAttack();
                var opt = new UIAttackOption();
                attack.Message.Text = currentItem.Text;
                int index = opt.AttackTypes.FindIndex(ati => ati.Source.AttackId.Equals("send_message", StringComparison.OrdinalIgnoreCase));
                opt.PatternSelectedIndex = index;
                opt.DelayMs = (int)(currentItem.Time.TotalMilliseconds - lastItem.Time.TotalMilliseconds);
                opt.AttackTypes[index].Source = attack;
                attackOptionList.Add(opt);
            }
            return attackOptionList;
        }

        private void LoadAttackConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "加载攻击配置",
                    Filter = "JSON 文件 (*.json)|*.json|歌词文件 (*.lrc)|*.lrc|所有文件 (*.*)|*.*",
                    DefaultExt = ".json",
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;   // 用户取消
                if (Path.GetExtension(dialog.FileName) == ".json")
                {
                    string jsonContent = File.ReadAllText(dialog.FileName);
                    JsonDocument doc = JsonDocument.Parse(jsonContent);
                    InjectParams(doc.RootElement);
                }
                else if (Path.GetExtension(dialog.FileName) == ".lrc")
                {
                    string lrcContent = File.ReadAllText(dialog.FileName);
                    ObservableCollection<UIAttackOption> options = new ObservableCollection<UIAttackOption>();
                    foreach (var opt in ParseLRCToAttackConfig(lrcContent))
                    {
                        options.Add(opt);
                    }
                    UIAttackOptions = options;
                }
                new Modern.ContentDialog
                {
                    Title = "加载成功",
                    Content = "攻击配置已成功加载。",
                    CloseButtonText = "确定"
                }.ShowAsync();
            }
            catch (Exception ex)
            {
                new Modern.ContentDialog
                {
                    Title = "加载时出现错误",
                    Content = ex.Message,
                    CloseButtonText = "确定"
                }.ShowAsync();
            }
        }
    }
    public class AttackTypeItem
    {
        public string AttackName { get => (Source.Target == AttackTarget.Teacher ? "(教师端) " : "") + Source.AttackName; }
        public IAttackPattern Source { get; set; }   // 原对象
    }
    public class UIAttackOption : INotifyPropertyChanged
    {
        private int _patternSelectedIndex = 0;
        private int _delayMs;
        private bool _enabled;
        public int PatternSelectedIndex
        {
            get => _patternSelectedIndex;
            set
            {
                _patternSelectedIndex = value;
                OnPropertyChanged();
            }
        }
        public int DelayMs
        {
            get => _delayMs;
            set
            {
                _delayMs = value;
                OnPropertyChanged();
            }
        }
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }
        public List<AttackTypeItem> AttackTypes { get; private set; }
        public SingleAttackPack ToSingleAttackPack()
        {
            return new SingleAttackPack()
            {
                AttackPattern = AttackTypes[PatternSelectedIndex].Source,
                DelayMilliseconds = DelayMs
            };
        }
        public UIAttackOption()
        {
            Enabled = true;
            AttackTypes = AttackIndex.GetNewAttackPatterns().Select(pt => new AttackTypeItem { Source = pt }).ToList();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
