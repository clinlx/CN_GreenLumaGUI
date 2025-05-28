using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CN_GreenLumaGUI.tools
{
	public static partial class OutAPI
	{
		private static partial class NativeMethods
		{
			internal const uint GW_OWNER = 4;

			internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

			[LibraryImport("User32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

			[LibraryImport("User32.dll")]
			internal static partial int GetWindowThreadProcessId(IntPtr hWnd, out IntPtr lpdwProcessId);

			[LibraryImport("User32.dll")]
			internal static partial IntPtr GetWindow(IntPtr hWnd, uint uCmd);

			[LibraryImport("User32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static partial bool IsWindowVisible(IntPtr hWnd);
		}
		[DllImport("User32.dll")]//根据句柄名称返回一个句柄
		public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
		/// <summary>
		/// 该函数将创建指定窗口的线程设置到前台，并且激活该窗口。键盘输入转向该窗口，并为用户改各种可视的记号。系统给创建前台窗口的线程分配的权限稍高于其他线程。
		/// </summary>
		/// <param name="hWnd">将被激活并被调入前台的窗口句柄。</param>
		/// <returns>如果窗口设入了前台，返回值为非零；如果窗口未被设入前台，返回值为零。</returns>
		[LibraryImport("User32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static partial bool SetForegroundWindow(IntPtr hWnd);

		public static IntPtr GetMainWindowHandle(nint processId)
		{
			IntPtr MainWindowHandle = IntPtr.Zero;

			NativeMethods.EnumWindows(new NativeMethods.EnumWindowsProc((hWnd, lParam) =>
			{
				_ = NativeMethods.GetWindowThreadProcessId(hWnd, out nint PID);
				if (PID == lParam &&
					NativeMethods.IsWindowVisible(hWnd) &&
					NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER) == IntPtr.Zero)
				{
					MainWindowHandle = hWnd;
					return false;
				}

				return true;

			}), new IntPtr(processId));

			return MainWindowHandle;
		}

		public const string TempDir = "C:\\tmp\\exewim2oav.addy.vlz";

		public const string LogFilePath = $"{OutAPI.TempDir}\\log0.txt";
		public static string SystemTempDir => Path.Combine(Path.GetTempPath(), "exewim2oav.addy.vlz");

		//public static void MsgBox(string text, string? title = null)
		//{
		//	PrintLog("msg:" + text);
		//	if (title is null)
		//		MessageBox.Show(text);
		//	else
		//		MessageBox.Show(text, title);
		//}

		public static async Task MsgBox(string text, string? title = null)
		{
			await Task.Delay(100);
			PrintLog("msg:" + text);
			await Task.Run(() =>
			{
				if (title is null)
					MessageBox.Show(text);
				else
					MessageBox.Show(text, title);
			});
		}
		public static void PrintLog(string? text)
		{
			if (text is null || string.IsNullOrEmpty(text.Trim()))
			{
				return;
			}
			try
			{
				var writer = File.AppendText(LogFilePath);
				string contain = $"\r\n[{DateTime.Now}]" +
					"\r\n" + text.Trim() + "\r\n";
				writer.Write(contain);
				Console.Write(contain);
				writer.Close();
			}
			catch
			{

			}
		}
		public static string? GetFromRes(string fileName)
		{
			string resourcefile = $"CN_GreenLumaGUI.{fileName}";
			//从资源读取
			Assembly assm = Assembly.GetExecutingAssembly();
			Stream? istr = assm.GetManifestResourceStream(resourcefile);
			if (istr is null) return null;
			StreamReader sr = new(istr, Encoding.UTF8);
			string? str = sr.ReadToEnd();
			sr.Close();
			istr.Close();
			return str;
        }
        public static byte[]? GetByteFromRes(string fileName)
        {
            string resourcefile = $"CN_GreenLumaGUI.{fileName}";
            //从资源读取
            Assembly assm = Assembly.GetExecutingAssembly();
            Stream? istr = assm.GetManifestResourceStream(resourcefile);
            if (istr is null) return null;
            byte[] b = new byte[istr.Length];
            istr.Read(b, 0, b.Length);
            istr.Close();
            return b;
        }
        public static string? CreateByRes(string targetFile, string fileName, bool replace = false)
		{
			string resourcefile = $"CN_GreenLumaGUI.{fileName}";
			bool needCreate = true;
			if (File.Exists(targetFile) && !replace)
			{
				needCreate = false;
			}
			if (needCreate)
			{
				//从资源读取
				Assembly assm = Assembly.GetExecutingAssembly();
				Stream? istr = assm.GetManifestResourceStream(resourcefile);
				if (istr is null) return null;
				byte[] b = new byte[istr.Length];
				istr.Read(b, 0, b.Length);
				istr.Close();
				//写入
				File.WriteAllBytes(targetFile, b);
			}
			return targetFile;
        }
        public static string? CreateByB64(string targetFile, string b64FileName, bool replace = false)
		{
			string resourcefile = $"CN_GreenLumaGUI.DLLInjector.{b64FileName}.b64";
			//string targetFile = $"{dir}\\{fileName}";
			bool needCreate = true;
			if (File.Exists(targetFile) && !replace)
			{
				needCreate = false;
			}
			if (needCreate)
			{
				//从资源读取
				Assembly assm = Assembly.GetExecutingAssembly();
				Stream? istr = assm.GetManifestResourceStream(resourcefile);
				if (istr is null) return null;
				System.IO.StreamReader sr = new(istr);
				string str = sr.ReadToEnd();
				//写入exe
				File.WriteAllBytes(targetFile, Convert.FromBase64String(str));
			}
			return targetFile;
		}
		public static void TempClear()
		{
			try
			{
				Directory.Delete(TempDir, true);
			}
			catch
			{

			}
		}
		public static void TempClear(string fileName)
		{
			string targetFile = $"{TempDir}\\{fileName}";
			if (File.Exists(targetFile))
			{
				File.Delete(targetFile);
			}
		}

		public static void OpenInBrowser(string url)
		{
			using Process p = new();
			string cmd = $"start {url}&exit";
			p.StartInfo.FileName = "cmd.exe";
			p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启动
			p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
			p.StartInfo.CreateNoWindow = true;          //不显示程序窗口
			p.Start();//启动程序
			p.StandardInput.WriteLine(cmd);//向cmd窗口写入命令
			p.StandardInput.AutoFlush = true;
			p.WaitForExit();//等待程序执行完退出进程
			p.Close();
		}

		/// <summary>
		/// 为文件添加users，everyone用户组的完全控制权限
		/// </summary>
		/// <param name="filePath"></param>
		public static void AddSecurityControll2File(string filePath, bool echoLog = true)
		{
			try
			{
				//获取文件信息
				FileInfo fileInfo = new(filePath);
				//获得该文件的访问权限
				System.Security.AccessControl.FileSecurity fileSecurity = fileInfo.GetAccessControl();
				//添加ereryone用户组的访问权限规则 完全控制权限
				fileSecurity.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, AccessControlType.Allow));
				//添加Users用户组的访问权限规则 完全控制权限
				fileSecurity.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, AccessControlType.Allow));
				//设置访问权限
				fileInfo.SetAccessControl(fileSecurity);
			}
			catch (Exception e)
			{
				if (echoLog)
					OutAPI.PrintLog(e.Message);
				if (e.StackTrace is not null)
					if (echoLog)
						OutAPI.PrintLog(e.StackTrace);
			}
		}

		/// <summary>
		///为文件夹添加users，everyone用户组的完全控制权限
		/// </summary>
		/// <param name="dirPath"></param>
		public static void AddSecurityControll2Folder(string dirPath)
		{
			try
			{
				//获取文件夹信息
				DirectoryInfo dir = new(dirPath);
				//获得该文件夹的所有访问权限
				System.Security.AccessControl.DirectorySecurity dirSecurity = dir.GetAccessControl(AccessControlSections.All);
				//设定文件ACL继承
				InheritanceFlags inherits = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
				//添加ereryone用户组的访问权限规则 完全控制权限
				FileSystemAccessRule everyoneFileSystemAccessRule = new("Everyone", FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow);
				//添加Users用户组的访问权限规则 完全控制权限
				FileSystemAccessRule usersFileSystemAccessRule = new("Users", FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow);
				dirSecurity.ModifyAccessRule(AccessControlModification.Add, everyoneFileSystemAccessRule, out _);
				dirSecurity.ModifyAccessRule(AccessControlModification.Add, usersFileSystemAccessRule, out _);
				//设置访问权限
				dir.SetAccessControl(dirSecurity);
			}
			catch (Exception e)
			{
				OutAPI.PrintLog(e.Message);
				if (e.StackTrace is not null)
					OutAPI.PrintLog(e.StackTrace);
			}
		}


		public static string Post(string url, Dictionary<string, string> dic)
		{
			try
			{
				string result = "";
#pragma warning disable SYSLIB0014 // 类型或成员已过时
				HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
#pragma warning restore SYSLIB0014 // 类型或成员已过时
				req.Method = "POST";
				req.ContentType = "application/x-www-form-urlencoded";
				#region 添加Post 参数
				StringBuilder builder = new();
				int i = 0;
				foreach (var item in dic)
				{
					if (i > 0)
						builder.Append('&');
					builder.AppendFormat("{0}={1}", item.Key, item.Value);
					i++;
				}
				byte[] data = Encoding.UTF8.GetBytes(builder.ToString());
				req.ContentLength = data.Length;
				using (Stream reqStream = req.GetRequestStream())
				{
					reqStream.Write(data, 0, data.Length);
					reqStream.Close();
				}
				#endregion
				HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
				Stream stream = resp.GetResponseStream();
				//获取响应内容
				using (StreamReader reader = new(stream, Encoding.UTF8))
				{
					result = reader.ReadToEnd();
				}
				return result;
			}
			catch
			{

			}
			return "exception";
		}

		public static string GBKToUTF8(string str)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			Encoding utf8;
			Encoding gbk;
			utf8 = Encoding.GetEncoding("utf-8");
			gbk = Encoding.GetEncoding("gbk");
			byte[] gb = gbk.GetBytes(str);
			gb = Encoding.Convert(gbk, utf8, gb);
			return utf8.GetString(gb);
		}
	}
}
