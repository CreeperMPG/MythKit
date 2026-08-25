using MythKit.Pages.Home;
using MythKit.Pages.UDPAttack.AttackFunctions;
using TeacherAttack = MythKit.Pages.UDPAttack.AttackFunctions.TeacherAttack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Modern = iNKORE.UI.WPF.Modern.Controls;

namespace MythKit.Pages.UDPAttack
{
    /// <summary>
    /// AttackIndex.xaml 的交互逻辑
    /// </summary>
    public partial class AttackIndex : UserControl
    {
        public List<IAttackPattern> AttackTypes { get; set; } = new List<IAttackPattern>();
        public AttackIndex(string defaultType = null, string[] typeArguments = null)
        {
            InitializeComponent();
            InitializePage(defaultType, typeArguments);
        }
        public AttackIndex()
        {
            InitializeComponent();
            InitializePage();
        }
        private void InitializePage(string defaultType = null, string[] typeArguments = null)
        {
            DataContext = this;
            StringFormattingDescription.Text = "只有部分输入框支持字符串格式化\n" +
                "格式化语法：\n" +
                "\t${ip} - IP 地址\n" +
                "\t${cycle} - 攻击轮数\n" +
                "\t${group} - 攻击组数";
            // 注册攻击类型
            AttackTypes.Add(new MessageAttack());
            AttackTypes.Add(new CommandAttack());
            AttackTypes.Add(new BlackScreenAttack());
            AttackTypes.Add(new TeacherAttack.RaiseHandAttack());
            AttackCommandTypeComboBox.ItemsSource = AttackTypes.Select((item) => item.AttackName);
            if (defaultType != null)
            {
                int index = AttackTypes.FindIndex((item) => item.AttackId == defaultType);
                if (index >= 0)
                {
                    if (typeArguments != null && typeArguments.Length > 0)
                    {
                        IAttackPattern attackPattern = AttackTypes[index];
                        Type patternType = attackPattern.GetType();
                        ConstructorInfo constructor = patternType.GetConstructors().FirstOrDefault();
                        if (constructor != null)
                        {
                            try
                            {
                                object instance = constructor.Invoke(typeArguments);
                                AttackTypes[index] = (IAttackPattern)instance;
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    index = 0;
                }
                AttackCommandTypeComboBox.SelectedIndex = index;
            }
            else
            {
                AttackCommandTypeComboBox.SelectedIndex = 0;
            }
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
                AttackPattern = AttackTypes[AttackCommandTypeComboBox.SelectedIndex],
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

        private void AttackCommandTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AttackArgumentsCard.Content = AttackTypes[AttackCommandTypeComboBox.SelectedIndex];
        }
    }
}
