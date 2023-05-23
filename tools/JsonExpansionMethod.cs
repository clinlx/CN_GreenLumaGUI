using Newtonsoft.Json;

namespace CN_GreenLumaGUI.tools
{
	public static class JsonExpansionMethod
	{
		#region objectJSONExpansionMethod

		/// <summary>
		/// 将任意类的实例转换成JSON字符串
		/// </summary>
		public static string ToJSON(this object obj, bool indented = false)
		{
			return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
		}
		/// <summary>
		/// 将JSON字符串转换成指定类的实例
		/// </summary>
		public static T? FromJSON<T>(this string str)
		{
			return JsonConvert.DeserializeObject<T>(str);
		}

		#endregion

	}
}
