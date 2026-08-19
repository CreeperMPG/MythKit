using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MythKit.Pages.UDPAttack
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UDPAttackTypeAttribute : Attribute
    {
        private string _name;
        private string _packetConstructor; // 方法名
        private AttackTarget _attackTarget;
        public UDPAttackTypeAttribute(string name, string packetConstructor, AttackTarget attackTarget)
        { 
            _name = name;
            _packetConstructor = packetConstructor;
            _attackTarget = attackTarget;
        }
        public string Name => _name;
        public string PacketConstruct => _packetConstructor;
        public AttackTarget AttackTarget => _attackTarget;
    }
}
