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
    class AttackInfoLegacy
    {
        public object Function { get; set; }
        public Type FunctionType { get; set; }
        public UDPAttackTypeLegacyAttribute FunctionAttribute { get; set; }
    }
    /// <summary>
    /// AttackIndex.xaml 的交互逻辑
    /// </summary>
    public partial class AttackIndex : UserControl
    {
        private Dictionary<string, AttackInfoLegacy> AttackFunctionDictL = new Dictionary<string, AttackInfoLegacy>();
        public List<IAttackPattern> AttackTypes { get; set; } = new List<IAttackPattern>();
        public AttackIndex()
        {
            this.InitializeComponent();
            DataContext = this;
            // LEGACY-遍历程序集中的所有类型
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                bool flag = type.IsClass && !type.IsAbstract && Attribute.IsDefined(type, typeof(UDPAttackTypeLegacyAttribute));
                if (flag)
                {
                    object attackFunction = Activator.CreateInstance(type);
                    var attackTypes = type.GetCustomAttributes(typeof(UDPAttackTypeLegacyAttribute), false) as UDPAttackTypeLegacyAttribute[];
                    foreach (UDPAttackTypeLegacyAttribute attackType in attackTypes)
                    {
                        string nickname = attackType.Name;
                        if (type.Namespace.Contains("TeacherAttack"))
                        {
                            nickname = $"（教师端）{nickname}";
                        }
                        this.AttackFunctionDictL.Add(nickname, new AttackInfoLegacy() { Function = attackFunction, FunctionAttribute = attackType, FunctionType = type });
                        this.AttackTypeComboBoxL.Items.Add(nickname);
                    }
                }
            }
            this.AttackTypeComboBoxL.SelectedIndex = 0;
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
            AttackCommandTypeComboBox.SelectedIndex = 0;
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

        private void AttackTypeComboBoxL_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.OptionCardL.Content = this.AttackFunctionDictL[this.AttackTypeComboBoxL.SelectedValue as string].Function;
        }

        private void StartButton_Legacy_Click(object sender, RoutedEventArgs e)
        {
            AttackInfoLegacy info = this.AttackFunctionDictL[this.AttackTypeComboBoxL.SelectedValue as string];
            new AttackDialog(info.Function, info.FunctionType, info.FunctionAttribute, this).ShowAsync();
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
        }

        private void AttackCommandTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AttackArgumentsCard.Content = AttackTypes[AttackCommandTypeComboBox.SelectedIndex];
        }
    }
}
