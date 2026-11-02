using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MythKit.Utils
{
    public static class RegUtils
    {
        public static void EnsureRegistryKeyValue0OrNotExists(RegistryKey rootKey, string subKeyPath, string valueName)
        {
            using (var subKey = rootKey.OpenSubKey(subKeyPath, writable: false))
            {
                if (subKey != null)
                {
                    var value = subKey.GetValue(valueName);
                    if (value == null || (value is int intValue && intValue == 0))
                    {
                        return;
                    }
                }
            }
            using (var subKey = rootKey.OpenSubKey(subKeyPath, writable: true))
            {
                if (subKey != null)
                {
                    subKey.SetValue(valueName, 0, RegistryValueKind.DWord);
                }
            }
        }
        public static void EnsureRegValueZeroOrNotExists(string subKeyPath, string valueName)
        {
            EnsureRegistryKeyValue0OrNotExists(Registry.CurrentUser, subKeyPath, valueName);
            EnsureRegistryKeyValue0OrNotExists(Registry.LocalMachine, subKeyPath, valueName);
        }
        public static void WriteRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName, object targetValue, RegistryValueKind valueKind = RegistryValueKind.String)
        {
            try
            {
                using (RegistryKey registryKey = rootKey.OpenSubKey(subKeyPath, true))
                {
                    if (registryKey == null)
                    {
                        using (RegistryKey registryKey2 = rootKey.CreateSubKey(subKeyPath))
                        {
                            registryKey2.SetValue(valueName, RuntimeHelpers.GetObjectValue(targetValue), valueKind);
                            return;
                        }
                    }
                    registryKey.SetValue(valueName, RuntimeHelpers.GetObjectValue(targetValue), valueKind);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("写入注册表值时发生错误: " + ex.Message);
            }
        }
        public static void DeleteRegistryKey(RegistryKey registryKey, string path, string subKeyName, bool throwWhenNotExists = true)
        {
            try
            {
                RegistryKey subKey = registryKey.OpenSubKey(path, writable: true);
                subKey.DeleteSubKeyTree(subKeyName, throwOnMissingSubKey: false);
                subKey.DeleteValue(subKeyName, throwOnMissingValue: false);
            }
            catch (NullReferenceException nfe)
            {
                if (throwWhenNotExists)
                {
                    throw nfe;
                }
            }
        }
        public static object GetRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName, bool throwOnFailure = true)
        {
            try
            {
                using (RegistryKey registryKey = rootKey.OpenSubKey(subKeyPath))
                {
                    return registryKey.GetValue(valueName);
                }
            }
            catch (Exception ex)
            {
                if (throwOnFailure)
                {
                    throw new Exception("读取注册表值时发生错误: " + ex.Message);
                }
                return null;
            }
        }
    }
}
