﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

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
		/// <summary>
		/// 该函数将创建指定窗口的线程设置到前台，并且激活该窗口。键盘输入转向该窗口，并为用户改各种可视的记号。系统给创建前台窗口的线程分配的权限稍高于其他线程。
		/// </summary>
		/// <param name="hWnd">将被激活并被调入前台的窗口句柄。</param>
		/// <returns>如果窗口设入了前台，返回值为非零；如果窗口未被设入前台，返回值为零。</returns>
		[LibraryImport("User32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static partial bool SetForegroundWindow(IntPtr hWnd);

		public static IntPtr GetMainWindowHandle(int processId)
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

		private static readonly string programTempDir = /*$"{Path.GetTempPath()}"*/"C:\\tmp\\" + "exewim2oav.addy.vlz";
		public static string TempDir
		{
			get
			{
				//创建目录
				if (!Directory.Exists(programTempDir))
				{
					Directory.CreateDirectory(programTempDir);
				}
				return programTempDir;
			}
		}
		public readonly static string LogFilePath = $"{OutAPI.TempDir}\\log.txt";
		public static void PrintLog(string? text)
		{
			if (text is null || string.IsNullOrEmpty(text.Trim()))
			{
				return;
			}
			var writer = File.AppendText(LogFilePath);
			string contain = $"\r\n[{DateTime.Now}]" +
				"\r\n" + text.Trim() + "\r\n";
			writer.Write(contain);
			Console.Write(contain);
			writer.Close();
		}
		public static string? CreateByB64(string targetFile, string b64FileName, bool replace = false)
		{
			string resourcefile = "CN_GreenLumaGUI.DLLInjector." + b64FileName + ".b64";
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
				Directory.Delete(programTempDir, true);
			}
			catch
			{

			}
		}
		public static void TempClear(string fileName)
		{
			string targetFile = $"{programTempDir}\\{fileName}";
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
		public static void AddSecurityControll2File(string filePath)
		{

			//获取文件信息
			FileInfo fileInfo = new FileInfo(filePath);
			//获得该文件的访问权限
			System.Security.AccessControl.FileSecurity fileSecurity = fileInfo.GetAccessControl();
			//添加ereryone用户组的访问权限规则 完全控制权限
			fileSecurity.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, AccessControlType.Allow));
			//添加Users用户组的访问权限规则 完全控制权限
			fileSecurity.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, AccessControlType.Allow));
			//设置访问权限
			fileInfo.SetAccessControl(fileSecurity);
		}

		/// <summary>
		///为文件夹添加users，everyone用户组的完全控制权限
		/// </summary>
		/// <param name="dirPath"></param>
		public static void AddSecurityControll2Folder(string dirPath)
		{
			//获取文件夹信息
			DirectoryInfo dir = new DirectoryInfo(dirPath);
			//获得该文件夹的所有访问权限
			System.Security.AccessControl.DirectorySecurity dirSecurity = dir.GetAccessControl(AccessControlSections.All);
			//设定文件ACL继承
			InheritanceFlags inherits = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
			//添加ereryone用户组的访问权限规则 完全控制权限
			FileSystemAccessRule everyoneFileSystemAccessRule = new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow);
			//添加Users用户组的访问权限规则 完全控制权限
			FileSystemAccessRule usersFileSystemAccessRule = new FileSystemAccessRule("Users", FileSystemRights.FullControl, inherits, PropagationFlags.None, AccessControlType.Allow);
			bool isModified = false;
			dirSecurity.ModifyAccessRule(AccessControlModification.Add, everyoneFileSystemAccessRule, out isModified);
			dirSecurity.ModifyAccessRule(AccessControlModification.Add, usersFileSystemAccessRule, out isModified);
			//设置访问权限
			dir.SetAccessControl(dirSecurity);
		}
	}
}
