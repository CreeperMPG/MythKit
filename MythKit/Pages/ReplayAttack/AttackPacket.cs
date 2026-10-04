using System.Collections.Generic;

namespace MythKit.Pages.ReplayAttack
{
    public class AttackPacket
    {
        public List<byte[]> AttackPackets;
        public int TargetPort;
        public int IntervalMiliseconds;
        public AttackPacket(List<byte[]> attackPackets, int targetPort = -1, int intervalMiliseconds = 0)
        {
            if (targetPort == -1)
            {
                targetPort = AttackIndex.MythwareDefaultPort;
            }
            AttackPackets = attackPackets;
            TargetPort = targetPort;
            IntervalMiliseconds = intervalMiliseconds;
        }
        public static string FormatString(string str, string ip, int cycleCount, int groupCount)
        {
            return str.Replace("${ip}", ip).Replace("${cycle}", cycleCount.ToString()).Replace("${group}", groupCount.ToString());
        }
    }
}
