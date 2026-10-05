using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MythKit.Pages.ReplayAttack
{
    public interface IAttackPattern
    {
        AttackPacket ConstructPacket(ref string errorMessage, IPAddress targetIP, int cycleId, int groupId);
        AttackTarget Target { get; }
        string AttackName { get; }
        string AttackId { get; }
        void Deserialize(Dictionary<string, object> serializedData);
        Dictionary<string, object> Serialize();
    }
}
