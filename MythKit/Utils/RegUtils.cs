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
        public static object GetRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName)
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
                throw new Exception("读取注册表值时发生错误: " + ex.Message);
            }
        }
    }
}
