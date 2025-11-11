using CN_GreenLumaGUI.Messages;
using CN_GreenLumaGUI.tools;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Controls;

namespace CN_GreenLumaGUI.Pages
{
	/// <summary>
	/// SearchPage.xaml 的交互逻辑
	/// </summary>
	public partial class SearchPage : Page
	{
		public SearchPage()
		{
			InitializeComponent();

			// 監聽語言變更事件，手動刷新 DataGrid 欄位標題
			WeakReferenceMessenger.Default.Register<ConfigChangedMessage>(this, (r, m) =>
			{
				if (m.kind == nameof(DataSystem.Instance.LanguageCode))
				{
					RefreshDataGridColumns();
				}
			});
		}

		/// <summary>
		/// 刷新 DataGrid 的欄位標題，強制重新評估 DynamicResource 綁定
		/// </summary>
		private void RefreshDataGridColumns()
		{
			// 取得所有欄位的 DynamicResource key
			var columnHeaders = new[]
			{
				("Search_IndexColumn", 0),
				("Search_CoverColumn", 1),
				("Search_NameColumn", 2),
				("Search_RatingColumn", 3),
				("Search_StorePageColumn", 4),
				("Search_AddToListColumn", 5)
			};

			// 手動更新每個欄位的 Header
			foreach (var (resourceKey, columnIndex) in columnHeaders)
			{
				if (columnIndex < searchResList.Columns.Count)
				{
					var column = searchResList.Columns[columnIndex];
					// 從資源字典中取得新的文字
					if (Application.Current.TryFindResource(resourceKey) is string headerText)
					{
						column.Header = headerText;
					}
				}
			}
		}
	}
}
