using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace PowerfulSign
{
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
            Color = Color.White;

            ProcessText();
        }
        public bool ProcessText()
        {
            try
            {
                _PromptText = null;
                _CombatText = null;
                if (Text.Contains("\n"))
                {
                    var lines = Text.Split("\n");
                    if (lines[0].StartsWith("[") && lines[0].EndsWith("]"))
                    {
                        var maintype = lines[0].SearchString("[", "]").ToLower();
                        var owner = TShock.Players.FirstOrDefault(p => p != null && p.AccountEX().ID == Owner) ?? new TSPlayer(-1) { Account = this.Account ?? new UserAccount(), Group = TShock.Groups.GetGroupByName(Account.Group) };
                        switch (maintype)
                        {
                            case "shop":
                                if (owner.HasPermission("ps.use.shop"))
                                {
                                    if (lines.Count >= 2)
                                    {
                                        if (lines[1].ToLower() == "sell" || lines[1].ToLower() == "buy")
                                        {
                                            var tempShop = new PSSign_Shop();
                                            for (int i = 2; i < lines.Count; i++)
                                            {
                                                if (lines[i].Contains(":"))
                                                {
                                                    var key = lines[i].Split(':')[0];
                                                    var value = lines[i].Replace(key + ":", "");
                                                    key = key.ToLower();
                                                    switch (key)
                                                    {
                                                        case "item":
                                                            if (value.Contains(" "))
                                                            {
                                                                var temp_item = value.Split(' ');
                                                                if (temp_item.Count() >= 2)
                                                                {
                                                                    if (int.TryParse(temp_item[0], out int itemtype) && int.TryParse(temp_item[1], out int num) && int.TryParse(temp_item.Count() >= 3 ? temp_item[1] : "0", out int prefix))
                                                                    {
                                                                        tempShop.ItemType = itemtype;
                                                                        var tempitem = new Item();
                                                                        tempitem.SetDefaults(itemtype);
                                                                        if (tempitem.type == 0)
                                                                        {
                                                                            owner.SendErrorMessage($"[C/66D093:<PowerfulSign>] 无效的物品id.");
                                                                            Error = true;
                                                                            return false;
                                                                        }
                                                                        else if (num < 1)
                                                                        {
                                                                            owner.SendErrorMessage($"[C/66D093:<PowerfulSign>] 每次购买数量应大于零.");
                                                                            Error = true;
                                                                            return false;
                                                                        }
                                                                        else if (prefix != 0 && !tempitem.Prefix(prefix))
                                                                        {
                                                                            owner.SendErrorMessage($"[C/66D093:<PowerfulSign>] 物品前缀无效. 若无需设定物品前缀可只设定物品id及堆叠.");
                                                                            Error = true;
                                                                            return false;
                                                                        }
                                                                        tempitem.stack = num;
                                                                        tempitem.prefix = (byte)prefix;
                                                                        tempShop.Item = tempitem;
                                                                        tempShop.Stack = num;
                                                                    }
                                                                    else
                                                                    {
                                                                        owner.SendErrorMessage($"[C/66D093:<PowerfulSign>] item列格式错误. 应为 [c/6CCCA8:item:物品id 每次购买数量]");
                                                                        Error = true;
                                                                        return false;
                                                                    }
                                                                }

                                                            }
                                                            break;
                                                        case "canbuyset":
                                                            tempShop.CanBuySet = value.ToLower() == "true";
                                                            break;
                                                        case "unlimit":
                                                            if (owner.HasPermission("ps.admin.unlimit"))
                                                            {
                                                                tempShop.UnLimit = value.ToLower() == "true";
                                                            }
                                                            else
                                                            {
                                                                owner.SendInfoMessage($"[C/66D093:<PowerfulSign>] 你没有权限设定无限商店 <ps.admin.unlimit>, 此设置项将不会生效.");
                                                            }
                                                            break;
                                                        case "price":
                                                            if (int.TryParse(value, out int price))
                                                            {
                                                                tempShop.Price = price;
                                                            }
                                                            break;
                                                        case "combat":
                                                            if (owner.HasPermission("ps.use.combat"))
                                                            {
                                                                _CombatText = value;
                                                            }
                                                            else
                                                            {
                                                                owner.SendInfoMessage($"[C/66D093:<PowerfulSign>] 你没有权限自定义CombatText <ps.use.prompt>, 此设置项将不会生效.");
                                                            }
                                                            break;
                                                        case "prompt":
                                                            if (owner.HasPermission("ps.use.prompt"))
                                                            {
                                                                _PromptText = value;
                                                            }
                                                            else
                                                            {
                                                                owner.SendInfoMessage($"[C/66D093:<PowerfulSign>] 你没有权限自定义PromptText <ps.use.prompt>, 此设置项将不会生效.");
                                                            }
                                                            break;
                                                        case "color":
                                                            var color = System.Drawing.ColorTranslator.FromHtml("#FF0000");
                                                            if (System.Drawing.Color.Empty != color)
                                                            {
                                                                Color = new Color(color.R, color.G, color.B);
                                                            }
                                                            else
                                                            {
                                                                owner.SendErrorMessage($"[C/66D093:<PowerfulSign>] 无效的Hex颜色格式. 请搜索十六进制颜色.");
                                                                Error = true;
                                                                return false;
                                                            }
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    return false;
                                                }
                                            }
                                            if (tempShop.ItemType == 0)
                                            {
                                                owner.SendErrorMessage($"[C/66D093:<PowerfulSign>] 无效的商店格式. 缺少必要条件: 物品id.");
                                                Error = true;
                                                return false;
                                            }
                                            else if (tempShop.Stack == 0)
                                            {
                                                owner.SendErrorMessage($"[C/66D093:<PowerfulSign>] 无效的商店格式. 缺少必要条件: 物品数量.");
                                                Error = true;
                                                return false;
                                            }
                                            if (Utils.FindChestByGuessing(X + 2, Y) == -1)
                                            {
                                                owner.SendErrorMessage($"[C/66D093:<PowerfulSign>] 未能在标牌右侧找到箱子.");
                                                Error = true;
                                                return false;
                                            }
                                            tempShop.Type = lines[1].ToLower() == "sell" ? PSSign_Shop.SELL : PSSign_Shop.BUY;
                                            Shop = tempShop;
                                            Type = Types.Shop;
                                            Error = false;
                                            return true;
                                        }
                                        else
                                        {
                                            owner.SendInfoMessage($"[C/66D093:<PowerfulSign>] 无效的商店格式. 第二排应为 sell 或 buy.");
                                            Error = true;
                                        }
                                    }
                                    else
                                    {
                                        owner.SendInfoMessage($"[C/66D093:<PowerfulSign>] 无效的商店格式. 第二排应为 sell 或 buy.");
                                        Error = true;
                                    }
                                }
                                else
                                {
                                    owner.SendInfoMessage($"[C/66D093:<PowerfulSign>] 你没有权限创建标牌商店 <ps.use.shop>.");
                                    return false;
                                }
                                break;
                            case "command":
                                if (lines.Count >= 2)
                                {
                                    if (owner.HasPermission("ps.admin.command"))
                                    {
                                        Type = Types.Command;
                                        var tempCommand = new PSSign_Command(new List<string>(), 1000, 0);
                                        for (int i = 1; i < lines.Count; i++)
                                        {
                                            if (lines[i].StartsWith(TShock.Config.Settings.CommandSpecifier))
                                            {
                                                tempCommand.Commands.Add(lines[i].Remove(0, 1));
                                            }
                                            else if (lines[i].Contains(":"))
                                            {
                                                var key = lines[i].Split(':')[0];
                                                var value = lines[i].Replace(key + ":", "");
                                                key = key.ToLower();
                                                switch (key)
                                                {
                                                    case "cooldown":
                                                        if (long.TryParse(value, out long cooldown))
                                                        {
                                                            tempCommand.CoolDown = cooldown;
                                                        }
                                                        else
                                                        {
                                                            owner.SendErrorMessage($"[C/66D093:<PowerfulSign>] 冷却时间格式错误, 应为毫秒 (1秒 = 1000毫秒).");
                                                            Error = true;
                                                            return false;
                                                        }
                                                        break;
                                                    case "cost":
                                                        if (int.TryParse(value, out int cost))
                                                        {
                                                            tempCommand.Cost = cost;
                                                        }
                                                        else
                                                        {
                                                            owner.SendErrorMessage($"[C/66D093:<PowerfulSign>] 消耗货币数量格式错误.");
                                                            Error = true;
                                                            return false;
                                                        }
                                                        break;
                                                    case "color":
                                                        var color = System.Drawing.ColorTranslator.FromHtml("#FF0000");
                                                        if (System.Drawing.Color.Empty != color)
                                                        {
                                                            Color = new Color(color.R, color.G, color.B);
                                                        }
                                                        else
                                                        {
                                                            owner.SendErrorMessage($"[C/66D093:<PowerfulSign>] 无效的Hex颜色格式. 请搜索十六进制颜色.");
                                                            Error = true;
                                                            return false;
                                                        }
                                                        break;
                                                    case "type":
                                                        if (value.ToLower() == "close") {
                                                            tempCommand.Type = PSSign_Command.CLOST;
                                                        }
                                                        else if (value.ToLower() == "click")
                                                        {
                                                            tempCommand.Type = PSSign_Command.CLICK;
                                                        }
                                                        else
                                                        {
                                                            owner.SendErrorMessage($"[C/66D093:<PowerfulSign> 无效的标牌类型. 应为 click 或 close, 默认为click.");
                                                            Error = true;
                                                            return false;
                                                        }
                                                        break;
                                                    case "ignoreperms":
                                                    case "ignorepermission":
                                                        if (value.ToLower() == "true")
                                                        {
                                                            tempCommand.IgnorePermissions = true;
                                                        }
                                                        break;
                                                }
                                            }
                                            Command = tempCommand;
                                            Error = false;
                                            return true;
                                        }
                                    }
                                    else
                                    {
                                        owner.SendInfoMessage($"[C/66D093:<PowerfulSign>] 你没有权限设定命令标牌 <ps.admin.command>.");
                                        return false; 
                                    }
                                }
                                break;
                        }
                    }
                    else Type = Types.Normal;
                    SendToAll();
                }
                return false;
            }
            catch (Exception ex) { TShock.Log.ConsoleError(ex.Message); return false; }
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
            TShock.Players.Where(p => p != null).ForEach(p => p.SendSignDataDirect(t, false, 0));
        }
        public bool Error = false;
        public UserAccount Account => TShock.UserAccounts.GetUserAccountByID(Owner) ?? new UserAccount() { ID = -2, Name = "UnKnown" };
        public int ID { get; set; }
        public int Owner { get; set; }
        public List<int> Friends { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Color Color { get; set; }
        public string Text { get; set; }
        public bool CanEdit { get; set; }
        public string _PromptText;
        public string PromptText
        {
            get
            {
                return Type switch
                {
                    Types.Normal => (_PromptText ?? PSPlugin.Config.DefaultPromptText.Normal)
                    .Replace("\\n", "\n")
                    .Replace("{moneyname}", PSPlugin.Config.MoneyName)
                    .Replace("{text}", Text)
                    .Replace("{owner}", Account.Name)
                    ,

                    Types.Shop => (_PromptText ?? PSPlugin.Config.DefaultPromptText.Shop)
                    .Replace("\\n", "\n")
                    .Replace("{moneyname}", PSPlugin.Config.MoneyName)
                    .Replace("{text}", Text)
                    .Replace("{type}", Shop.Type == PSSign_Shop.SELL ? "出售" : "收购")
                    .Replace("{shop.name}", Shop.Item.Name)
                    .Replace("{shop.stack}", Shop.Stack.ToString())
                    .Replace("{shop.inventory}", Inventory.ToString())
                    .Replace("{shop.price}", Shop.Price.ToString())
                    .Replace("{shop.text}", Text)
                    .Replace("{shop.owner}", Account.Name)
                    ,

                    Types.Command => (_PromptText ?? PSPlugin.Config.DefaultPromptText.Command)
                    .Replace("\\n", "\n")
                    .Replace("{moneyname}", PSPlugin.Config.MoneyName)
                    .Replace("{text}", Text)
                    .Replace("{command.cost}", Command.Cost.ToString())
                    .Replace("{command.cooldown}", Command.CoolDown.ToString())
                    .Replace("{command.count}", Command.Commands.Count.ToString())
                    ,
                    _ => Text
                };
            }
        }
        public string _CombatText;
        public string CombatText
        {
            get
            {
                return Type switch
                {
                    Types.Normal => (_CombatText ?? PSPlugin.Config.DefaultCombatText.Normal)
                    .Replace("\\n", "\n")
                    .Replace("{text}", Text)
                    .Replace("{moneyname}", PSPlugin.Config.MoneyName)
                    .Replace("{owner}", Account.Name)
                    ,

                    Types.Shop => (_CombatText ?? PSPlugin.Config.DefaultCombatText.Shop)
                    .Replace("{type}", Shop.Type == PSSign_Shop.SELL ? "出售" : "收购")
                    .Replace("\\n", "\n")
                    .Replace("{moneyname}", PSPlugin.Config.MoneyName)
                    .Replace("{shop.name}", Shop.Item.Name)
                    .Replace("{shop.stack}", Shop.Stack.ToString())
                    .Replace("{shop.inventory}", Inventory.ToString())
                    .Replace("{shop.price}", Shop.Price.ToString())
                    .Replace("{shop.text}", Text)
                    .Replace("{shop.owner}", Account.Name)
                    ,

                    Types.Command => (_CombatText ?? PSPlugin.Config.DefaultCombatText.Command),
                    _ => Text
                };
            }
        }
        public PSSign_Command Command { get; set; }
        public PSSign_Shop Shop { get; set; }
        #region 箱子商店部分功能
        public int ChestID { get { return Utils.FindChestByGuessing(X + 2, Y); } }
        public int Inventory
        {
            get
            {
                if (Type != Types.Shop) return -1;
                if (ChestID != -1)
                {
                    var chest = Main.chest[ChestID];
                    var num = 0;
                    chest.item.ForEach(i =>
                    {
                        if (i.type == Shop.Item.type && (Shop.Prefix == 0 ? true : i.prefix == Shop.Prefix))
                        {
                            num += i.stack;
                        }
                    });
                    return num;
                }
                return 0;
            }
        }
        public int AviliableSlot
        {
            get
            {
                if (ChestID != -1)
                {
                    var chest = Main.chest[ChestID];
                    var num = 0;
                    chest.item.ForEach(i =>
                    {
                        if (i != null && i.type != 0)
                        {
                            if (i.type == Shop.ItemType)
                            {
                                num += Shop.Item.maxStack - i.stack;
                            }
                        }
                        else num += Shop.Item.maxStack;
                    });
                    return num;
                }
                return 0;
            }
        }
        public bool AddItemToChest(Item item, int stack)
        {
            if (ChestID != -1)
            {
                var chest = Main.chest[ChestID];
                int num = 0;
                var slots = new List<int>();
                for (int id = 0; id < 40; id++)
                {
                    var i = chest.item[id];
                    if (num < stack)
                    {
                        if (i.type != 0 && (Shop.Prefix == 0 ? true : i.prefix == Shop.Prefix) && item.maxStack - i.stack > 0 && i.type == item.type)
                        {
                            var margin = i.maxStack - i.stack;
                            if (margin > 0)
                            {
                                if (margin >= (stack - num))
                                {
                                    Main.chest[ChestID].item[id].stack += (stack - num);
                                    num = stack;
                                    slots.Add(id);
                                }
                                else
                                {
                                    num += margin;
                                    Main.chest[ChestID].item[id].stack = i.maxStack;
                                    slots.Add(id);
                                }
                            }
                        }
                        else if (i == null || i.type == 0)
                        {
                            if (Shop.Item.maxStack >= (stack - num))
                            {
                                Main.chest[ChestID].item[id] = Shop.Item.Clone();
                                Main.chest[ChestID].item[id].stack = i.maxStack;
                                Main.chest[ChestID].item[id].prefix = Shop.Prefix == 0 ? 0 : (byte)Shop.Prefix;
                                num += i.maxStack;
                                slots.Add(id);
                            }
                            else
                            {
                                Main.chest[ChestID].item[id] = Shop.Item.Clone();
                                Main.chest[ChestID].item[id].stack = (stack - num);
                                Main.chest[ChestID].item[id].prefix = Shop.Prefix == 0 ? 0 : (byte)Shop.Prefix;
                                num = stack;
                                slots.Add(id);
                            }
                        }
                    }
                }
                Utils.UpdateChest(ChestID, slots);
                if (num == stack) return true;
                return false;
            }
            else
            {
                return false;
            }
        }
        public bool DelItemFromChest(Item item, int stack)
        {
            if (ChestID != -1)
            {
                var chest = Main.chest[ChestID];
                int num = 0;
                var slots = new List<int>();
                for (int id = 0; id < 40; id++)
                {
                    var i = chest.item[id];
                    if (num < stack)
                    {
                        if (i.type != 0 && i.type == Shop.ItemType && (Shop.Prefix == 0 ? true : i.prefix == Shop.Prefix))
                        {
                            var margin = i.stack;
                            if (margin > 0)
                            {
                                if (margin >= (stack - num))
                                {
                                    Main.chest[ChestID].item[id].stack -= (stack - num);
                                    num = stack;
                                    slots.Add(id);
                                }
                                else
                                {
                                    num += margin;
                                    Main.chest[ChestID].item[id] = new Item();
                                    slots.Add(id);
                                }
                            }
                        }
                    }
                }
                Utils.UpdateChest(ChestID, slots);
                if (num == stack) return true;
                return false;
            }
            else
            {
                return false;
            }
        }
        #endregion
    }
    public struct PSSign_Shop
    {
        public PSSign_Shop(int type, int itemType, int stack, int prefix, int price, bool canBuySet, bool unlimited = false)
        {
            Type = type;
            ItemType = itemType;
            Stack = stack;
            Prefix = prefix;
            Price = price;
            CanBuySet = canBuySet;
            UnLimit = unlimited;
            var i = new Item();
            i.SetDefaults(ItemType);
            i.stack = stack;
            i.prefix = (byte)prefix;
            Item = i;
        }
        public const int SELL = 0;
        public const int BUY = 1;
        public int Type { get; set; }
        public Item Item { get; set; }
        public int ItemType { get; set; }
        public int Stack { get; set; }
        public int Prefix { get; set; }
        public int Price { get; set; }
        public bool CanBuySet { get; set; }
        public bool UnLimit { get; set; }
    }
    public struct PSSign_Command
    {
        public PSSign_Command(List<string> commands, long cooldown, int cost = 0)
        {
            Type = 0;
            Commands = commands;
            CoolDown = cooldown;
            Cost = cost;
            IgnorePermissions = false;
        }
        public const int CLICK = 0;
        public const int CLOST = 1;
        public int Type { get; set; }
        public bool IgnorePermissions { get; set; }
        public List<string> Commands { get; set; }
        public long CoolDown { get; set; }
        public int Cost { get; set; }
    }
}
