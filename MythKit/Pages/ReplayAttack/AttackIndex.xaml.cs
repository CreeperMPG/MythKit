using iNKORE.UI.WPF.Modern.Common.IconKeys;
using Microsoft.Win32;
using MythKit.Pages.ReplayAttack.AttackFunctions;
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Modern = iNKORE.UI.WPF.Modern.Controls;
using TeacherAttack = MythKit.Pages.ReplayAttack.AttackFunctions.TeacherAttack;

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
                int index = opt.AttackTypes.FindIndex(ati => ati.AttackName.Equals(element.GetProperty("attackName").GetString(), StringComparison.OrdinalIgnoreCase));
                if (index == -1)
                {
                    throw new InvalidOperationException("未找到对应的攻击名称。");
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
            StringFormattingDescription.Text = "只有部分输入框支持字符串格式化\n" +
                "格式化语法：\n" +
                "${ip} - IP 地址\n" +
                "${cycle} - 攻击轮数\n" +
                "${group} - 攻击组数";
            UIAttackOptions.Add(new UIAttackOption());
        }
        public static List<IAttackPattern> GetNewAttackPatterns()
        {
            List<IAttackPattern> result = new List<IAttackPattern>
            {
                new MessageAttack(),
                new CommandAttack(),
                new BlackScreenAttack(),
                new TeacherAttack.RaiseHandAttack()
            };
            return result;
        }
        private void InternetIPCollectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string localIPAddress = this.GetLocalIPAddress();
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

        private string GetLocalIPAddress()
        {
            string result = string.Empty;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint ipendPoint = socket.LocalEndPoint as IPEndPoint;
                result = ipendPoint.Address.ToString();
            }
            return result;
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
                            attackName = option.AttackTypes[option.PatternSelectedIndex].AttackName,
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
            catch(Exception ex)
            {
                new Modern.ContentDialog
                {
                    Title = "保存时出现错误",
                    Content = ex.Message,
                    CloseButtonText = "确定"
                }.ShowAsync();
            }
        }

        private void LoadAttackConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "加载攻击配置",
                    Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    DefaultExt = ".json",
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;   // 用户取消
                string jsonContent = File.ReadAllText(dialog.FileName);
                JsonDocument doc = JsonDocument.Parse(jsonContent);
                InjectParams(doc.RootElement);
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
        public string AttackName { get; set; }
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
            AttackTypes = AttackIndex.GetNewAttackPatterns().Select(pt => new AttackTypeItem { AttackName = pt.AttackName, Source = pt }).ToList();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
