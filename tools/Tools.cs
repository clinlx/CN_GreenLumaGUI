using System;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace CN_GreenLumaGUI.tools
{
    public class NetTools
    {
        //检查ip和端口
        public static bool IpRight(string ip)
        {
            return PingHost(ip, 1, 100, false) != -2;
        }
        public static bool PortRight(int port)
        {
            return port > 0 && port <= 65535;
        }
        /// <summary>
        /// 连接用的密钥
        /// </summary>
        public static bool AreKeyRecent(string key)
        {
            for (int i = -5; i <= 5; i++)
                if (GetCntKey(i) == key)
                    return true;
            return false;
        }
        public static string GetCntKey()
        {
            return GetCntKey(GetTimeMs());
        }
        public static string GetCntKey(int disSecond)
        {
            return GetCntKey(GetTimeMs() + disSecond * 1000);
        }
        public static string GetCntKey(long timeMs)
        {
            return MD5Tool.GetMD5_16("Birdry" + (timeMs / 1000) + "202cb962ac5975b964b7152d234b70");
        }
        /// <summary>
        /// Time for many ms
        /// </summary>
        public static long GetTimeMs()
        {
            return (DateTime.Now.Ticks / 10000);
        }
        /// <summary>
        /// PING
        /// </summary>
        public static long PingHost(string host, int times, bool echoLog = true)
        {
            return PingHost(host, times, 500, echoLog);
        }
        public static long PingHost(string host, int times, int pingTimeOut, bool echoLog = true)
        {
            return PingHost(host, times, out _, pingTimeOut, echoLog);
        }
        public static long PingHost(string host, int times, out string IP, int pingTimeOut, bool echoLog = true)
        {
            //Ping 实例对象;
            Ping pingSender = new();
            //ping选项;
            PingOptions options = new()
            {
                DontFragment = true
            };
            string data = "ping test data";
            byte[] buf = Encoding.ASCII.GetBytes(data);
            IP = "";
            bool canConnect = false;
            long sums = -1;
            int successTimes = 0;
            for (int i = 1; i <= times; i++)
            {
                if (echoLog)
                    Console.WriteLine($"尝试第{i}次......");
                //调用同步send方法发送数据，结果存入reply对象;
                try
                {
                    PingReply reply = pingSender.Send(host, pingTimeOut, buf, options);
                    if (reply.Status == IPStatus.Success)
                    {
                        successTimes++;
                        if (echoLog)
                            Console.WriteLine($"第{i}次成功！地址{reply.Address},延迟{reply.RoundtripTime}ms");
                        //主机地址reply.Address
                        //往返时间reply.RoundtripTime
                        //生存时间TTLreply.Options.Ttl
                        //缓冲区大小reply.Buffer.Length
                        //数据包是否分段reply.Options.DontFragment
                        if (canConnect == false)
                            IP = reply.Address.ToString();
                        canConnect = true;
                        sums += reply.RoundtripTime;
                    }
                    else
                    {
                        //sums += pingTimeOut;
                    }
                }
                catch
                {
                    return -2;//地址输入错误
                }
            }
            if (canConnect == false)
                return -1;//无法连接
            return sums / successTimes;
        }
    }
    public class Base64
    {
        /// <summary>
        /// Base64加密，采用utf8编码方式加密
        /// </summary>
        /// <param name="source">待加密的明文</param>
        /// <returns>加密后的字符串</returns>
        public static string Base64Encode(string source)
        {
            return Base64Encode(Encoding.UTF8, source);
        }

        /// <summary>
        /// Base64加密
        /// </summary>
        /// <param name="encodeType">加密采用的编码方式</param>
        /// <param name="source">待加密的明文</param>
        /// <returns></returns>
        public static string Base64Encode(Encoding encodeType, string source)
        {
            byte[] bytes = encodeType.GetBytes(source);
            string encode;
            try
            {
                encode = Convert.ToBase64String(bytes);
            }
            catch
            {
                encode = source;
            }
            return encode;
        }

        /// <summary>
        /// Base64解密，采用utf8编码方式解密
        /// </summary>
        /// <param name="result">待解密的密文</param>
        /// <returns>解密后的字符串</returns>
        public static string Base64Decode(string result)
        {
            return Base64Decode(Encoding.UTF8, result);
        }

        /// <summary>
        /// Base64解密
        /// </summary>
        /// <param name="encodeType">解密采用的编码方式，注意和加密时采用的方式一致</param>
        /// <param name="result">待解密的密文</param>
        /// <returns>解密后的字符串</returns>
        public static string Base64Decode(Encoding encodeType, string result)
        {
            byte[] bytes = Convert.FromBase64String(result);
            string decode;
            try
            {
                decode = encodeType.GetString(bytes);
            }
            catch
            {
                decode = result;
            }
            return decode;
        }
    }
    public class MD5Tool
    {
        public static String GetMD5_10(string str)
        {
            // 创建MD5对象
            // 需要将字符串转换成字节数组
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            // 返回一个加密好的字节数组
            byte[] MD5Buffer = MD5.HashData(buffer);

            // 将字节数组每个元素ToString()  10进制
            // 3244185981728979115075721453575112
            string s = "";
            foreach (var item in MD5Buffer)
            {
                s += item.ToString();
            }
            return s;
        }

        public static string GetMD5_16(string str)
        {
            // 创建MD5对象
            // 需要将字符串转换成字节数组
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            // 返回一个加密好的字节数组
            byte[] MD5Buffer = MD5.HashData(buffer);

            // 将字节数组每个元素ToString(x) 16进制
            //202cb962ac5975b964b7152d234b70
            string s2 = "";
            foreach (var item in MD5Buffer)
            {
                s2 += item.ToString("x");
            }
            return s2;
        }
        public static string GetMD5_16_x2(string str)
        {
            // 创建MD5对象
            // 需要将字符串转换成字节数组
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            // 返回一个加密好的字节数组
            byte[] MD5Buffer = MD5.HashData(buffer);

            // 将字节数组每个元素ToString(x) 16进制
            //202cb962ac5975b964b7152d234b70
            string s2 = "";
            foreach (var item in MD5Buffer)
            {
                s2 += item.ToString("x2");
            }
            return s2;
        }

    }
}
