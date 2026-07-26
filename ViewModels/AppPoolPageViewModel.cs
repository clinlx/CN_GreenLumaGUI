using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.Models;
using CN_GreenLumaGUI.Pages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace CN_GreenLumaGUI.ViewModels
{
	public class AppPoolPageViewModel : ObservableObject
	{
		/// <summary>筛选模式</summary>
		public const int FilterAll = 0;
		public const int FilterEnabledOnly = 1;
		public const int FilterDisabledOnly = 2;

		readonly AppPoolPage page;
		public AppPoolPageViewModel(AppPoolPage page)
		{
			this.page = page;
			addAppIdText = "";
			AddAppCmd = new RelayCommand(AddApp);
			ShowMappingCmd = new RelayCommand(ShowMapping);
			itemList = new ObservableCollection<AppPoolItem>();
			ReloadList();

			WeakReferenceMessenger.Default.Register<AppPoolChangedMessage>(this, (r, m) =>
			{
				//用 BeginInvoke 延后刷新：勾选框自身触发的变化不能在它的事件里清空列表
				Application.Current?.Dispatcher.BeginInvoke(ReloadList);
			});
			WeakReferenceMessenger.Default.Register<ConfigChangedMessage>(this, (r, m) =>
			{
				if (m.kind == nameof(DataSystem.Instance.LanguageCode))
				{
					foreach (var item in itemList)
						item.RefreshLanguage();
					OnPropertyChanged(nameof(FilterOptions));
					OnPropertyChanged(nameof(CountText));
				}
			});
		}

		private void ReloadList()
		{
			itemList.Clear();
			foreach (var (id, isBuiltIn, isDisabled) in AppPoolSystem.Instance.GetAllItems())
			{
				if (isBuiltIn && !ShowBuiltIn) continue;
				if (FilterMode == FilterEnabledOnly && isDisabled) continue;
				if (FilterMode == FilterDisabledOnly && !isDisabled) continue;
				itemList.Add(new AppPoolItem(id, isBuiltIn, isDisabled));
			}
			OnPropertyChanged(nameof(ItemList));
			OnPropertyChanged(nameof(CountText));
			OnPropertyChanged(nameof(PageEndText));
		}

		//Bindings
		private readonly ObservableCollection<AppPoolItem> itemList;
		public ObservableCollection<AppPoolItem> ItemList => itemList;

		private bool showBuiltIn;
		/// <summary>显示内置池</summary>
		public bool ShowBuiltIn
		{
			get => showBuiltIn;
			set
			{
				if (showBuiltIn == value) return;
				showBuiltIn = value;
				OnPropertyChanged();
				ReloadList();
			}
		}

		private int filterMode = FilterAll;
		public int FilterMode
		{
			get => filterMode;
			set
			{
				if (filterMode == value) return;
				filterMode = value;
				OnPropertyChanged();
				ReloadList();
			}
		}

		public List<string> FilterOptions => new()
		{
			LocalizationService.GetString("AppPool_FilterAll"),
			LocalizationService.GetString("AppPool_FilterEnabledOnly"),
			LocalizationService.GetString("AppPool_FilterDisabledOnly"),
		};

		private string addAppIdText;
		public string AddAppIdText
		{
			get => addAppIdText;
			set
			{
				addAppIdText = value;
				OnPropertyChanged();
			}
		}

		/// <summary>当前可用数量(即解锁上限)</summary>
		public string CountText => string.Format(LocalizationService.GetString("AppPool_CountFormat"),
			AppPoolSystem.Instance.AvailableCount, itemList.Count);

		public string PageEndText => itemList.Count == 0
			? LocalizationService.GetString("AppPool_EmptyPrompt")
			: LocalizationService.GetString("AppPool_NoMore");

		public string ScrollBarEchoState => DataSystem.Instance.ScrollBarEcho ? "Visible" : "Hidden";

		//Commands
		public RelayCommand AddAppCmd { get; }
		public RelayCommand ShowMappingCmd { get; }

		private Windows.InformWindow? mappingWindow;
		/// <summary>弹窗展示当前 AppList.ini 中“池app → 解锁项”的对应关系</summary>
		private void ShowMapping()
		{
			try
			{
				var mapping = AppPoolSystem.BuildMapping(out bool overflow);
				var lines = new List<TextItemModel>
				{
					new(LocalizationService.GetString("AppPool_MapTitle"), 16, "Bold", "Black"),
					new(LocalizationService.GetString("AppPool_MapHint"), 12, "Thin", "Gray"),
					new(""),
				};
				if (mapping.Count == 0)
				{
					lines.Add(new(LocalizationService.GetString("AppPool_MapEmpty"), 14, "Normal", "Gray"));
				}
				else
				{
					foreach (var e in mapping)
					{
						// 游戏顶格，DLC 缩进
						var text = $"{(e.IsDlc ? "    " : "")}{e.PoolAppId} → {e.Name}({e.AppId})";
						lines.Add(new(text, 14, e.IsDlc ? "Thin" : "Normal", e.IsDlc ? "Gray" : "Black"));
					}
					lines.Add(new(""));
					lines.Add(new(string.Format(LocalizationService.GetString("AppPool_MapCountFormat"), mapping.Count), 12, "Thin", "Gray"));
				}
				if (overflow)
					lines.Add(new(LocalizationService.GetString("AppPool_MapOverflow"), 14, "Bold", "Red"));

				if (mappingWindow is null || mappingWindow.IsClosed)
				{
					mappingWindow = new Windows.InformWindow(LocalizationService.GetString("AppPool_MapWindowTitle"), lines);
					// 设为主窗口的子窗口，主窗口关闭时它会一起关闭
					var owner = Window.GetWindow(page);
					if (owner is not null)
						mappingWindow.Owner = owner;
					else
						mappingWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
				}
				if (!mappingWindow.IsVisible)
					mappingWindow.Show();
				else
					mappingWindow.Close();
			}
			catch { }
		}

		private void AddApp()
		{
			var text = (AddAppIdText ?? "").Trim();
			if (text.Length == 0)
			{
				ManagerViewModel.Inform(LocalizationService.GetString("AppPool_InputEmpty"));
				return;
			}
			if (!long.TryParse(text, out long id) || id <= 0)
			{
				ManagerViewModel.Inform(LocalizationService.GetString("AppPool_InvalidAppId"));
				return;
			}
			if (!AppPoolSystem.Instance.AddApp(id))
			{
				ManagerViewModel.Inform(LocalizationService.GetString("AppPool_AlreadyExists"));
				return;
			}
			AddAppIdText = "";
			ManagerViewModel.Inform(LocalizationService.GetString("AppPool_Added"));
		}
	}
}
