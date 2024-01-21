using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

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
			OutAPI.MsgBox("未找到Steam安装目录，请手动选择steam.exe所在位置!").Wait();
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
		public const string DLLInjectorExeBakPath = $"{DLLInjectorConfigDir}\\DLLInjector_bak.exe";
		public const string SpcrunExePath = $"{DLLInjectorConfigDir}\\spcrun.exe";
		public const string SpcrunExitCodePath = $"{DLLInjectorConfigDir}\\ExitCode.txt";
		private static int DllIndex = 0;
		public static string GreenLumaDllPath
		{
			get
			{
				//if (DllIndex == 0)
				//	return $"{DLLInjectorConfigDir}\\GreenLuma.dll";
				return $"{DLLInjectorConfigDir}\\GreenLuma{DllIndex}.dll";
			}
		}
		public const string DLLInjectorIniPath = $"{DLLInjectorConfigDir}\\DLLInjector.ini";
		public const string DLLInjectorBakTxtPath = $"{DLLInjectorConfigDir}\\DLLInjector_bak.txt";
		public const string DLLInjectorAppList = $"{DLLInjectorConfigDir}\\AppList";
		public const string DLLInjectorLogTxt = $"{DLLInjectorConfigDir}\\log.txt";
		public const string DLLInjectorLogErrTxt = $"{DLLInjectorConfigDir}\\logerr.txt";
		public const string DLLInjectorBakStartTxtPath = $"{DLLInjectorConfigDir}\\bak_start.txt";
		public const string DLLInjectorBakEndTxtPath = $"{DLLInjectorConfigDir}\\bak_end.txt";
		public const string GreenLumaLogTxt = $"{DLLInjectorConfigDir}\\GreenLuma_2024.log";
		public const string GreenLumaNoQuestionFile = $"{DLLInjectorConfigDir}\\NoQuestion.bin";
		private const string fileHead = "W0RsbEluamVjdG9yXQpBbGxvd011bHRpcGxlSW5zdGFuY2VzT2ZETExJbmplY3RvciA9IDAKVXNlRnVsbFBhdGhzRnJvbUluaSA9IDEK";
		private const string fileEnd = "CldhaXRGb3JQcm9jZXNzVGVybWluYXRpb24gPSAwCkVuYWJsZUZha2VQYXJlbnRQcm9jZXNzID0gMApGYWtlUGFyZW50UHJvY2VzcyA9IGV4cGxvcmVyLmV4ZQpFbmFibGVNaXRpZ2F0aW9uc09uQ2hpbGRQcm9jZXNzID0gMApERVAgPSAxClNFSE9QID0gMQpIZWFwVGVybWluYXRlID0gMQpGb3JjZVJlbG9jYXRlSW1hZ2VzID0gMQpCb3R0b21VcEFTTFIgPSAxCkhpZ2hFbnRyb3B5QVNMUiA9IDEKUmVsb2NhdGlvbnNSZXF1aXJlZCA9IDEKU3RyaWN0SGFuZGxlQ2hlY2tzID0gMApXaW4zMmtTeXN0ZW1DYWxsRGlzYWJsZSA9IDAKRXh0ZW5zaW9uUG9pbnREaXNhYmxlID0gMQpDRkcgPSAxCkNGR0V4cG9ydFN1cHByZXNzaW9uID0gMQpTdHJpY3RDRkcgPSAxCkR5bmFtaWNDb2RlRGlzYWJsZSA9IDAKRHluYW1pY0NvZGVBbGxvd09wdE91dCA9IDAKQmxvY2tOb25NaWNyb3NvZnRCaW5hcmllcyA9IDAKRm9udERpc2FibGUgPSAxCk5vUmVtb3RlSW1hZ2VzID0gMQpOb0xvd0xhYmVsSW1hZ2VzID0gMQpQcmVmZXJTeXN0ZW0zMiA9IDAKUmVzdHJpY3RJbmRpcmVjdEJyYW5jaFByZWRpY3Rpb24gPSAxClNwZWN1bGF0aXZlU3RvcmVCeXBhc3NEaXNhYmxlID0gMApTaGFkb3dTdGFjayA9IDAKQ29udGV4dElQVmFsaWRhdGlvbiA9IDAKQmxvY2tOb25DRVRFSENPTlQgPSAwCkNyZWF0ZUZpbGVzID0gMgpGaWxlVG9DcmVhdGVfMSA9IFN0ZWFsdGhNb2RlLmJpbgpGaWxlVG9DcmVhdGVfMiA9IE5vUXVlc3Rpb24uYmluClVzZTRHQlBhdGNoID0gMApGaWxlVG9QYXRjaF8xID0K";
		public static bool IsGreenLumaReady()
		{
			if (!Directory.Exists(DLLInjectorConfigDir))
				return false;
			if (!File.Exists(DLLInjectorExePath))
				return false;
			if (!File.Exists(DLLInjectorExeBakPath))
				return false;
			if (!File.Exists(SpcrunExePath))
				return false;
			if (!File.Exists(GreenLumaDllPath))
				return false;
			if (!File.Exists(DLLInjectorIniPath))
				return false;
			if (!File.Exists(DLLInjectorBakTxtPath))
				return false;
			if (!Directory.Exists(DLLInjectorAppList))
				return false;
			if (!new DirectoryInfo(DLLInjectorAppList).GetFiles("*.txt").Any())
				return false;
			return true;
		}
		//TODO: 带Steam启动参数
		public static int StartGreenLuma(bool adminModel = true)
		{
			//日志清理
			GLFileTools.ClearLogs();
			lock (bak_Err_Str_lock)
			{
				greenLuma_Bak_Err_Str = new();
			}
			int pExitCode;
			if (adminModel) OutAPI.PrintLog("Start Normal Program (Admin)");
			else OutAPI.PrintLog("Start Normal Program");
			using Process p = new();
			try
			{
				File.Delete(DLLInjectorBakTxtPath);
				string cmd;
				if (adminModel)
				{
					cmd = $"cd /d {DLLInjectorConfigDir}&dir&spcrun.exe&exit";
					p.StartInfo.Verb = "runas";
				}
				else
					cmd = $"cd /d {DLLInjectorConfigDir}&dir&%SystemRoot%\\explorer.exe {DLLInjectorConfigDir}\\spcrun.exe&exit";//降低权限，以普通用户运行spcrun.exe,间接运行DLLInjector.exe
				p.StartInfo.FileName = "cmd.exe";
				p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启动
				p.StartInfo.RedirectStandardOutput = true;  //由调用程序获取输出信息
				p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
				p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
				p.StartInfo.CreateNoWindow = !Program.isDebug;          //不显示程序窗口
																		//绑定事件
				p.OutputDataReceived += new DataReceivedEventHandler(P_OutputDataReceived);
				p.ErrorDataReceived += P_ErrorDataReceived;
				p.Start();//启动程序
				p.StandardInput.WriteLine(cmd);//向cmd窗口写入命令
				p.StandardInput.AutoFlush = true;
				p.BeginOutputReadLine();//开始读取输出数据
				p.BeginErrorReadLine();//开始读取错误数据，重要！
			}
			catch (Exception ex)
			{
				_ = OutAPI.MsgBox(ex.Message, "Error");
				throw new Exception("StartGreenLuma Error");
			}
			finally
			{
				p.WaitForExit();//等待程序执行完退出进程
				pExitCode = p.ExitCode;
				p.Close();
			}

			//获取spcrun.exe返回值
			for (int i = 0; i < 100; i++)
			{
				Thread.Sleep(100);
				if (File.Exists(DLLInjectorBakEndTxtPath))
					break;
				if (pExitCode != 0)
					break;
			}

			try
			{
				lock (bak_Err_Str_lock)
				{
					File.AppendAllText(DLLInjectorLogErrTxt, greenLuma_Bak_Err_Str.ToString());
					OutAPI.PrintLog($"Write string builder success : {greenLuma_Bak_Err_Str}");
				}
			}
			catch { }
			if (File.Exists(SpcrunExitCodePath))
				if (int.TryParse(File.ReadAllText(SpcrunExitCodePath), out int exitCode))
					return exitCode;

			//正常运行spcrun.exe但是没获取到返回值
			if (pExitCode == 0)
				return 2048;

			//不正常运行spcrun.exe
			return pExitCode;
		}
		private static readonly object bak_Err_Str_lock = new();
		private static StringBuilder? greenLuma_Bak_Err_Str;
		public static int StartGreenLuma_Bak(bool adminModel = true)
		{
			//日志清理
			ClearLogs();
			int res = 0;
			lock (bak_Err_Str_lock)
			{
				greenLuma_Bak_Err_Str = new();
			}
			if (adminModel) OutAPI.PrintLog("Start Bak Program (Admin)");
			else OutAPI.PrintLog("Start Bak Program");
			using Process p = new();
			try
			{
				File.Delete(DLLInjectorIniPath);
				string cmd;
				if (adminModel)
				{
					cmd = $"cd /d {DLLInjectorConfigDir}&dir&DLLInjector_bak.exe&exit";
					p.StartInfo.Verb = "runas";
				}
				else
					cmd = $"cd /d {DLLInjectorConfigDir}&dir&%SystemRoot%\\explorer.exe {DLLInjectorConfigDir}\\spcrun.exe&exit";
				p.StartInfo.FileName = "cmd.exe";
				p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启动
				p.StartInfo.RedirectStandardOutput = true;  //由调用程序获取输出信息
				p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
				p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
				p.StartInfo.CreateNoWindow = !Program.isDebug;          //不显示程序窗口
																		//绑定事件
				p.OutputDataReceived += new DataReceivedEventHandler(P_OutputDataReceived);
				p.ErrorDataReceived += P_ErrorDataReceived;
				p.Start();//启动程序
				p.StandardInput.WriteLine(cmd);//向cmd窗口写入命令
				p.StandardInput.AutoFlush = true;
				p.BeginOutputReadLine();//开始读取输出数据
				p.BeginErrorReadLine();//开始读取错误数据，重要！
			}
			catch (Exception ex)
			{
				_ = OutAPI.MsgBox(ex.Message, "Error");
				throw new Exception("StartGreenLuma_Bak Error");
			}
			finally
			{
				p.WaitForExit();//等待程序执行完退出进程
				if (adminModel)
					res = p.ExitCode;
				p.Close();
			}
			if (!adminModel)
			{
				int haveEnd = 0;
				for (int i = 0; i < 100; i++)
				{
					Thread.Sleep(100);
					if (File.Exists(DLLInjectorBakEndTxtPath))
					{
						haveEnd = 1; break;
					}
					if (res != 0)
					{
						haveEnd = 3; break;
					}
				}
				if (haveEnd == 3) return res;
				for (int i = 0; i < 50; i++)
				{
					Thread.Sleep(100);
					if (File.Exists(SpcrunExitCodePath))
						break;
					if (res != 0)
						break;
				}
				OutAPI.PrintLog($"int haveEnd = {haveEnd};");
				res = 2048;
				if (haveEnd == 1)
					if (File.Exists(DLLInjectorBakEndTxtPath))
						if (int.TryParse(File.ReadAllText(DLLInjectorBakEndTxtPath), out int exitCode))
							res = exitCode;
						else
							res = 2050;
				if (res == 2048)
				{
					if (File.Exists(SpcrunExitCodePath))
						if (int.TryParse(File.ReadAllText(SpcrunExitCodePath), out int exitCode))
							OutAPI.PrintLog($"Spcrun exit : {exitCode}");
					if (File.Exists(DLLInjectorBakStartTxtPath))
						res = 2049;//运行成功但是中途异常
				}
				return res;//为2048代表根本没运行
			}
			else
			{
				try
				{
					lock (bak_Err_Str_lock)
					{
						File.WriteAllText(DLLInjectorLogErrTxt, greenLuma_Bak_Err_Str.ToString());
						OutAPI.PrintLog($"Write string builder success : {greenLuma_Bak_Err_Str}");
					}
				}
				catch { }
			}
			return res;
		}
		public static void ClearLogs()
		{
			//try
			//{
			//	if (File.Exists(DLLInjectorLogTxt))
			//		File.Delete(DLLInjectorLogTxt);
			//}
			//catch { }
			try
			{
				if (File.Exists(DLLInjectorLogErrTxt))
					File.Delete(DLLInjectorLogErrTxt);
			}
			catch { }
			try
			{
				if (File.Exists(SpcrunExitCodePath))
					File.Delete(SpcrunExitCodePath);
			}
			catch { }
			try
			{
				if (File.Exists(DLLInjectorBakStartTxtPath))
					File.Delete(DLLInjectorBakStartTxtPath);
			}
			catch { }
			try
			{
				if (File.Exists(DLLInjectorBakEndTxtPath))
					File.Delete(DLLInjectorBakEndTxtPath);
			}
			catch { }
		}
		private static void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data is not null)
			{
				OutAPI.PrintLog("err:" + e.Data);
				if (e.Data != "" && greenLuma_Bak_Err_Str != null)
				{
					lock (bak_Err_Str_lock)
					{
						greenLuma_Bak_Err_Str?.Append(e.Data);
					}
					//OutAPI.MsgBox(e.Data, "Error");
				}
			}
		}

		private static void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data is not null)
			{
				OutAPI.PrintLog("log:" + e.Data);
			}
		}

		public static bool WirteGreenLumaConfig(string? steamPath)
		{
			if (steamPath is null or "")
			{
				_ = OutAPI.MsgBox("Fail: steamPath is null", "Error");
				return false;
			}
			try
			{
				// 解包DLLInjector
				if (!Directory.Exists(DLLInjectorConfigDir))
				{
					Directory.CreateDirectory(DLLInjectorConfigDir);
				}
				OutAPI.CreateByB64(DLLInjectorExePath, "DLLInjector.exe", true);
				OutAPI.CreateByB64(SpcrunExePath, "spcrun.exe", true);
				OutAPI.CreateByB64(DLLInjectorExeBakPath, "DLLInjector_bak.exe", true);
				bool writeGreenLumaDll = false;
				for (int i = 0; i < 10; i++)
				{
					try
					{
						OutAPI.CreateByB64(GreenLumaDllPath, "GreenLuma.dll", true);
						writeGreenLumaDll = true;
						break;
					}
					catch
					{
						DllIndex = (DllIndex + 1) % 10;
					}
				}
				if (!writeGreenLumaDll)
				{
					_ = OutAPI.MsgBox("Dll文件被占用！请尝试重启电脑！", "Error");
					return false;
				}
			}
			catch (Exception e)
			{
				_ = OutAPI.MsgBox("尝试在C盘解包临时文件失败！", "Error");
				OutAPI.PrintLog(e.Message);
				OutAPI.PrintLog(e.StackTrace);
				return false;
			}
			// 生成“无需询问”配置
			File.WriteAllText(GreenLumaNoQuestionFile, "1");
			// 生成 ini 文件
			File.WriteAllText(DLLInjectorIniPath, Base64.Base64Decode(fileHead).Replace("\n", "\r\n"));
			File.AppendAllText(DLLInjectorIniPath, $"Exe = {steamPath}\r\nCommandLine =\r\n\r\nDll = {GreenLumaDllPath}");
			File.AppendAllText(DLLInjectorIniPath, Base64.Base64Decode(fileEnd).Replace("\n", "\r\n"));
			// 检验 ini 文件
			try
			{
				var iniStr = File.ReadAllText(DLLInjectorIniPath);
				if (!iniStr.Contains("[DllInjector]") ||
					!iniStr.Contains("FileToPatch_1 ="))
				{
					_ = OutAPI.MsgBox("尝试输出ini配置异常！", "Error");
					return false;
				}
			}
			catch (Exception e)
			{
				OutAPI.PrintLog(e.Message);
				return false;
			}
			// 生成 bak txt文件
			File.WriteAllText(DLLInjectorBakTxtPath, "steam.exe\r\n");
			File.AppendAllText(DLLInjectorBakTxtPath, $"{GreenLumaDllPath}\r\n");
			File.AppendAllText(DLLInjectorBakTxtPath, $"{steamPath}\r\n");
			File.AppendAllText(DLLInjectorBakTxtPath, "-inhibitbootstrap\r\n");
			File.AppendAllText(DLLInjectorBakTxtPath, "10\r\n");
			// 检验 bak txt文件
			try
			{
				var txtStr = File.ReadAllText(DLLInjectorBakTxtPath);
				if (!txtStr.Contains(GreenLumaDllPath) ||
					!txtStr.Contains("-inhibitbootstrap"))
				{
					_ = OutAPI.MsgBox("尝试输出txt配置异常！", "Error");
					return false;
				}
			}
			catch (Exception e)
			{
				OutAPI.PrintLog(e.Message);
				return false;
			}
			// 生成游戏id列表文件
			if (!Directory.Exists(DLLInjectorAppList))
			{
				Directory.CreateDirectory(DLLInjectorAppList);
			}
			long pos = 0;
			foreach (var i in DataSystem.Instance.GetGameDatas())
			{
				if (!i.IsSelected) continue;
				File.WriteAllText($"{DLLInjectorAppList}\\{pos}.txt", i.GameId.ToString());
				//OutAPI.AddSecurityControll2File($"{DLLInjectorAppList}\\{pos}.txt");
				pos++;
				foreach (var j in i.DlcsList)
				{
					if (!j.IsSelected) continue;
					File.WriteAllText($"{DLLInjectorAppList}\\{pos}.txt", j.DlcId.ToString());
					//OutAPI.AddSecurityControll2File($"{DLLInjectorAppList}\\{pos}.txt");
					pos++;
				}
			}

			//试着解决权限问题
			OutAPI.AddSecurityControll2File(DLLInjectorExePath);
			OutAPI.AddSecurityControll2File(DLLInjectorExeBakPath);
			OutAPI.AddSecurityControll2File(SpcrunExePath);
			OutAPI.AddSecurityControll2File(GreenLumaDllPath);

			OutAPI.AddSecurityControll2File(DLLInjectorIniPath);
			OutAPI.AddSecurityControll2File(DLLInjectorBakTxtPath);

			OutAPI.AddSecurityControll2File(SpcrunExitCodePath, false);
			OutAPI.AddSecurityControll2File(DLLInjectorLogTxt, false);
			OutAPI.AddSecurityControll2File(DLLInjectorLogErrTxt, false);
			OutAPI.AddSecurityControll2File(GreenLumaLogTxt, false);
			OutAPI.AddSecurityControll2File(GreenLumaNoQuestionFile, false);

			OutAPI.AddSecurityControll2Folder(DLLInjectorConfigDir);
			OutAPI.AddSecurityControll2Folder(DLLInjectorAppList);
			return true;
		}
		public static void DeleteGreenLumaConfig()
		{
			// 清理ini文件
			if (File.Exists(DLLInjectorIniPath))
			{
				File.Delete(DLLInjectorIniPath);
			}
			if (File.Exists(DLLInjectorBakTxtPath))
			{
				File.Delete(DLLInjectorBakTxtPath);
			}
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

