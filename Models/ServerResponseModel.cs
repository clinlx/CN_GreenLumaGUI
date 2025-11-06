using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CN_GreenLumaGUI.Models
{
    public record ApiSimpleDepot(uint Id, string Gid, uint Parent)
    {
        public AppModelLite ToLite(string name)
        {
            return new AppModelLite(name, Id, "", "", false, Parent);
        }
    }

    public record ApiSimpleApp(uint Id, string Type, string? Name, string TempName, List<uint> ListOfDlc, List<ApiSimpleDepot> Depots, uint? Parent = null, uint? DlcForAppid = null, bool IsTempData = false, string? ChineseName = null)
    {
        public AppModelLite ToLite()
        {
            return new AppModelLite(ChineseName ?? Name ?? TempName, Id, "", $"https://store.steampowered.com/app/{Id}", Type != "DLC", Parent ?? 0);
        }
    }

    public record ApiCacheLine(ApiSimpleApp? App, DateTime LastUpdateTime)
    {
        public static ApiCacheLine Create(ApiCacheLine line) => new(line.App, DateTime.Now);
        public static ApiCacheLine Create(ApiSimpleApp? app) => new(app, DateTime.Now);
        public bool IsOutDate()
        {
            return DateTime.Now - LastUpdateTime > TimeSpan.FromDays(1);
        }
    }
}
