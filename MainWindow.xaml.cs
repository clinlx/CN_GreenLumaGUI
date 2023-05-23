using CN_GreenLumaGUI.tools;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace CN_GreenLumaGUI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
		}
		private async void Terminar()
		{
			Hide();
			try
			{
				#region LoadingWindow
				//打开读取动画窗口
				//var loadingWindow = new LoadingWindow();
				//loadingWindow.Show();

				//耗时操作

				//结束读取动画窗口
				//loadingWindow.Close();
				#endregion

				#region ManagerWindow
				//打开管理窗口并等到其结束
				await ShowPopup(new ManagerWindow());
				#endregion
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			finally
			{
				GLFileTools.DeleteGreenLumaConfig();
				DataSystem.Instance.SaveData();
			}
			//this.Show();
			Close();
		}
		private void Window_Activated(object? sender, EventArgs e)
		{
			Activated -= Window_Activated;
			Terminar();
		}
		private static Task ShowPopup<TPopup>(TPopup popup) where TPopup : Window
		{
			var task = new TaskCompletionSource<object>();
			popup.Owner = Application.Current.MainWindow;
			popup.Closed += (s, a) => task.SetResult(new());
			popup.Show();
			popup.Focus();
			return task.Task;
		}

	}
}
