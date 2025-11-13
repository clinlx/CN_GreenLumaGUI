using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CN_GreenLumaGUI.Pages
{
	/// <summary>
	/// SearchPage.xaml 的交互逻辑
	/// </summary>
	public partial class SearchPage : Page
	{
		private bool isMessengerRegistered;

		public SearchPage()
		{
			InitializeComponent();

			Loaded += SearchPage_Loaded;
			Unloaded += SearchPage_Unloaded;
		}

		private void SearchPage_Loaded(object sender, RoutedEventArgs e)
		{
			if (isMessengerRegistered)
			{
				RefreshDataGridColumns();
				return;
			}

			OutAPI.PrintLog("[SearchPage] Loaded, registering ConfigChangedMessage listener");
			WeakReferenceMessenger.Default.Register<SearchPage, ConfigChangedMessage>(this, OnConfigChangedMessage);
			isMessengerRegistered = true;
			RefreshDataGridColumns();
		}

		private void SearchPage_Unloaded(object sender, RoutedEventArgs e)
		{
			if (!isMessengerRegistered)
			{
				return;
			}

			OutAPI.PrintLog("[SearchPage] Unloaded, unregistering ConfigChangedMessage listener");
			WeakReferenceMessenger.Default.Unregister<ConfigChangedMessage>(this);
			isMessengerRegistered = false;
		}

		private void OnConfigChangedMessage(SearchPage recipient, ConfigChangedMessage message)
		{
			OutAPI.PrintLog($"[SearchPage] ConfigChangedMessage received: kind={message.kind}");

			if (message.kind == nameof(DataSystem.LanguageCode))
			{
				OutAPI.PrintLog("[SearchPage] LanguageCode change detected, refreshing columns");

				// 確保在 UI 線程上執行
				Dispatcher.Invoke(() =>
				{
					RefreshDataGridColumns();
				});
			}
		}

		/// <summary>
		/// 刷新 DataGrid 的欄位標題，強制重新評估 DynamicResource 綁定
		/// </summary>
		private void RefreshDataGridColumns()
		{
			try
			{
				OutAPI.PrintLog($"[SearchPage] RefreshDataGridColumns called, Columns.Count = {searchResList.Columns.Count}");

				// 建立欄位索引與資源 key 的映射，使用 Header 當前值來識別欄位
				var columnResourceKeys = new Dictionary<int, string>
				{
					{ 0, "Search_IndexColumn" },
					{ 1, "Search_CoverColumn" },
					{ 2, "Search_NameColumn" },
					{ 3, "Search_RatingColumn" },
					{ 4, "Search_StorePageColumn" },
					{ 5, "Search_AddToListColumn" }
				};

				// 更新每個欄位的 Header
				for (int i = 0; i < searchResList.Columns.Count && i < columnResourceKeys.Count; i++)
				{
					if (columnResourceKeys.TryGetValue(i, out var resourceKey))
					{
						var headerText = LocalizationService.GetString(resourceKey);
						OutAPI.PrintLog($"[SearchPage] Column {i}: {resourceKey} -> {headerText}");
						searchResList.Columns[i].Header = headerText;
					}
				}
			}
			catch (System.Exception ex)
			{
				OutAPI.PrintLog($"[SearchPage] RefreshDataGridColumns error: {ex.Message}");
			}
		}
	}
}
