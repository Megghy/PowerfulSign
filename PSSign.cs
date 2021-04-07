using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            ProcessText();
        }
        public bool ProcessText()
        {
            try
            {
                if (Text.Contains("\n"))
                {
                    var lines = Text.Split("\n");
                    if (lines[0].StartsWith("[") && lines[0].EndsWith("]"))
                    {
                        var maintype = lines[0].SearchString("[", "]").ToLower();
                        var owner = TShock.Players.FirstOrDefault(p => p != null && p.Account != null && p.Account.ID == Owner) ?? new TSPlayer(-1);
                        switch (maintype)
                        {
                            case "shop":
                                Type = Types.Shop;
                                if (lines.Count >= 3)
                                {
                                    if (lines[1].ToLower() == "sell" || lines[1].ToLower() == "buy")
                                    {
                                        var tempshop = new PSSign_Shop();
                                        for (int i = 2; i < lines.Count; i++)
                                        {
                                            if (lines[i].Contains(":"))
                                            {
                                                var key = lines[i].ToLower().Split(':')[0];
                                                var value = lines[i].Replace(key + ":", "");
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
                                                                    tempshop.ItemType = itemtype;
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
                                                                    tempshop.Item = tempitem;
                                                                    tempshop.Stack = num;
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
                                                        tempshop.CanBuySet = value.ToLower() == "true";
                                                        break;
                                                    case "unlimit":
                                                        if (owner.HasPermission("ps.admin.unlimit"))
                                                        {
                                                            tempshop.UnLimit = value.ToLower() == "true";
                                                        }
                                                        else
                                                        {
                                                            owner.SendInfoMessage($"[C/66D093:<PowerfulSign>] 你没有权限设定无限商店, 此设置项将不会生效.");
                                                        }
                                                        break;
                                                    case "price":
                                                        if (int.TryParse(value, out int price))
                                                        {
                                                            tempshop.Price = price;
                                                        }
                                                        break;
                                                    case "combat":
                                                        if (owner.HasPermission("ps.use.combat"))
                                                        {
                                                            _CombatText = value;
                                                        }
                                                        else
                                                        {
                                                            owner.SendInfoMessage($"[C/66D093:<PowerfulSign>] 你没有权限自定义CombatText, 此设置项将不会生效.");
                                                        }
                                                        break;
                                                    case "prompt":
                                                        if (owner.HasPermission("ps.use.prompt"))
                                                        {
                                                            _PromptText = value;
                                                        }
                                                        else
                                                        {
                                                            owner.SendInfoMessage($"[C/66D093:<PowerfulSign>] 你没有权限自定义PromptText, 此设置项将不会生效.");
                                                        }
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                return false;
                                            }
                                        }
                                        if (tempshop.ItemType == 0)
                                        {
                                            owner.SendErrorMessage($"[C/66D093:<PowerfulSign>] 无效的商店格式. 缺少必要条件: 物品id.");
                                            Error = true;
                                            return false;
                                        }
                                        else if (tempshop.Stack == 0)
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
                                        tempshop.Type = lines[1].ToLower() == "sell" ? PSSign_Shop.SELL : PSSign_Shop.BUY;
                                        Shop = tempshop;
                                        Type = Types.Shop;
                                        Error = false;
                                        return true;
                                    }
                                    else
                                    {
                                        if (owner != null) owner.SendInfoMessage($"[C/66D093:<PowerfulSign>] 无效的商店格式. 第二排应为 sell 或 buy.");
                                        Error = true;
                                    }
                                }
                                break;
                            case "command":
                                Type = Types.Command;

                                break;
                        }
                    }
                }
                else Type = Types.Normal;
                SendToAll();
                return false;
            }
            catch { return false; }
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
        public bool Error = false;
        public UserAccount Account => TShock.UserAccounts.GetUserAccountByID(Owner) ?? new UserAccount() { ID = -1, Name = "UnKnown" };
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
                    Types.Normal => PSPlugin.Config.DefaultPromptText.Normal
                    .Replace("{text}", Text)
                    .Replace("{owner}", Account.Name)
                    ,

                    Types.Shop => PSPlugin.Config.DefaultPromptText.Shop
                    .Replace("{type}", Shop.Type == PSSign_Shop.SELL ? "出售" : "收购")
                    .Replace("{item.name}", Shop.Item.Name)
                    .Replace("{item.stack}", Shop.Stack.ToString())
                    .Replace("{inventory}", Inventory.ToString())
                    .Replace("{price}", Shop.Price.ToString())
                    .Replace("{text}", Text)
                    .Replace("{owner}", Account.Name)
                    ,

                    Types.Command => PSPlugin.Config.DefaultPromptText.Command,
                    _ => Text
                } : _PromptText;
            }
        }
        public string _CombatText = string.Empty;
        public string CombatText
        {
            get
            {
                return _CombatText == string.Empty ? Type switch
                {
                    Types.Normal => PSPlugin.Config.DefaultCombatText.Normal == "" ? Text : PSPlugin.Config.DefaultCombatText.Normal
                    .Replace("{text}", Text)
                    .Replace("{owner}", Account.Name)
                    ,

                    Types.Shop => PSPlugin.Config.DefaultCombatText.Shop
                    .Replace("{type}", Shop.Type == PSSign_Shop.SELL ? "出售" : "收购")
                    .Replace("{item.name}", Shop.Item.Name)
                    .Replace("{item.stack}", Shop.Stack.ToString())
                    .Replace("{inventory}", Inventory.ToString())
                    .Replace("{price}", Shop.Price.ToString())
                    .Replace("{text}", Text)
                    .Replace("{owner}", Account.Name)
                    ,

                    Types.Command => PSPlugin.Config.DefaultCombatText.Command,
                    _ => Text
                } : _CombatText;
            }
        }
        public List<PSSign_Command> Commands { get; set; }
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
                    chest.item.ForEach(i => {
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
                    chest.item.ForEach(i => {
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
        public Item Item{ get;set; }
        public int ItemType { get; set; }
        public int Stack { get; set; }
        public int Prefix { get; set; }
        public int Price { get; set; }
        public bool CanBuySet { get; set; }
        public bool UnLimit { get; set; }
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
