using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
		void DockInit()
		{
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
				_ = Task.Factory.StartNew(() => messageQueue.Enqueue(message));
			}
		}

		//Binding
		private readonly int maxUnlockNum = 128; //GreenLuma最大支持到137的上限
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
						MessageBox.Show("解锁数量超限。");
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
			if (!File.Exists(DataSystem.Instance.SteamPath))
			{
				StateToStartSteam();
				MessageBox.Show("steam路径错误！");
				return;
			}
			//防止前一次kill不及时，略微延时
			await Task.Delay(500);
			//解锁模式启动steam
			if (DataSystem.Instance.CheckedNum > 0)
			{
				//写入GreenLuma配置文件
				GLFileTools.WirteGreenLumaConfig(DataSystem.Instance.SteamPath);
				//检测GreenLuma完整性
				if (!GLFileTools.IsGreenLumaReady())
				{
					GLFileTools.DeleteGreenLumaConfig();
					StateToStartSteam();
					MessageBox.Show("文件缺失！");
					return;
				}
				//启动GreenLuma
				GLFileTools.StartGreenLuma();
				await Task.Delay(10000);
				//清理GreenLuma配置文件
				GLFileTools.DeleteGreenLumaConfig();
			}
			if (buttonState == "Disable")
			{
				//普通模式启动steam
				var steamProcess = new Process();
				steamProcess.StartInfo.FileName = DataSystem.Instance.SteamPath;
				//steamProcess.StartInfo.Arguments = ;
				steamProcess.Start();
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
				}
				Thread.Sleep(1000);
			}
		}
	}
}
