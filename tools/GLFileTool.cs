using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CN_GreenLumaGUI.tools
{
	public static class GLFileTools
	{
		public static string GetSteamPath_UserChose()
		{
			OpenFileDialog fileDialog = new()
			{
				Filter = "Steam|steam.exe"
			};
			fileDialog.ShowDialog();
			System.Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
			if (fileDialog.FileName == "" || !File.Exists(fileDialog.FileName))
			{
				return "";
			}
			return fileDialog.FileName;
		}
		public static string GetSteamPath_Auto()
		{
			string res = GetSteamPath_RegistryKey();
			if (res != "")
				return res;
			res = GetSteamPath_StartMenu();
			if (res != "")
				return res;
			res = GetSteamPath_ProgramFiles();
			if (res != "")
				return res;
			OutAPI.MsgBox("未找到Steam安装目录，请手动选择steam.exe所在位置!");
			return GetSteamPath_UserChose();
		}
		public static string GetSteamPath_RegistryKey()
		{
			string res = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam")?.GetValue("SteamExe")?.ToString() ?? "";
			res = res.Trim();
			if (res != "" && !File.Exists(res)) res = "";
			return res;
		}

		public static string GetSteamPath_StartMenu()
		{
			string res = "";
			string path = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + "\\Programs\\Steam\\Steam.lnk";
			if (File.Exists(path))
			{
				IWshRuntimeLibrary.WshShell shell = new();
				var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(path);
				res = shortcut.TargetPath;
			}
			res = res.Trim();
			if (res != "" && !File.Exists(res)) res = "";
			return res;
		}
		public static string GetSteamPath_ProgramFiles()
		{
			string path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\Steam\\steam.exe";
			if (File.Exists(path))
				return path;
			path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Steam\\steam.exe";
			if (File.Exists(path))
				return path;
			return "";
		}

		public const string DLLInjectorConfigDir = "C:\\tmp\\exewim2oav.addy.vlz\\DLLInjector";
		public const string DLLInjectorExePath = $"{DLLInjectorConfigDir}\\DLLInjector.exe";
		public const string SpcrunExePath = $"{DLLInjectorConfigDir}\\spcrun.exe";
		public const string GreenLumaDllPath = $"{DLLInjectorConfigDir}\\GreenLuma.dll";
		public const string DLLInjectorIniPath = $"{DLLInjectorConfigDir}\\DLLInjector.ini";
		public const string DLLInjectorAppList = $"{DLLInjectorConfigDir}\\AppList";
		private const string fileHead = "W0RsbEluamVjdG9yXQpBbGxvd011bHRpcGxlSW5zdGFuY2VzT2ZETExJbmplY3RvciA9IDAKVXNlRnVsbFBhdGhzRnJvbUluaSA9IDEK";
		private const string fileEnd = "CkRsbCA9IEdyZWVuTHVtYS5kbGwKV2FpdEZvclByb2Nlc3NUZXJtaW5hdGlvbiA9IDAKRW5hYmxlRmFrZVBhcmVudFByb2Nlc3MgPSAwCkZha2VQYXJlbnRQcm9jZXNzID0gZXhwbG9yZXIuZXhlCkVuYWJsZU1pdGlnYXRpb25zT25DaGlsZFByb2Nlc3MgPSAwCkRFUCA9IDEKU0VIT1AgPSAxCkhlYXBUZXJtaW5hdGUgPSAxCkZvcmNlUmVsb2NhdGVJbWFnZXMgPSAxCkJvdHRvbVVwQVNMUiA9IDEKSGlnaEVudHJvcHlBU0xSID0gMQpSZWxvY2F0aW9uc1JlcXVpcmVkID0gMQpTdHJpY3RIYW5kbGVDaGVja3MgPSAwCldpbjMya1N5c3RlbUNhbGxEaXNhYmxlID0gMApFeHRlbnNpb25Qb2ludERpc2FibGUgPSAxCkNGRyA9IDEKQ0ZHRXhwb3J0U3VwcHJlc3Npb24gPSAxClN0cmljdENGRyA9IDEKRHluYW1pY0NvZGVEaXNhYmxlID0gMApEeW5hbWljQ29kZUFsbG93T3B0T3V0ID0gMApCbG9ja05vbk1pY3Jvc29mdEJpbmFyaWVzID0gMApGb250RGlzYWJsZSA9IDEKTm9SZW1vdGVJbWFnZXMgPSAxCk5vTG93TGFiZWxJbWFnZXMgPSAxClByZWZlclN5c3RlbTMyID0gMApSZXN0cmljdEluZGlyZWN0QnJhbmNoUHJlZGljdGlvbiA9IDEKU3BlY3VsYXRpdmVTdG9yZUJ5cGFzc0Rpc2FibGUgPSAwClNoYWRvd1N0YWNrID0gMApDb250ZXh0SVBWYWxpZGF0aW9uID0gMApCbG9ja05vbkNFVEVIQ09OVCA9IDAKQ3JlYXRlRmlsZXMgPSAyCkZpbGVUb0NyZWF0ZV8xID0gU3RlYWx0aE1vZGUuYmluCkZpbGVUb0NyZWF0ZV8yID0gTm9RdWVzdGlvbi5iaW4KVXNlNEdCUGF0Y2ggPSAwCkZpbGVUb1BhdGNoXzEgPQo=";
		public static bool IsGreenLumaReady()
		{
			if (!Directory.Exists(DLLInjectorConfigDir))
				return false;
			if (!File.Exists(DLLInjectorExePath))
				return false;
			if (!File.Exists(SpcrunExePath))
				return false;
			if (!File.Exists(GreenLumaDllPath))
				return false;
			if (!File.Exists(DLLInjectorIniPath))
				return false;
			if (!Directory.Exists(DLLInjectorAppList))
				return false;
			if (!new DirectoryInfo(DLLInjectorAppList).GetFiles("*.txt").Any())
				return false;
			return true;
		}
		public static void StartGreenLuma(bool adminModel = true)
		{
			using Process p = new();
			string cmd;
			if (adminModel)
				cmd = $"cd /d {DLLInjectorConfigDir}&dir&spcrun.exe&exit";
			else
				cmd = $"cd /d {DLLInjectorConfigDir}&dir&explorer.exe spcrun.exe&exit";//降低权限，以普通用户运行spcrun.exe,间接运行DLLInjector.exe
			p.StartInfo.FileName = "cmd.exe";
			p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启		动
			p.StartInfo.RedirectStandardOutput = true;  //由调用程序获取输出信息
			p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
			p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
			p.StartInfo.CreateNoWindow = !Program.isDebug;          //不显示程序窗口
																	//绑定事件
			p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
			p.ErrorDataReceived += P_ErrorDataReceived;
			p.Start();//启动程序
			p.StandardInput.WriteLine(cmd);//向cmd窗口写入命令
			p.StandardInput.AutoFlush = true;
			p.BeginOutputReadLine();//开始读取输出数据
			p.BeginErrorReadLine();//开始读取错误数据，重要！
			p.WaitForExit();//等待程序执行完退出进程
			p.Close();
		}

		private static void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			OutAPI.PrintLog(e.Data);
		}

		private static void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			OutAPI.PrintLog(e.Data);
		}

		public static void WirteGreenLumaConfig(string? steamPath)
		{
			if (steamPath is null or "")
			{
				OutAPI.MsgBox("Fail: steamPath is null", "Error");
				return;
			}
			try
			{
				// 解包DLLInjector
				if (!Directory.Exists(DLLInjectorConfigDir))
				{
					Directory.CreateDirectory(DLLInjectorConfigDir);
					OutAPI.AddSecurityControll2Folder(DLLInjectorConfigDir);
				}

				OutAPI.CreateByB64(DLLInjectorExePath, "DLLInjector.exe", true);
				OutAPI.CreateByB64(SpcrunExePath, "spcrun.exe", true);
				OutAPI.CreateByB64(GreenLumaDllPath, "GreenLuma.dll", true);
				OutAPI.AddSecurityControll2File(DLLInjectorExePath);
				OutAPI.AddSecurityControll2File(SpcrunExePath);
				OutAPI.AddSecurityControll2File(GreenLumaDllPath);
			}
			catch
			{
				OutAPI.MsgBox("尝试在C盘解包临时文件失败！", "Error");
			}
			// 生成ini文件
			File.WriteAllText(DLLInjectorIniPath, Base64.Base64Decode(fileHead));
			File.AppendAllText(DLLInjectorIniPath, $"Exe = {steamPath}\nCommandLine =");
			File.AppendAllText(DLLInjectorIniPath, Base64.Base64Decode(fileEnd));
			OutAPI.AddSecurityControll2File(DLLInjectorIniPath);
			// 生成游戏id列表文件
			if (!Directory.Exists(DLLInjectorAppList))
			{
				Directory.CreateDirectory(DLLInjectorAppList);
				OutAPI.AddSecurityControll2Folder(DLLInjectorAppList);
			}
			long pos = 0;
			foreach (var i in DataSystem.Instance.GetGameDatas())
			{
				if (!i.IsSelected) continue;
				File.WriteAllText($"{DLLInjectorAppList}\\{pos}.txt", i.GameId.ToString());
				OutAPI.AddSecurityControll2File($"{DLLInjectorAppList}\\{pos}.txt");
				pos++;
				foreach (var j in i.DlcsList)
				{
					if (!j.IsSelected) continue;
					File.WriteAllText($"{DLLInjectorAppList}\\{pos}.txt", j.DlcId.ToString());
					OutAPI.AddSecurityControll2File($"{DLLInjectorAppList}\\{pos}.txt");
					pos++;
				}
			}
		}
		public static void DeleteGreenLumaConfig()
		{
			// 清理ini文件
			File.Delete(DLLInjectorIniPath);
			// 清理游戏id列表文件
			if (Directory.Exists(DLLInjectorAppList))
			{
				try
				{
					Directory.Delete(DLLInjectorAppList, true);
				}
				catch
				{
					DirectoryInfo di = new(DLLInjectorAppList);
					FileInfo[] arrFi = di.GetFiles("*.txt");
					foreach (FileInfo fi in arrFi)
					{
						try
						{
							File.Delete(di + "\\" + fi.Name);
						}
						catch
						{
						}
					}
				}
			}
		}
	}
}

