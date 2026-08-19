using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MythKit.Pages.Home
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class SuggestionAttribute : Attribute
    {
        private string _name;
        private string _description;
        private string _isEnabled; // 方法名
        private string _buttonText;
        private string _buttonCallback; // 方法名
        public SuggestionAttribute(string name, string description, string isEnabled, string buttonText, string buttonCallback)
        {
            _name = name;
            _description = description;
            _buttonText = buttonText;
            _isEnabled = isEnabled;
            _buttonCallback = buttonCallback;
        }
        public string Name => _name;
        public string Description => _description;
        public string IsEnabled => _isEnabled;
        public string ButtonText => _buttonText;
        public string ButtonCallback => _buttonCallback;
    }
}
