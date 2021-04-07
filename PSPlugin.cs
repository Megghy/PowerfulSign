using System;
using System.Collections.Generic;
using System.Linq;
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
        public override Version Version => new Version(1, 0);
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
            Main.sign = new Sign[1000];
            DB.TryCreateTable();
            ServerApi.Hooks.NetGetData.Register(this, Net.OnGetData);
            //ServerApi.Hooks.NetSendData.Register(this, Net.OnSendData);
            ServerApi.Hooks.NetGreetPlayer.Register(this, (GreetPlayerEventArgs g) =>
            {
                TShock.Players[g.Who].SetData<PSPlayer>("PSPlayer", new PSPlayer(TShock.Players[g.Who]));
            });
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
                    case "1":
                        plr.SendSignDataInCircle(PSPlugin.Config.RefreshRadius);
                        break;
                    case "check":
                        int num = 0;
                        SignList.ToList().Where(s => s.X >= 0 && s.X < Main.maxTilesX && s.Y >= 0 && s.Y < Main.maxTilesY && !Main.tileSign[Main.tile[s.X, s.Y].type]).ForEach(s => { SignList.Remove(s); num++; });
                        plr.SendInfoMessage($"移除 {num} 个无效标牌数据.");
                        break;
                }
            }
            else
            {

            }
        }
    }
}
