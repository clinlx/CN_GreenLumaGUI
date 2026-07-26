using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace CN_GreenLumaGUI.Models
{
	/// <summary>
	/// “用于替换的app池”中的一项
	/// </summary>
	public class AppPoolItem : ObservableObject
	{
		public AppPoolItem(long appId, bool isBuiltIn, bool isDisabled)
		{
			AppId = appId;
			IsBuiltIn = isBuiltIn;
			isDisabledValue = isDisabled;
			DeleteCmd = new RelayCommand(Delete);
		}

		public long AppId { get; }
		/// <summary>是否来自内置(默认)池</summary>
		public bool IsBuiltIn { get; }

		public string AppIdText => AppId.ToString();

		private bool isDisabledValue;
		public bool IsEnabled
		{
			get => !isDisabledValue;
			set
			{
				if (isDisabledValue == !value) return;
				isDisabledValue = !value;
				AppPoolSystem.Instance.SetDisabled(AppId, isDisabledValue);
				OnPropertyChanged();
				OnPropertyChanged(nameof(TextColor));
				OnPropertyChanged(nameof(SourceText));
			}
		}
		public bool IsDisabled => isDisabledValue;

		/// <summary>内置项显示为灰色，拓展项显示为常规色；被禁用时更淡</summary>
		public string TextColor
		{
			get
			{
				if (isDisabledValue) return IsBuiltIn ? "#BDBDBD" : "#E08A8A";
				return IsBuiltIn ? "#8A8A8A" : "#1976D2";
			}
		}

		public string SourceText => LocalizationService.GetString(IsBuiltIn ? "AppPool_SourceBuiltIn" : "AppPool_SourceExtend");

		/// <summary>只有拓展项可以删除</summary>
		public Visibility DeleteVisibility => IsBuiltIn ? Visibility.Collapsed : Visibility.Visible;

		public RelayCommand DeleteCmd { get; }
		private void Delete()
		{
			AppPoolSystem.Instance.RemoveApp(AppId);
		}

		public void RefreshLanguage()
		{
			OnPropertyChanged(nameof(SourceText));
		}
	}
}
