using CN_GreenLumaGUI.tools;
using System;
using System.IO;

namespace CN_GreenLumaGUI
{
	public class Program
	{

#if DEBUG
		public static readonly bool isDebug = true;
#else
		public static readonly bool isDebug = false;
#endif

		static Program()
		{
		}

		[STAThread]
		public static void Main(string[] args)
		{
			if (args.Length > 0) return;
			//运行检测
			#region 判断相同进程是否已启动,如果是则将已有进程设置到前台
			nint hwnd = OutAPI.FindWindow(null, "CN_GreenLumaGUI_ManagerWindow");
			if (hwnd != 0)
			{
				OutAPI.SetForegroundWindow(hwnd);//将已有进程设置到前台
				return;//关闭
			}
			//string fileName = Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "CN_GreenLumaGUI";
			//Process[] myProcesses = Process.GetProcessesByName(fileName);//获取指定的进程名   
			//if (myProcesses.Length > 1) //如果可以获取到知道的进程名则说明已经启动
			//{
			//	//程序已启动
			//	OutAPI.SetForegroundWindow(myProcesses[0].MainWindowHandle);//将已有进程设置到前台
			//	return;//关闭
			//}
			#endregion

			try
			{
				if (File.Exists("./CN_GreenLumaGUI(English).pdb"))
				{
					File.WriteAllText("Version(English).txt", "CN_GreenLumaGUI(English)  v" + Version);
				}
			}
			catch
			{ }

			//创建目录
			if (!Directory.Exists(OutAPI.TempDir))
			{
				Directory.CreateDirectory(OutAPI.TempDir);
			}

			//获取信息
			_ = SteamWebData.Instance.UpdateLastVersion();

			//启动WPF-App
			App app = new();
			app.InitializeComponent();
			app.Run();
		}

		private static long debugId = 0;
		public static string DebugFileId { get { return $".\\LOG\\{debugId++}.txt"; } }
		private readonly static string version = System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "none";
		public static string Version { get { return version; } }

		public static bool NeedUpdate { get; set; } = false;

		public static int[] VersionCut(string version)
		{
			if (version[0] == 'v')
				version = version[1..];
			string[] cuts = version.Split('.');
			int[] result = new int[cuts.Length];
			for (int i = 0; i < cuts.Length; i++)
			{
				result[i] = int.Parse(cuts[i]);
			}
			return result;
		}

	}
}
