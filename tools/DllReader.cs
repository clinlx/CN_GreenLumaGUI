using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CN_GreenLumaGUI.tools
{
    class DllReader
    {
        private static int[]? defaultCache = null;
        private static string outsideDllKey = "";
        private static int[]? outsideDllCache = null;
        public static int[]? ReadAppList(string target = "default")
        {
            if (!DataSystem.Instance.SingleConfigFileMode) return null;
            int[]? data = null;
            if (target == "default")
            {
                if (defaultCache is not null)
                {
                    return defaultCache;
                }
                var str = OutAPI.GetFromRes("DLLInjector.GreenLuma.dll.b64");
                if (string.IsNullOrEmpty(str))
                {
                    OutAPI.PrintLog("Fail to read app list from dll.(GreenLuma.dll.b64 not found)");
                    return null;
                }
                data = ReadAppListFromByte(Convert.FromBase64String(str));
                if (data == null)
                {
                    _ = OutAPI.MsgBox(LocalizationService.GetString("Dll_ReadInlayFailed"));
                    return null;
                }
                defaultCache = data;
            }
            else
            {
                try
                {
                    FileInfo fileInfo = new(target);
                    if (!fileInfo.Exists)
                    {
                        OutAPI.PrintLog("Fail to read app list from dll.(Target DLL not found)");
                        return null;
                    }
                    var cacheKey = $"{target}|{fileInfo.Length}";
                    if (outsideDllKey == cacheKey && outsideDllCache is not null)
                    {
                        return outsideDllCache;
                    }
                    data = ReadAppListFromByte(File.ReadAllBytes(target));
                    if (data == null)
                    {
                        _ = OutAPI.MsgBox(LocalizationService.GetString("Dll_ReadOutsideFailed"));
                        return null;
                    }
                    outsideDllKey = cacheKey;
                    outsideDllCache = data;
                }
                catch (Exception e)
                {
                    _ = OutAPI.MsgBox($"Fail to read app list from dll.({e.Message})");
                    return null;
                }
            }
            return data;
        }
        public const int TotalMaxUnlockNum = 135; //GreenLuma最大支持到135的上限
        private const int intSize = 4;
        private const int preNum = 0;
        private static readonly byte[] prePattern =
        {
            0x5A,0x00,0x00,0x00,
            0xCD,0x00,0x00,0x00,
            0xDB,0x00,0x00,0x00,
            0x36,0x01,0x00,0x00,
        };
        private static int[]? ReadAppListFromByte(byte[] data)
        {
            int maxPos = data.Length - TotalMaxUnlockNum * intSize;

            for (int i = 0; i <= maxPos; i++)
            {
                if (IsMatch(data, i, prePattern))
                {
                    byte[] arrayBytes = new byte[TotalMaxUnlockNum * intSize];
                    Array.Copy(data, i + preNum, arrayBytes, 0, arrayBytes.Length);
                    // 转为 int 数组
                    List<int> intArray = new(TotalMaxUnlockNum + 5);
                    for (int k = 0; k < TotalMaxUnlockNum; k++)
                    {
                        int val = BitConverter.ToInt32(arrayBytes, k * intSize);
                        if (val <= 1) break;
                        intArray.Add(val);
                        // OutAPI.MsgBox($"{val}({k})");
                    }
                    return intArray.ToArray();
                }
            }
            return null;
        }

        private static bool IsMatch(byte[] data, int pos, byte[] pattern)
        {
            if (pos + pattern.Length > data.Length) return false;
            for (int i = 0; i < pattern.Length; i++)
            {
                if (data[pos + i] != pattern[i])
                    return false;
            }
            return true;
        }
    }
}
