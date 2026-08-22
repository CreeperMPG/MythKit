using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MythKit.Pages.UDPAttack
{
    public interface IAttackType
    {
        AttackPacket ConstructPacket(ref string message, string ip, int cycleCount, int groupCount);
        AttackTarget Target { get; }
        string AttackName { get; }
    }
}
