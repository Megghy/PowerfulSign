using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;

namespace PowerfulSign
{
    [ApiVersion(2, 1)]
    public class PSPlugin : TerrariaPlugin
    {
        public PSPlugin(Main game) : base(game)
        {
        }
        public override string Name => "PowerfulSgin";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public override string Author => "Megghy";
        public override string Description => "强大的标牌增强插件.";
        #region 临时储存的各种数据
        public static Config Config = new Config();
        public static List<PSSign> SignList = new List<PSSign>();
        #endregion
        public override void Initialize()
        {
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            Config.Load();
        }
        public void OnPostInitialize(EventArgs args)
        {
            if (!ServerApi.Plugins.Where(p => p.Plugin.Name == "UnifiedEconomyFramework").Any())
            {
                TShock.Log.ConsoleError("未检测到 UnifiedEconomyFramework 前置插件, 部分功能将无法生效.");
            }
            DB.TryCreateTable();
            ServerApi.Hooks.NetGetData.Register(this, Net.OnGetData);
            //ServerApi.Hooks.NetSendData.Register(this, Net.OnSendData);
            ServerApi.Hooks.NetGreetPlayer.Register(this, (GreetPlayerEventArgs g) =>
            {
                if(!TShock.Players[g.Who].ContainsData("PSPlayer")) TShock.Players[g.Who].SetData<PSPlayer>("PSPlayer", new PSPlayer(TShock.Players[g.Who]));
            });
            ServerApi.Hooks.ServerLeave.Register(this, (LeaveEventArgs l) => TShock.Players[l.Who].RemoveData("PSPlayer"));
            GetDataHandlers.TileEdit += Net.OnTileEdit;
            GeneralHooks.ReloadEvent += (ReloadEventArgs r) => { Config.Load(); DB.GetAllSign(); };
            Commands.ChatCommands.Add(new Command("ps.use", OnCommand, new string[] { "ps", "标牌" }));
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
        public void OnCommand(CommandArgs args)
        {
            var plr = args.Player;
            var cmd = args.Parameters;
            if (cmd.Any())
            {
                switch (cmd[0])
                {
                    
                }
            }
            else
            {

            }
        }
    }
}
