using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;

namespace CN_GreenLumaGUI.ViewModels
{
	public partial class ManagerViewModel : ObservableObject
	{
		const string defStartButtonColor = "#64bd4d";
		const string closeStartButtonColor = "#f44b56";//ffa754
		const string darkStartButtonColor = "#424242";
		const string defStartButtonContent = "启动Steam";
		const string closeStartButtonContent = "关闭Steam";
		const string darkStartButtonContent = "X";

		private bool CancelWait { get; set; }

		void DockInit()
		{
			lock (this)
			{
				CancelWait = false;
			}
			StartButtonColor = defStartButtonColor;
			StartButtonContent = defStartButtonContent;
			LoadingBarEcho = Visibility.Hidden;
			ButtonPromptTextEcho = Visibility.Collapsed;
			StartButtonCmd = new RelayCommand(StartButton);
			Thread updateThread = new(UpdateSteamState)
			{
				IsBackground = true
			};
			updateThread.Start();
			checkedNum = DataSystem.Instance.CheckedNum;
			WeakReferenceMessenger.Default.Register<CheckedNumChangedMessage>(this, (r, m) =>
			{
				CheckedNumNow = DataSystem.Instance.CheckedNum;
			});

			WeakReferenceMessenger.Default.Register<DockInformMessage>(this, (r, m) =>
			{
				Application.Current.Dispatcher.Invoke((Action)delegate ()
				{
					PrivateDockInform(m.messageText);
				});
			});

			WeakReferenceMessenger.Default.Register<ConfigChangedMessage>(this, (r, m) =>
			{
				if (m.kind == "ScrollBarEcho")
				{
					Application.Current.Dispatcher.Invoke((Action)delegate ()
					{
						OnPropertyChanged(nameof(ScrollBarEchoState));
					});
				}
				if (m.kind == "HidePromptText")
				{
					ButtonPromptTextEcho = DataSystem.Instance.HidePromptText ? Visibility.Collapsed : Visibility.Visible;
				}
			});
		}
		public static void Inform(string str)
		{
			WeakReferenceMessenger.Default.Send(new DockInformMessage(str));
		}
		private void PrivateDockInform(string message)
		{
			if (windowFrom.SnackbarInform.MessageQueue is { } messageQueue)
			{
				_ = Task.Factory.StartNew(() =>
				{
					messageQueue.Clear();
					messageQueue.Enqueue(message);
				}
				);
			}
		}

		//Binding
		private readonly int maxUnlockNum = 149; //GreenLuma最大支持到149的上限
		public long MaxUnlockNum { get { return maxUnlockNum; } }

		private long checkedNum;
		public long CheckedNumNow
		{
			get { return checkedNum; }
			set
			{
				checkedNum = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(CheckedNumColor));
			}
		}
		public string CheckedNumColor
		{
			get { return checkedNum > MaxUnlockNum ? "#f04a55" : "#4ec9ae"; }
		}
		private string? startButtonColor;
		public string? StartButtonColor
		{
			get { return startButtonColor; }
			set
			{
				startButtonColor = value;
				OnPropertyChanged();
			}
		}
		private string? startButtonContent;
		public string? StartButtonContent
		{
			get { return startButtonContent; }
			set
			{
				startButtonContent = value;
				OnPropertyChanged();
			}
		}

		private Visibility loadingBarEcho;
		public Visibility LoadingBarEcho
		{
			get { return loadingBarEcho; }
			set
			{
				loadingBarEcho = value;
				OnPropertyChanged();
			}
		}

		private Visibility buttonPromptTextEcho;
		public Visibility ButtonPromptTextEcho
		{
			get { return buttonPromptTextEcho; }
			set
			{
				buttonPromptTextEcho = value;
				OnPropertyChanged();
			}
		}

		//Commands
		public RelayCommand? StartButtonCmd { get; set; }
		private string buttonState = "StartSteam";
		private void StartButton()
		{
			if (ButtonPromptTextEcho == Visibility.Visible)
				ButtonPromptTextEcho = Visibility.Collapsed;

			switch (buttonState)
			{
				case "StartSteam":
					//超出上限时提醒
					if (CheckedNumNow > MaxUnlockNum)
					{
						OutAPI.MsgBox("解锁数量超限。");
						return;
					}
					//点击开始按钮如果配置中没有路径就读取steam路径
					if (DataSystem.Instance.SteamPath is null or "")
					{
						DataSystem.Instance.SteamPath = GLFileTools.GetSteamPath_Auto();
						if (DataSystem.Instance.SteamPath == "")
						{
							DataSystem.Instance.SteamPath = null;
							return;
						}
					}
					lock (this)
					{
						CancelWait = false;
					}
					StateToDisable();
					Task.Run(StartSteamUnlock);
					break;
				case "CloseSteam":
					KillSteam();
					StateToStartSteam();
					break;
				default:
					return;
			}
		}

		private async Task StartSteamUnlock()
		{
			try
			{
				OutAPI.PrintLog("Task start.");
				if (!File.Exists(DataSystem.Instance.SteamPath))
				{
					StateToStartSteam();
					await Task.Delay(50);
					OutAPI.MsgBox("steam路径错误！");
					return;
				}
				//防止前一次kill不及时，略微延时
				await Task.Delay(500);
				//解锁模式启动steam
				if (DataSystem.Instance.CheckedNum > 0)
				{
					OutAPI.PrintLog("GreenLuma ready start.");
					//清理GreenLuma配置文件
					GLFileTools.DeleteGreenLumaConfig();
					//写入GreenLuma配置文件
					GLFileTools.WirteGreenLumaConfig(DataSystem.Instance.SteamPath);
					//检测GreenLuma完整性
					if (!GLFileTools.IsGreenLumaReady())
					{
						GLFileTools.DeleteGreenLumaConfig();
						StateToStartSteam();
						await Task.Delay(50);
						OutAPI.MsgBox("文件缺失！");
						return;
					}
					//throw new Exception();//测试模拟异常
					//启动GreenLuma
					OutAPI.PrintLog("GreenLuma start.");
					int exitCode = 1024;
					if (DataSystem.Instance.StartWithBak)
					{
						GLFileTools.StartGreenLuma_Bak();
					}
					else exitCode = GLFileTools.StartGreenLuma();
					OutAPI.PrintLog("Exit " + exitCode);
					//返回值分析
					string errStr = "";
					if (exitCode != 0 && exitCode != 1024)
					{
						if (File.Exists(GLFileTools.DLLInjectorLogErrTxt))
							errStr = File.ReadAllText(GLFileTools.DLLInjectorLogErrTxt).Trim();
						if (errStr == "Access is denied.")
						{
							DataSystem.Instance.StartWithBak = true;
							OutAPI.MsgBox("检测到系统版本不支持问题，尝试使用兼容模式启动，请在接下来的选项中选择“是”。");
							GLFileTools.StartGreenLuma_Bak();
							exitCode = 1024;
						}
					}
					//等待启动
					await Task.Delay(20000);
					OutAPI.PrintLog("Wait time finish.");
					bool fileLost = false;
					if (!File.Exists(GLFileTools.DLLInjectorExePath))
					{
						OutAPI.PrintLog("DLLInjectorExe lost");
						fileLost = true;
					}
					if (!File.Exists(GLFileTools.DLLInjectorExeBakPath))
					{
						OutAPI.PrintLog("DLLInjectorExe_Bak lost");
						fileLost = true;
					}
					if (!File.Exists(GLFileTools.SpcrunExePath))
					{
						OutAPI.PrintLog("SpcrunExe lost");
						fileLost = true;
					}
					if (fileLost)
					{
						OutAPI.MsgBox("文件好像丢失了，可能是被Windows杀软误删了，可以安装一个火绒用来屏蔽Windows自带的安全中心再试试");
					}
					else if (exitCode != 0 && exitCode != 1024)
					{
						string errmsg = "启动失败！请联系开发者。";
						if (!string.IsNullOrEmpty(errStr))
							errmsg += $"({errStr})";
						OutAPI.MsgBox(errmsg);
					}
				}
				else
					OutAPI.PrintLog("checkednum<=0");

			}
			catch (Exception e)
			{
				OutAPI.MsgBox(e.Message);
				if (e.StackTrace is not null)
					OutAPI.MsgBox(e.StackTrace);
			}
			await Task.Delay(5000);
			if (buttonState == "Disable")
			{
				if (DataSystem.Instance.CheckedNum == 0)
				{
					OutAPI.MsgBox("请先勾选需要解锁的游戏。");
					////普通模式启动steam
					//var steamProcess = new Process();
					//steamProcess.StartInfo.FileName = DataSystem.Instance.SteamPath;
					////steamProcess.StartInfo.Arguments = ;
					//steamProcess.Start();
				}
				else
				{
					try
					{
						string data = $"[v{Program.Version}]\n";
						if (File.Exists(OutAPI.LogFilePath))
							data += "-----[log0.txt]-----\n" + File.ReadAllText(OutAPI.LogFilePath) + "\n";
						if (File.Exists(GLFileTools.DLLInjectorLogTxt))
						{
							string logStr = File.ReadAllText(GLFileTools.DLLInjectorLogTxt);
							data += "-----[log.txt]-----\n" + logStr + "\n";
						}
						if (File.Exists(GLFileTools.DLLInjectorLogErrTxt))
						{
							string logStr = File.ReadAllText(GLFileTools.DLLInjectorLogErrTxt);
							data += "-----[logerr.txt]-----\n" + logStr + "\n";
						}
						string dataB64 = Base64.Base64Encode(Encoding.UTF8, data);
						dataB64 = HttpUtility.UrlEncode(dataB64);
						Dictionary<string, string> dic = new()
						{
							{ "logString", dataB64  ?? ""}
						};
						//发送日志
						OutAPI.Post("http://8.222.250.210:9000/SoftLog", dic);
					}
					catch (Exception e)
					{
						string data = $"[v{Program.Version}]\n";
						data += $"{e.Message}\n";
						data += "Have expection when send log\n";
						data += $"{e.StackTrace}\n";
						string dataB64 = Base64.Base64Encode(Encoding.UTF8, data);
						dataB64 = HttpUtility.UrlEncode(dataB64);
						Dictionary<string, string> dic = new()
						{
							{ "logString", dataB64 ?? ""}
						};
						//发送日志发送错误的日志
						OutAPI.Post("http://8.222.250.210:9000/SoftLog", dic);
					}
				}
				await Task.Delay(50);
				lock (this)
				{
					CancelWait = true;
				}
			}

		}

		private void KillSteam()
		{
			if (steamProcesses is null) return;
			foreach (var process in steamProcesses)
			{
				process.Kill(true);
			}
		}
		private void StateToDisable()
		{
			buttonState = "Disable";
			StartButtonColor = darkStartButtonColor;
			StartButtonContent = darkStartButtonContent;
			LoadingBarEcho = Visibility.Visible;
		}
		private void StateToStartSteam()
		{
			buttonState = "StartSteam";
			StartButtonColor = defStartButtonColor;
			StartButtonContent = defStartButtonContent;
			LoadingBarEcho = Visibility.Hidden;
		}
		private void StateToCloseSteam()
		{
			buttonState = "CloseSteam";
			StartButtonColor = closeStartButtonColor;
			StartButtonContent = closeStartButtonContent;
			LoadingBarEcho = Visibility.Hidden;
		}
		private Process[]? steamProcesses;
		private void UpdateSteamState()
		{
			while (true)
			{
				steamProcesses = Process.GetProcessesByName("steam");//获取指定的进程名   
				if (steamProcesses.Length > 0) //如果可以获取到知道的进程名则说明已经启动
				{
					StateToCloseSteam();
				}
				else
				{
					steamProcesses = null;
					if (buttonState != "Disable")
						StateToStartSteam();
					lock (this)
					{
						if (CancelWait)
							StateToStartSteam();
					}

				}
				Thread.Sleep(1000);
			}
		}
	}
}
