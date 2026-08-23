using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MythKit.Pages.UDPAttack
{
    public struct IPGroupConfig
    {
        public int SingleGroupSize { get; set; }
        public int GroupIntevalMilliseconds { get; set; }
    }
    public class AttackConfig
    {
        public List<IPAddress> TargetIPs { get; set; } = new List<IPAddress>();
        public IPGroupConfig? GroupConfig { get; set; }
        public int CycleIntervalMilliseconds { get; set; } = 1000;
        public int? TotalCycles { get; set; } // Infinity when null
        public IAttackPattern AttackPattern { get; set; }
    }
}
