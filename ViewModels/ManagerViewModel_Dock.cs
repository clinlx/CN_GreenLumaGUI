using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.tools;
using CN_GreenLumaGUI.Windows;
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
using System.Windows.Controls;
using System.Xml;

namespace CN_GreenLumaGUI.ViewModels
{
	public partial class ManagerViewModel : ObservableObject
	{
		const string defStartButtonColor = "#64bd4d";
		const string closeStartButtonColor = "#f44b56";//ffa754
		const string darkStartButtonColor = "#424242";
		string defStartButtonContent => LocalizationService.GetString("Bottom_Start");
		string closeStartButtonContent => LocalizationService.GetString("Bottom_Close");
		string darkStartButtonContent => LocalizationService.GetString("Bottom_Loading");

		private bool CancelWait { get; set; }

		void DockInit()
		{
			lock (this)
			{
				CancelWait = false;
			}
			StartButtonColor = darkStartButtonColor;
			StartButtonContent = darkStartButtonContent;
			LoadingBarEcho = Visibility.Hidden;
			NormalStartButtonVisibility = DataSystem.Instance.EchoStartSteamNormalButton ? Visibility.Visible : Visibility.Collapsed;
			FAQButtonEcho = DataSystem.Instance.HidePromptText ? Visibility.Collapsed : Visibility.Visible;
			FAQButtonCmd = new RelayCommand(FAQButton);
			StartButtonCmd = new RelayCommand(StartButton);
			NormalStartButtonCmd = new RelayCommand(NormalStartButton);
			NormalRestartButtonCmd = new RelayCommand(NormalRestartButton);
			InjectRestartButtonCmd = new RelayCommand(InjectRestartButton);
			checkedNum = DataSystem.Instance.CheckedNum;

			EchoButtonPromptTextDelay();

			WeakReferenceMessenger.Default.Register<LoadFinishedMessage>(this, (r, m) =>
			{
				StateToStartSteam();
				Thread updateThread = new(UpdateSteamState)
				{
					IsBackground = true
				};
				updateThread.Start();
			});
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
					FAQButtonEcho = DataSystem.Instance.HidePromptText ? Visibility.Collapsed : Visibility.Visible;
				}
				if (m.kind == "EchoStartSteamNormalButton")
				{
					NormalStartButtonVisibility = DataSystem.Instance.EchoStartSteamNormalButton ? Visibility.Visible : Visibility.Collapsed;
				}
				if (m.kind == nameof(DataSystem.Instance.LanguageCode))
				{
					// 當語言變更時，根據當前按鈕狀態更新按鈕文字
					switch (buttonState)
					{
						case "Disable":
							StartButtonContent = darkStartButtonContent;
							break;
						case "StartSteam":
							StartButtonContent = defStartButtonContent;
							break;
						case "CloseSteam":
							StartButtonContent = closeStartButtonContent;
							break;
					}
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
		private readonly int maxUnlockNum = DllReader.TotalMaxUnlockNum;
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

		private Visibility normalStartButtonVisibility;
		public Visibility NormalStartButtonVisibility
		{
			get
			{
				if (buttonState != "StartSteam")
					return Visibility.Collapsed;
				return normalStartButtonVisibility;
			}
			set
			{
				normalStartButtonVisibility = value;
				OnPropertyChanged();
			}
		}

		private bool isFirstRun = true;
		private Visibility buttonPromptTextEcho;
		public Visibility ButtonPromptTextEcho
		{
			get
			{
				if (buttonState != "StartSteam")
					return Visibility.Collapsed;
				if (!isFirstRun)
					return Visibility.Collapsed;
				return buttonPromptTextEcho;
			}
			set
			{
				buttonPromptTextEcho = value;
				OnPropertyChanged();
			}
		}
		private void EchoButtonPromptTextDelay()
		{
			Task.Run(async () =>
			{
				await Task.Delay(10000);
				if (isFirstRun)
				{
					Application.Current.Dispatcher.Invoke((Action)delegate ()
					{
						ButtonPromptTextEcho = Visibility.Visible;
					});
				}
			});
		}

		private Visibility fAQButtonEcho;
		public Visibility FAQButtonEcho
		{
			get { return fAQButtonEcho; }
			set
			{
				fAQButtonEcho = value;
				OnPropertyChanged();
			}
		}
		//Commands
		private InformWindow? faqWindow;
		public RelayCommand? FAQButtonCmd { get; set; }
		private void FAQButton()
		{
			try
			{
				if (faqWindow is null || faqWindow.IsClosed)
				{
					string readmeFileName = DataSystem.Instance.LanguageCode switch
					{
						"zh-CN" => "README.md",
						_ => $"README-{DataSystem.Instance.LanguageCode}.md"
					};
					string? readme = OutAPI.GetFromRes(readmeFileName) ?? OutAPI.GetFromRes("README-en-US.md");
					if (readme is null) return;
					faqWindow = new(LocalizationService.GetString("Dock_FAQ"), TextItemModel.CreateListFromMarkDown(readme));
				}
				if (!faqWindow.IsVisible)
				{
					faqWindow.Show();
				}
				else
				{
					faqWindow.Close();
				}
			}
			catch { }
		}
		public RelayCommand? StartButtonCmd { get; set; }
		public RelayCommand? NormalStartButtonCmd { get; set; }
		public RelayCommand? NormalRestartButtonCmd { get; set; }
		public RelayCommand? InjectRestartButtonCmd { get; set; }

		private string buttonState = "Disable";
		public static bool SteamRunning => ManagerWindow.ViewModel?.buttonState == "CloseSteam";
		private void StartWithInject()
		{
			//超出上限时提醒
			if (CheckedNumNow > MaxUnlockNum)
			{
				_ = OutAPI.MsgBox(LocalizationService.GetString("Dock_UnlockLimitExceeded"));
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
		}
		private void StartWithoutInject()
		{
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
			Task.Run(StartSteamNormal);
		}

		// 启动/关闭 Steam 共用按钮
		private void StartButton()
		{
			if (isFirstRun)
			{
				isFirstRun = false;
				OnPropertyChanged(nameof(ButtonPromptTextEcho));
			}
			switch (buttonState)
			{
				case "StartSteam":
					StartWithInject();
					break;
				case "CloseSteam":
					KillSteam();
					StateToStartSteam();
					break;
				default:
					return;
			}
		}
		// "N" 按钮
		private void NormalStartButton()
		{
			if (isFirstRun)
			{
				isFirstRun = false;
				OnPropertyChanged(nameof(ButtonPromptTextEcho));
			}
			switch (buttonState)
			{
				case "StartSteam":
					StartWithoutInject();
					break;
				case "CloseSteam":
					KillSteam();
					StateToStartSteam();
					break;
				default:
					return;
			}
		}
		// 正常重启：关闭 Steam 并正常启动（不注入）
		private void NormalRestartButton()
		{
			if (buttonState != "CloseSteam") return;
			KillSteam();
			StartWithoutInject();
		}

		// 注入重启：关闭 Steam，执行注入后启动 Steam
		private void InjectRestartButton()
		{
			if (buttonState != "CloseSteam") return;
			KillSteam();
			StartWithInject();
		}
		private int lastStartType = 0;
		private int lastStartId = 0;
		private object lastStartLock = new();
		private async Task StartSteamNormal()
		{
			lastStartType = -1;
			int lastStartSteamTimes = startSteamTimesNormal;
			int nowStartId;
			lock (lastStartLock)
			{
				nowStartId = lastStartId;
				lastStartId++;
			}
			DataSystem.Instance.SaveData();
			if (!File.Exists(DataSystem.Instance.SteamPath))
			{
				StateToStartSteam();
				await Task.Delay(50);
				_ = OutAPI.MsgBox(LocalizationService.GetString("Dock_SteamPathError"));
				return;
			}

			KillSteam();
			//防止前一次kill不及时，略微延时
			await Task.Delay(500);

			OutAPI.PrintLog("Start Steam without inject : begin");

			// 正常启动 Steam（不注入）
			var steamProcess = new Process();
			steamProcess.StartInfo.FileName = DataSystem.Instance.SteamPath;
			steamProcess.StartInfo.UseShellExecute = false;
			steamProcess.Start();


			//等待启动，超过时间则认为未成功
			await Task.Delay(2000);
			long waitSeconds = 20;
			while (waitSeconds < 100)
			{
				await Task.Delay(100);
				waitSeconds++;
				if (startSteamTimesNormal != lastStartSteamTimes)
					break;//启动已经成功则不再等待
			}
			await Task.Delay(1000);
			if (startSteamTimesNormal == lastStartSteamTimes)
			{
				await Task.Delay(50);
				lock (this)
				{
					CancelWait = true;
				}
				OutAPI.PrintLog("Start Steam without inject : fail");
			}
			else
			{
				OutAPI.PrintLog("Start Steam without inject : success");
			}
			lastStartType = 0;
		}
		private async Task StartSteamUnlock()
		{
			lastStartType = 1;
			bool isNoCheckedGame = false;
			int nowStartSteamTimes = startSteamTimes;
			int nowStartSteamTimesNormal = startSteamTimesNormal;
			int nowStartId;
			lock (lastStartLock)
			{
				nowStartId = lastStartId;
				lastStartId++;
			}
			try
			{
				DataSystem.Instance.SaveData();
				OutAPI.PrintLog(DataSystem.Instance.ToJSON());
				OutAPI.PrintLog($"isLoaded = {DataSystem.isLoaded};isLoadedEnd = {DataSystem.isLoadedEnd};isError = {DataSystem.isError}");
				OutAPI.PrintLog("Task start.");
				if (!File.Exists(DataSystem.Instance.SteamPath))
				{
					StateToStartSteam();
					await Task.Delay(50);
					_ = OutAPI.MsgBox(LocalizationService.GetString("Dock_SteamPathError"));
					return;
				}
				KillSteam();
				//防止前一次kill不及时，略微延时
				await Task.Delay(500);
				DateTime beginTime = DateTime.Now;
				//解锁模式启动steam
				if (DataSystem.Instance.CheckedNum > 0)
				{
					OutAPI.PrintLog($"CheckedNum = {DataSystem.Instance.CheckedNum}");
					OutAPI.PrintLog("GreenLuma ready start.");
					//清理GreenLuma配置文件
					GLFileTools.DeleteGreenLumaConfig();
					await Task.Delay(50);
					//写入GreenLuma配置文件
					if (!GLFileTools.WriteGreenLumaConfig(DataSystem.Instance.SteamPath, DataSystem.Instance.SkipSteamUpdate))
					{
						StateToStartSteam();
						_ = OutAPI.MsgBox(LocalizationService.GetString("Dock_WriteFailed"));
						return;
					}
					await Task.Delay(50);
					//检测GreenLuma完整性
					if (!GLFileTools.IsGreenLumaReady())
					{
						//GLFileTools.DeleteGreenLumaConfig();
						StateToStartSteam();
						await Task.Delay(50);
						_ = OutAPI.MsgBox(LocalizationService.GetString("Dock_FileMissing"));
						return;
					}
					//throw new Exception();//测试模拟异常
					//启动GreenLuma
					OutAPI.PrintLog("GreenLuma start.");
					int exitCode;
					bool withBak = DataSystem.Instance.StartWithBak;
					bool withAdmin = DataSystem.Instance.RunSteamWithAdmin;
					if (withBak)
					{
						exitCode = GLFileTools.StartGreenLuma_Bak(withAdmin);
					}
					else
					{
						exitCode = GLFileTools.StartGreenLuma(withAdmin);
					}
					OutAPI.PrintLog("Exit " + exitCode);
					if (nowStartId + 1 != lastStartId)
					{
						//只处理最新的启动的返回值分析
						return;
					}
					await Task.Delay(5000);
					//返回值分析
					bool exitCodeIgnore = false;
					string? errStr = "";
					//_ = OutAPI.MsgBox(exitCode.ToString());
					var notStart = startSteamTimes == nowStartSteamTimes && startSteamTimesNormal == nowStartSteamTimesNormal;
					//对已知返回值分析
					if (exitCode == -1073741510 || exitCode == -1)
					{
						//用户手动关闭 或者 kill
						return;
					}
					if (notStart)
					{
						if (retValueNeedHandle.TryGetValue(exitCode, out var reason))
						{
							//对已知普通返回值分析
							_ = OutAPI.MsgBox(reason);
							exitCodeIgnore = true;
						}
						if (exitCode == -1073741515)
						{
							//对已知运行库缺失返回值分析
							_ = OutAPI.MsgBox(LocalizationService.GetString("Dock_VCRedistMissing"));
							_ = Task.Run(() =>
							{
								//点击确定打开
								if (MessageBox.Show(LocalizationService.GetString("Dock_OpenVCRedistUrl"), LocalizationService.GetString("Dock_DownloadPrompt"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
								{
									OutAPI.OpenInBrowser("https://download.microsoft.com/download/9/3/F/93FCF1E7-E6A4-478B-96E7-D4B285925B00/vc_redist.x86.exe");
								}
							});
							exitCodeIgnore = true;
						}
						//未知返回值，转而处理stderr通道的错误信息
						if (exitCode != 0 && !exitCodeIgnore)
						{
							try
							{
								if (File.Exists(GLFileTools.DLLInjectorLogErrTxt))
									errStr = File.ReadAllText(GLFileTools.DLLInjectorLogErrTxt).Trim();
							}
							catch { }
							if (!DataSystem.Instance.HaveTriedBak && withBak != true && errStr == "Access is denied.")
							{
								DataSystem.Instance.StartWithBak = true;
								DataSystem.Instance.HaveTriedBak = true;
								OutAPI.MsgBox(LocalizationService.GetString("Dock_TryCompatMode")).Wait();
								//清理GreenLuma配置文件
								GLFileTools.DeleteGreenLumaConfig();
								//重新写入GreenLuma配置文件
								if (!GLFileTools.WriteGreenLumaConfig(DataSystem.Instance.SteamPath, DataSystem.Instance.SkipSteamUpdate))
								{
									StateToStartSteam();
									_ = OutAPI.MsgBox(LocalizationService.GetString("Dock_WriteFailed"));
									return;
								}
								;
								//备用方式启动
								exitCode = GLFileTools.StartGreenLuma_Bak(withAdmin);
								OutAPI.PrintLog("Bak First Exit " + exitCode);
								errStr = null;
							}
						}
					}
					//等待启动，超过时间则认为未成功
					long waitSeconds = 50;//前面等了5秒
					while (waitSeconds < 200)
					{
						await Task.Delay(100);
						waitSeconds++;
						if (startSteamTimes != nowStartSteamTimes)
							break;//启动已经成功则不再等待
						if (waitSeconds >= 130 && exitCodeIgnore)
							break;//已经识别出错误则不再等待
					}
					OutAPI.PrintLog($"Wait time finish. (After {waitSeconds / 10.0} seconds)");
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
					if (!File.Exists(GLFileTools.GreenLumaDllPath))
					{
						OutAPI.PrintLog("dll lost");
						fileLost = true;
					}
					if (fileLost)
					{
						_ = OutAPI.MsgBox(LocalizationService.GetString("Dock_TempFileLost"));

						if (DataSystem.Instance.LanguageCode.StartsWith("zh-"))
						{
							_ = Task.Run(() =>
							{
								//点击确定打开
								if (MessageBox.Show(LocalizationService.GetString("Dock_OpenHuorongUrl"), LocalizationService.GetString("Dock_DownloadPrompt"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
								{
									OutAPI.OpenInBrowser("https://www.huorong.cn/person5.html");
								}
							});
						}
						exitCodeIgnore = true;
					}
					//读取错误信息
					try
					{
						if (string.IsNullOrEmpty(errStr) && File.Exists(GLFileTools.DLLInjectorLogErrTxt))
							errStr = File.ReadAllText(GLFileTools.DLLInjectorLogErrTxt).Trim();
					}
					catch { }
					OutAPI.PrintLog($"{{ exitCodeIgnore({exitCodeIgnore}) beforeTimes({nowStartSteamTimes}) nowTimes({startSteamTimes}) errStr({errStr ?? "null"}) }}");
					//返回值异常 或是 到时间了还是没成功启动(有异常)
					if (!exitCodeIgnore && (exitCode != 0 || (startSteamTimes == nowStartSteamTimes && errStr != null && errStr.Length > 0)))
					{
						string errmsg = LocalizationService.GetString("Dock_LaunchFailedBase");
						if (!string.IsNullOrEmpty(errStr))
							errmsg = string.Format(LocalizationService.GetString("Dock_LaunchFailedFormat"), errStr);
						_ = Task.Run(async () =>
						{
							await OutAPI.MsgBox(errmsg);

							if (errStr == "The system cannot execute the specified program.")
							{
								await OutAPI.MsgBox(LocalizationService.GetString("Dock_CheckFAQ"));
							}
						});
					}
					else
					{
						OutAPI.PrintLog($"Skip MsgBox");
					}
				}
				else
				{
					OutAPI.PrintLog("checkednum<=0");
					_ = OutAPI.MsgBox(LocalizationService.GetString("Dock_NoGamesSelected"));
					isNoCheckedGame = true;
				}

			}
			catch (Exception e)
			{
				_ = Task.Run(async () =>
				{
					await OutAPI.MsgBox(e.Message);
					if (e.StackTrace is not null)
						await OutAPI.MsgBox(e.StackTrace);
				});
			}
			await Task.Delay(5000);
			if (startSteamTimes == nowStartSteamTimes)//buttonState == "Disable"
			{
				if (isNoCheckedGame)
				{
					////普通模式启动steam
					//var steamProcess = new Process();
					//steamProcess.StartInfo.FileName = DataSystem.Instance.SteamPath;
					////steamProcess.StartInfo.Arguments = ;
					//steamProcess.Start();
				}
				else
				{
					//尝试发送日志
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
						if (File.Exists(GLFileTools.GreenLumaLogTxt))
						{
							string logStr = File.ReadAllText(GLFileTools.GreenLumaLogTxt);
							data += "-----[GL_log.log]-----\n" + logStr + "\n";
						}
						string dataB64 = Base64.Base64Encode(Encoding.UTF8, data);
						dataB64 = HttpUtility.UrlEncode(dataB64);
						Dictionary<string, string> dic = new()
						{
							{ "logString", dataB64  ?? ""}
						};
						//发送日志
						OutAPI.Post(SteamWebData.LogUploadAddress, dic);
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
						OutAPI.Post(SteamWebData.LogUploadAddress, dic);
					}
				}
				await Task.Delay(50);
				lock (this)
				{
					CancelWait = true;
				}
			}

			lastStartType = 0;
		}
		private Dictionary<int, string> retValueNeedHandle => new()
		{
			{ 2048, LocalizationService.GetString("Dock_InjectorBlockedByAV")},
			{ 2049, LocalizationService.GetString("Dock_InjectorCrashed")},
			{ -10010, LocalizationService.GetString("Dock_InjectorUnknownError")},
			{ -10020, LocalizationService.GetString("Dock_InjectorStartFileFailed")},
			{ -10030, LocalizationService.GetString("Dock_InjectorDllReadFailed")},
			{ -10040, LocalizationService.GetString("Dock_InjectorSteamNotFound")},
			{ -10050, LocalizationService.GetString("Dock_InjectorConfigMissing")},
			{ -10100, LocalizationService.GetString("Dock_InjectorEndFileFailed")}
		};
		private void KillSteam()
		{
			//如果有残留注入器，就关闭进程(防止出问题了没退出)
			var injectorProcesses = Process.GetProcessesByName("spcrun");
			if (injectorProcesses.Length > 0)
			{
				foreach (var process in injectorProcesses)
				{
					process.Kill(true);
				}
			}
			var injectorProcesses1 = Process.GetProcessesByName("DLLInjector");
			if (injectorProcesses1.Length > 0)
			{
				foreach (var process in injectorProcesses1)
				{
					process.Kill(true);
				}
			}
			var injectorProcesses2 = Process.GetProcessesByName("DLLInjector_bak");
			if (injectorProcesses2.Length > 0)
			{
				foreach (var process in injectorProcesses2)
				{
					process.Kill(true);
				}
			}
			//关闭Steam
			if (steamProcesses is null) return;
			foreach (var process in steamProcesses)
			{
				process.Kill(true);
			}
		}
		public void StateToDisable()
		{
			buttonState = "Disable";
			StartButtonColor = darkStartButtonColor;
			StartButtonContent = darkStartButtonContent;
			LoadingBarEcho = Visibility.Visible;
			IsExpanded = false;
			OnPropertyChanged(nameof(NormalStartButtonVisibility));
		}
		public void StateToStartSteam()
		{
			buttonState = "StartSteam";
			StartButtonColor = defStartButtonColor;
			StartButtonContent = defStartButtonContent;
			LoadingBarEcho = Visibility.Hidden;
			IsExpanded = false;
			OnPropertyChanged(nameof(NormalStartButtonVisibility));
		}
		public void StateToCloseSteam()
		{
			buttonState = "CloseSteam";
			StartButtonColor = closeStartButtonColor;
			StartButtonContent = closeStartButtonContent;
			LoadingBarEcho = Visibility.Hidden;
			IsExpanded = true;
			OnPropertyChanged(nameof(NormalStartButtonVisibility));
		}
		private volatile int startSteamTimesNormal = 0;
		private volatile int startSteamTimes = 0;
		private Process[]? steamProcesses;
		private void UpdateSteamState()
		{
			while (true)
			{
				steamProcesses = Process.GetProcessesByName("steam");//获取指定的进程名   
				if (steamProcesses.Length > 0) //如果可以获取到知道的进程名则说明已经启动
				{
					if (buttonState != "CloseSteam")
					{
						if (lastStartType < 0)
						{
							startSteamTimesNormal++;
						}
						if (lastStartType > 0)
						{
							//记录本次运行启动次数
							startSteamTimes++;
							//记录总启动次数
							DataSystem.Instance.StartSuccessTimes++;
						}
					}
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
		private bool isExpanded = true;
		public bool IsExpanded
		{
			get => isExpanded;
			set
			{
				if (isExpanded != value)
				{
					isExpanded = value;
					ExecuteGridAnimation(value);
					OnPropertyChanged(nameof(IsExpanded));
				}
			}
		}
		private void ExecuteGridAnimation(bool expand)
		{
			Application.Current.Dispatcher.Invoke((Action)delegate ()
			{
				VisualStateManager.GoToElementState(windowFrom.RestartContainer, expand ? "RestartContainerExpanded" : "RestartContainerCollapsed", true);
			});
		}
	}
}
