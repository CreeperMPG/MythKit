using MythKit.Pages.Home;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace MythKit.Pages.UDPAttack
{
    class AttackFunctionInfo
    {
        public object Function { get; set; }
        public Type FunctionType { get; set; }
        public UDPAttackTypeAttribute FunctionAttribute { get; set; }
    }
    /// <summary>
    /// AttackIndex.xaml 的交互逻辑
    /// </summary>
    public partial class AttackIndex : UserControl
    {// MythKit.Pages.UDPAttack.AttackIndex
     // Token: 0x04000044 RID: 68
        private Dictionary<string, AttackFunctionInfo> AttackFunctionDict = new Dictionary<string, AttackFunctionInfo>();
        // Token: 0x0600004C RID: 76 RVA: 0x00003D0C File Offset: 0x00001F0C
        public AttackIndex()
        {
            this.InitializeComponent();
            // 遍历程序集中的所有类型
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                bool flag = type.IsClass && !type.IsAbstract && Attribute.IsDefined(type, typeof(UDPAttackTypeAttribute));
                if (flag)
                {
                    object attackFunction = Activator.CreateInstance(type);
                    var attackTypes = type.GetCustomAttributes(typeof(UDPAttackTypeAttribute), false) as UDPAttackTypeAttribute[];
                    foreach (UDPAttackTypeAttribute attackType in attackTypes)
                    {
                        string nickname = attackType.Name;
                        if (type.Namespace.Contains("TeacherAttack"))
                        {
                            nickname = $"（教师端）{nickname}";
                        }
                        this.AttackFunctionDict.Add(nickname, new AttackFunctionInfo() { Function = attackFunction, FunctionAttribute = attackType, FunctionType = type });
                        this.AttackTypeComboBox.Items.Add(nickname);
                    }
                }
            }
            this.AttackTypeComboBox.SelectedIndex = 0;
            StringFormattingDescription.Text = "只有部分输入框支持字符串格式化\n" +
                "格式化语法：\n" +
                "\t${ip} - IP 地址\n" +
                "\t${cycle} - 攻击轮数\n" +
                "\t${group} - 攻击组数";

        }
        // Token: 0x0600004D RID: 77 RVA: 0x00003DC4 File Offset: 0x00001FC4
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

        // Token: 0x0600004E RID: 78 RVA: 0x00003EAC File Offset: 0x000020AC
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

        // Token: 0x0600004F RID: 79 RVA: 0x00003F18 File Offset: 0x00002118
        private void TargetIP_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.TargetIPAddress.MaxWidth = e.NewSize.Width - 300.0;
        }

        // Token: 0x06000050 RID: 80 RVA: 0x00003F4A File Offset: 0x0000214A
        private void AttackTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.OptionCard.Content = this.AttackFunctionDict[this.AttackTypeComboBox.SelectedValue as string].Function;
        }

        // Token: 0x06000051 RID: 81 RVA: 0x00003F74 File Offset: 0x00002174
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            AttackFunctionInfo info = this.AttackFunctionDict[this.AttackTypeComboBox.SelectedValue as string];
            new AttackDialog(info.Function, info.FunctionType, info.FunctionAttribute, this).ShowAsync();
        }

    }
}
