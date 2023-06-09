﻿using System;
using System.Diagnostics;
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
			Process[] myProcesses2 = Process.GetProcessesByName("CN_GreenLumaGUI");//获取指定的进程名   
			if (myProcesses2.Length > 1) //如果可以获取到知道的进程名则说明已经启动
			{
				//程序已启动
				tools.OutAPI.SetForegroundWindow(myProcesses2[0].MainWindowHandle);//将已有进程设置到前台
				return;//关闭
			}
			#endregion

			if (isDebug)
			{
				File.WriteAllText("Version.txt", Version);
			}

			//启动WPF-App
			App app = new();
			app.InitializeComponent();
			app.Run();
		}

		private readonly static string version = System.Reflection.Assembly.GetExecutingAssembly()?.GetName()?.Version?.ToString() ?? "none";
		public static string Version { get { return version; } }

	}
}
