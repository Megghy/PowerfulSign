using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using UnifiedEconomyFramework;

namespace PowerfulSign.Models
{
    public class ShopSign : SignBase
    {
        public override string Type => "shop";

        public const int SELL = 0;
        public const int BUY = 1;
        public ShopSign() : this(-1, -1, -1, "", -1)
        {
        }
        public ShopSign(int id, int x, int y, string text, int ownerID) : base(id, x, y, text, ownerID)
        {
        }

        public int ShopType { get; set; }
        public Item Item { get; set; } = new();
        public int ItemID
        {
            get => Item.type; set
            {
                var temp = new Item();
                temp.SetDefaults(value);
                temp.stack = Stack;
                temp.prefix = Prefix;
                Item = temp;
            }
        }
        public int Stack { get => Item.stack; set => Item.stack = value; }
        public byte Prefix { get => Item.prefix; set => Item.prefix = value; }
        public int Price { get; set; } = 0;
        public bool CanBuySet { get; set; } = false;
        public bool UnLimit { get; set; } = false;

        public override bool CheckText(string text)
        {
            if (IsSpecialText(text, out var lines, out var type, out var owner))
                if (owner.HasPermission("ps.use.shop"))
                {
                    if (lines.Count >= 2)
                    {
                        if (lines[1].ToLower() is "sell" or "buy")
                        {
                            var tempShop = new ShopSign();
                            for (int i = 2; i < lines.Count; i++)
                            {
                                if (lines[i].Contains(":"))
                                {
                                    var key = lines[i].Split(':')[0];
                                    var value = lines[i].Replace(key + ":", "");
                                    key = key.ToLower();
                                    key = key.Replace(" ", "");
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
                                                        tempShop.ItemID = itemtype;
                                                        var tempitem = new Item();
                                                        tempitem.SetDefaults(itemtype);
                                                        if (tempitem.type == 0)
                                                        {
                                                            owner.SendErrorMessage($"无效的物品id.");
                                                            HasError = true;
                                                            return false;
                                                        }
                                                        else if (num < 1)
                                                        {
                                                            owner.SendErrorMessage($"每次购买数量应大于零.");
                                                            HasError = true;
                                                            return false;
                                                        }
                                                        else if (prefix != 0 && !tempitem.Prefix(prefix))
                                                        {
                                                            owner.SendErrorMessage($"物品前缀无效. 若无需设定物品前缀可只设定物品id及堆叠.");
                                                            HasError = true;
                                                            return false;
                                                        }
                                                        tempitem.stack = num;
                                                        tempitem.prefix = (byte)prefix;
                                                        tempShop.Item = tempitem;
                                                        tempShop.Stack = num;
                                                    }
                                                    else
                                                    {
                                                        owner.SendErrorMessage($"item列格式错误. 应为 [c/6CCCA8:item:物品id 每次购买数量]");
                                                        HasError = true;
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
                                                owner.SendInfoMessage($"你没有权限设定无限商店 <ps.admin.unlimit>, 此设置项将不会生效.");
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
                                                OriginCombatText = value;
                                            }
                                            else
                                            {
                                                owner.SendInfoMessage($"你没有权限自定义CombatText <ps.use.prompt>, 此设置项将不会生效.");
                                            }
                                            break;
                                        case "prompt":
                                            if (owner.HasPermission("ps.use.prompt"))
                                            {
                                                OriginPromptText = value;
                                            }
                                            else
                                            {
                                                owner.SendInfoMessage($"你没有权限自定义PromptText <ps.use.prompt>, 此设置项将不会生效.");
                                            }
                                            break;
                                        case "color":
                                            var color = System.Drawing.ColorTranslator.FromHtml(value);
                                            if (System.Drawing.Color.Empty != color)
                                            {
                                                Color = new Color(color.R, color.G, color.B);
                                            }
                                            else
                                            {
                                                owner.SendErrorMessage($"无效的Hex颜色格式. 请搜索十六进制颜色.");
                                                HasError = true;
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
                            if (tempShop.ItemID == 0)
                            {
                                owner.SendErrorMessage($"无效的商店格式. 缺少必要条件: 物品id.");
                                HasError = true;
                                return false;
                            }
                            else if (tempShop.Stack == 0)
                            {
                                owner.SendErrorMessage($"无效的商店格式. 缺少必要条件: 物品数量.");
                                HasError = true;
                                return false;
                            }
                            if (Utils.FindChestByGuessing(X + 2, Y) == -1)
                            {
                                owner.SendErrorMessage($"未能在标牌右侧找到箱子.");
                                HasError = true;
                                return false;
                            }
                            tempShop.ShopType = type == "sell" ? SELL : BUY;
                            CopyFrom(tempShop);
                            HasError = false;
                            return true;
                        }
                        else
                        {
                            owner.SendInfoMessage($"无效的商店格式. 第二排应为 sell 或 buy.");
                            HasError = true;
                        }
                    }
                    else
                    {
                        owner.SendInfoMessage($"无效的商店格式. 第二排应为 sell 或 buy.");
                        HasError = true;
                    }
                }
                else
                {
                    owner.SendInfoMessage($"你没有权限创建标牌商店 <ps.use.shop>.");
                    return false;
                }
            return false;
        }
        public override string ReplaceVariable(string text)
        {
            base.ReplaceVariable(text);
            return (text ?? Config.Instance.DefaultPromptText.Shop)
                    .Replace("{type}", ShopType == SELL ? "出售" : "收购")
                    .Replace("{shop.name}", Item.Name)
                    .Replace("{shop.stack}", Stack.ToString())
                    .Replace("{shop.inventory}", Inventory.ToString())
                    .Replace("{shop.price}", Price.ToString());
        }
        public override void OnUse(TSPlayer user)
        {
            var userID = user.Account is null ? -1 : user.Account.ID;
            var psp = user.PSPlayer();
            if (Owner != userID)
            {
                if (HasError)
                    user.SendInfoMessage("此标牌商店暂时无法使用.");
                else
                {
                    if (CanBuySet)
                        user.SendShopText(this);
                    else
                    {
                        if (psp.WannaTrade == null)
                        {
                            user.SendInfoMessage($"确定要{(ShopType == SELL ? "购买" : "出售")} {Stack} 个 {Item.Name} ? 你将{(ShopType == SELL ? "失去" : "获得")} {Price} {Config.Instance.MoneyName}.\n请在5秒内再次点击此标牌来确认{(ShopType == SELL ? "购买" : "出售")}.");
                            psp.WannaTrade = this;
                        }
                        else
                        {
                            var tempowner = TShock.UserAccounts.GetUserAccountByID(Owner) ?? new TShockAPI.DB.UserAccount();
                            if (ShopType == SELL)
                                BuyItem(user, tempowner, this, Item, Stack, Price); //卖家卖就是玩家买
                            else
                                SellItem(user, tempowner, this, Item, Stack, Price);
                            psp.WannaTrade = null;
                        }
                    }
                }
            }
            else
                user.SendSignDataVisiting(this);
        }
        public int ChestID { get { return Utils.FindChestByGuessing(X + 2, Y); } }
        public int Inventory
        {
            get
            {
                if (ChestID != -1)
                {
                    var chest = Main.chest[ChestID];
                    var num = 0;
                    chest.item.ForEach(i =>
                    {
                        if (i.type == Item.type && (Prefix == 0 ? true : i.prefix == Prefix))
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
                            if (i.type == ItemID)
                            {
                                num += Item.maxStack - i.stack;
                            }
                        }
                        else num += Item.maxStack;
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
                        if (i.type != 0 && (Prefix == 0 ? true : i.prefix == Prefix) && item.maxStack - i.stack > 0 && i.type == item.type)
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
                            if (Item.maxStack >= (stack - num))
                            {
                                Main.chest[ChestID].item[id] = Item.Clone();
                                Main.chest[ChestID].item[id].stack = i.maxStack;
                                Main.chest[ChestID].item[id].prefix = (byte)(Prefix == 0 ? 0 : Prefix);
                                num += i.maxStack;
                                slots.Add(id);
                            }
                            else
                            {
                                Main.chest[ChestID].item[id] = Item.Clone();
                                Main.chest[ChestID].item[id].stack = (stack - num);
                                Main.chest[ChestID].item[id].prefix = (byte)(Prefix == 0 ? 0 : Prefix);
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
                        if (i.type != 0 && i.type == ItemID && (Prefix == 0 ? true : i.prefix == Prefix))
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
        public void BuyItem(TSPlayer plr, UserAccount owner, Models.ShopSign sign, Item item, int stack, int cost)
        {
            if (sign.ChestID != -1)
            {
                if (sign.UnLimit)
                {
                    if (plr.TakeMoney(cost) && UEF.MoneyUp(owner.Name, cost))
                    {
                        plr.SendSuccessMessage($"成功购买 {stack} 个 {item.Name}, 花费 {cost} {Config.Instance.MoneyName}.");
                        plr.GiveItemEX(item.type, stack, item.prefix);
                    }
                    else
                    {
                        plr.SendErrorMessage($"发生错误.");
                    }
                }
                else
                {
                    if (sign.Inventory >= stack)
                    {
                        if (plr.Balance() >= cost)
                        {
                            if (plr.IsInventoryAviliable(item, stack))
                            {
                                if (sign.DelItemFromChest(item, stack) && plr.TakeMoney(cost) && UEF.MoneyUp(owner.Name, cost))
                                {
                                    plr.SendSuccessMessage($"成功购买 {stack} 个 {item.Name}, 花费 {cost} {Config.Instance.MoneyName}.");
                                    plr.GiveItemEX(item.type, stack, item.prefix);
                                }
                                else
                                {
                                    plr.SendErrorMessage($"发生错误.");
                                }
                            }
                            else
                            {
                                plr.SendErrorMessage($"背包空间不足, 无法装下 {stack} 个 {item.Name}.");
                            }
                        }
                        else
                        {
                            plr.SendErrorMessage($"你的余额不足. 当前剩余 {plr.Balance()}.");
                        }
                    }
                    else
                    {
                        plr.SendErrorMessage($"商店库存不足, 无法购买. 当前剩余: {sign.Inventory}.");
                    }
                }
            }
            else
            {
                plr.SendErrorMessage($"未发现此商店的附属储存空间.");
            }
        }
        public void SellItem(TSPlayer plr, UserAccount owner, Models.ShopSign sign, Item item, int stack, int cost)
        {
            if (sign.ChestID != -1)
            {
                if (sign.UnLimit)
                {
                    if (plr.DelItemFromInventory(item, stack) && plr.GiveMoney(cost))
                    {
                        plr.SendSuccessMessage($"成功出售 {stack} 个 {item.Name}, 获得 {cost} {Config.Instance.MoneyName}.");
                    }
                    else
                    {
                        plr.SendErrorMessage($"发生错误.");
                    }
                }
                else
                {
                    if (sign.AviliableSlot >= stack)
                    {
                        if (UEF.Balance(owner.Name) >= cost)
                        {
                            if (plr.ItemNumInInventory(item.type, item.prefix) >= stack)
                            {
                                if (sign.AddItemToChest(item, stack) && plr.DelItemFromInventory(item, stack) && plr.GiveMoney(cost) && UEF.MoneyDown(owner.Name, cost))
                                {
                                    plr.SendSuccessMessage($"成功出售 {stack} 个 {item.Name}, 获得 {cost} {Config.Instance.MoneyName}.");
                                }
                                else
                                {
                                    plr.SendErrorMessage($"发生错误.");
                                }
                            }
                            else
                            {
                                plr.SendErrorMessage($"未在你的背包中发现足够的 {item.Name}. 已找到 {plr.ItemNumInInventory(item.type, item.prefix)} 个.");
                            }
                        }
                        else
                        {
                            plr.SendErrorMessage($"卖家余额不足以支付此次交易.");
                        }
                    }
                    else
                    {
                        plr.SendErrorMessage($"商店储存空间不足, 无法装下 {stack} 个 {item.Name}. 当前剩余空间 {sign.AviliableSlot}.");
                    }
                }
            }
            else
            {
                plr.SendErrorMessage($"未发现此商店的附属储存空间.");
            }
        }
        public void CopyFrom(ShopSign sign)
        {
            ShopType = sign.ShopType;
            Item = sign.Item;
            Price = sign.Price;
            OriginCombatText = sign.OriginCombatText;
            OriginPromptText = sign.OriginPromptText;
            CanBuySet = sign.CanBuySet;
            UnLimit = sign.UnLimit;
        }
    }
}
