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
        /// <summary>
        /// 用于ini格式配置中，写在等号左边的“用于替换的app”列表。
        /// 现在从配置txt(默认池+拓展池)中读取，不再扫描dll。
        /// </summary>
        public static long[]? ReadAppList(string target = "default")
        {
            if (!DataSystem.Instance.SingleConfigFileMode) return null;
            var list = AppPoolSystem.Instance.GetAvailableList();
            if (list.Count == 0)
            {
                OutAPI.PrintLog("App pool is empty.");
                return null;
            }
            return list.ToArray();
        }
        public static int TotalMaxUnlockNum => GLFileTools.GetTotalMaxUnlockNum(); //解锁上限，等于可用app池的长度
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
            int totalMaxUnlockNum = TotalMaxUnlockNum;
            int maxPos = data.Length - totalMaxUnlockNum * intSize;

            for (int i = 0; i <= maxPos; i++)
            {
                if (IsMatch(data, i, prePattern))
                {
                    byte[] arrayBytes = new byte[totalMaxUnlockNum * intSize];
                    Array.Copy(data, i + preNum, arrayBytes, 0, arrayBytes.Length);
                    // 转为 int 数组
                    List<int> intArray = new(totalMaxUnlockNum + 5);
                    for (int k = 0; k < totalMaxUnlockNum; k++)
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
