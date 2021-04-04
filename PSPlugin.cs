using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

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
            TShockAPI.Hooks.GeneralHooks.ReloadEvent += Config.Load;
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
                }
            }
            else
            {

            }
        }
    }
    public class PSSign
    {
        public PSSign(int owner, List<int> friends, int x, int y, string text, int id = -1, bool canedit = false)
        {
            ID = id;
            Owner = owner;
            Friends = friends;
            X = x;
            Y = y;
            Text = text;
            CanEdit = canedit;

            ProcessText();
        }
        void ProcessText()
        {
            if (Text.StartsWith("[") && Text.EndsWith("]") && Text.Contains("\n"))
            {
                var lines = Text.Split("\n");
                var origintype = lines[0].SearchString("[", "]").ToLower();
                switch (origintype)
                {
                    case "shop":
                        Type = Types.Shop;
                        _CombatText = "看啥看";
                        break;
                    case "command":
                        Type = Types.Command;

                        break;
                }
            }
            else Type = Types.Normal;
        }
        public enum Types
        {
            Normal,
            Shop,
            Command,
        }
        public Types Type { get; set; }
        public void Update()
        {
            DB.UpdateSign(this);
            SendToAll();
        }
        public void SendToAll()
        {
            var t = this;
            TShock.Players.Where(p => p != null).ForEach(p => p.SendSignData(t, false, 0));
        }
        public int ID { get; set; }
        public int Owner { get; set; }
        public List<int> Friends { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Text { get; set; }
        public bool CanEdit { get; set; }
        public string _PromptText = string.Empty;
        public string PromptText
        {
            get
            {
                return _PromptText == string.Empty ? Type switch
                {
                    Types.Normal => PSPlugin.Config.DefaultPromptText.Normal == "" ? Text : PSPlugin.Config.DefaultPromptText.Normal,
                    Types.Shop => PSPlugin.Config.DefaultPromptText.Shop,
                    Types.Command => PSPlugin.Config.DefaultPromptText.Command,
                    _ => Text
                } : _PromptText;
            }
        }
        public string _CombatText = string.Empty;
        public string CombatText { get {
                return _CombatText == string.Empty ? Type switch
                {
                    Types.Normal => PSPlugin.Config.DefaultCombatText.Normal == "" ? Text : PSPlugin.Config.DefaultCombatText.Normal,
                    Types.Shop => PSPlugin.Config.DefaultCombatText.Shop,
                    Types.Command => PSPlugin.Config.DefaultCombatText.Command,
                    _ => Text
                } : _CombatText;
            } }
        public List<PSSign_Command> Commands { get; set; }
    }
    public struct PSSign_Command
    {
        public PSSign_Command(string command, int cooldown, int cost = 0)
        {
            Command = command;
            CoolDown = cooldown;
            Cost = cost;
        }
        public string Command { get; set; }
        public int CoolDown { get; set; }
        public int Cost { get; set; }
    }
}
