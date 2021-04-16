using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using UnifiedEconomyFramework;

namespace PowerfulSign
{
    static class Utils
    {
        #region PSSign辅助工具
        public static void ImportSign()
        {
            TShock.Log.ConsoleInfo("[C/66D093:<PowerfulSign>] 首次进入地图, 正在导入本地标牌数据...");
            DB.AddSign(new PSSign(-1, new List<int>(), -1, -1, "导入标识, 请勿删除."));
            for (int i = 0; i < Main.sign.Length; i++)
            {
                var sign = Main.sign[i];
                if (sign != null)
                {
                    DB.AddSign(new PSSign(-1, new List<int>(), sign.x, sign.y, sign.text, -1, true));
                    //TShock.Log.ConsoleInfo($"已导入第 {i + 1} 条.");
                }
            }
            TShock.Log.ConsoleInfo($"[C/66D093:<PowerfulSign>] 完成, 共导入 {PSPlugin.SignList.Count - 1} 条标牌数据.");
        }
        public static PSSign GetSign(int x, int y) => PSPlugin.SignList.Where(s => s.X == x && s.Y == y).FirstOrDefault();
        public static bool TryGetSign(int x, int y, out PSSign sign)
        {
            sign = PSPlugin.SignList.FirstOrDefault(s => s.X == x && s.Y == y);
            if (sign == null) return false;
            else return true;
        }
        public static bool TryGetSignByGuess(int x, int y, out PSSign sign)
        {
            sign = PSPlugin.SignList.FirstOrDefault(s => new Rectangle(s.X, s.Y, 2, 2).Contains(x, y));
            if (sign == null) return false;
            else return true;
        }
        #endregion
        public static int FindChestByGuessing(int X, int Y)
        {
            for (int i = 0; i < 8000; i++)
            {
                if (Main.chest[i] != null && new Rectangle(Main.chest[i].x, Main.chest[i].y, 2, 2).Contains(X, Y))
                {
                    return i;
                }
            }

            return -1;
        }
        public static bool TryGetAccountByID(int id, out UserAccount account)
        {
            account = TShock.UserAccounts.GetUserAccountByID(id);
            if (account == null) return false;
            else return true;
        }
        public static void DropItem(int x, int y, Item item, Vector2 vector = default)
        {
            int number = Item.NewItem((int)x, (int)y, (int)(vector == default ? 0 : vector.X), (int)(vector == default ? 0 : vector.Y), item.type, item.stack, true, item.prefix, true, false);
            NetMessage.SendData(21, -1, -1, null, number);
        }
        public async static Task<List<Item>> GetItemFromTile(int x, int y, OTAPI.Tile.ITile itile)
        {
            return await Task.Run(() =>
            {
                List<Item> list = new List<Item>(2)
                {
                    [0] = new Item(),
                    [1] = new Item()
                };
                try
                {
                    int id = 0;
                    int stack = 0;
                    int secondaryitem = 0;
                    int secondarystack = 0;
                    WorldGen.KillTile_GetItemDrops(x, y, itile, out id, out stack, out secondaryitem, out secondarystack);
                    list[0].SetDefaults(id);
                    list[0].stack = stack;
                    list[1].SetDefaults(id);
                    list[1].stack = stack;
                }
                catch { }
                return list;
            });
        }
        public static void UpdateChest(int index, List<int> slots) => slots.ForEach(s => UpdateChest(index, s));
        public static void UpdateChest(int index, int slot) => NetMessage.SendData(32, -1, -1, null, index, slot);
        public static bool IsPointInCircle(Point p, Circle circle) => IsPointInCircle(p.X, p.Y, circle.Center.X, circle.Center.Y, circle.R);
        public static bool IsPointInCircle(int x, int y, int cx, int cy, double r)
        {
            //到圆心的距离 是否大于半径。半径是R  
            //如O(x,y)点圆心，任意一点P（x1,y1） （x-x1）*(x-x1)+(y-y1)*(y-y1)>R*R 那么在圆外 反之在圆内

            if (!((cx - x) * (cx - x) + (cy - y) * (cy - y) > r * r))
            {
                return true;        //当前点在圆内
            }
            else
            {
                return false;       //当前点在圆外
            }
        }
        public class Circle//圆类
        {
            public Circle(Point point, double r)//构造函数
            {
                Center.X = point.X;
                Center.X = point.Y;
                this.R = r;
            }
            public int Is(Point point)//判断函数
            {
                double a = Math.Sqrt((Center.X - point.X) + (Center.Y - point.Y));//点到圆心的距离
                if (a > R) return -1;
                else if (a == R) return 0;
                else return 1;
            }
            public Point Center;//圆心坐标
            public double R;//圆半径
        }
        #region 一些拓展函数
        public static List<string> Split(this string text, string splitText) => text.Split(new[] { splitText }, StringSplitOptions.None).ToList();
        public async static void SendSignDataInCircle(this TSPlayer plr, int radius)
        {
            await Task.Run(() =>
            {
                var psp = plr.GetData<PSPlayer>("PSPlayer");
                psp.LastSignIndex = psp.VisitingSign == null ? -1 : 0; //从第一个开始, 第零个一般是当前正在看的
                PSPlugin.SignList.Where(s => IsPointInCircle(s.X, s.Y, plr.TileX, plr.TileY, radius) && psp.VisitingSign != s).ForEach(s => plr.SendSignData(s));
            });
        }
        public static void SendSignDataDirect(this TSPlayer plr, PSSign sign, bool open, int index)
        {
            var psp = plr.PSP();
            if (index == 0) psp.LastSignIndex = 0;
            if (open)
            {
                psp.VisitingSign = sign;
                NetMessage.PlayNetSound(new NetMessage.NetSoundInfo(plr.TPlayer.position, 122, -1, 0.62f), plr.Index);
            }
            plr.SendRawData(new RawDataBuilder(PacketTypes.SignNew).PackInt16((short)index).PackInt16((short)sign.X).PackInt16((short)sign.Y).PackString(open ? sign.Text : sign.PromptText).PackByte((byte)plr.Index).PackByte(new BitsByte(!open)).GetByteData());
        }
        public static void SendSignData(this TSPlayer plr, PSSign sign)
        {
            var psp = plr.PSP();
            if (psp.LastSignIndex > 998) psp.LastSignIndex = -1;
            psp.LastSignIndex++;
            plr.SendSignDataDirect(sign, sign == psp.VisitingSign, psp.LastSignIndex);
        }
        public static void SendSignDataVisiting(this TSPlayer plr, PSSign sign) => plr.SendSignDataDirect(sign, true, 0);
        public static void SendShopText(this TSPlayer plr, PSSign sign)
        {
            var text = $"[Customer]\n请勿修改所有已存在文本, 直接输入要购买/出售的数量.\n";
            plr.SendRawData(new RawDataBuilder(PacketTypes.SignNew).PackInt16((short)0).PackInt16((short)sign.X).PackInt16((short)sign.Y).PackString(text).PackByte((byte)plr.Index).PackByte(new BitsByte(false)).GetByteData());
        }
        public static UserAccount AccountEX(this TSPlayer plr) => plr.Account ?? new UserAccount() { ID = -2 };
        public static PSPlayer PSP(this TSPlayer plr) => plr.GetData<PSPlayer>("PSPlayer");
        public static int ItemNumInInventory(this TSPlayer plr, int type, int prefix)
        {
            int num = 0;
            plr.TPlayer.inventory.ForEach(i => { if (i.type == type && (prefix == 0 ? true : i.prefix == prefix)) num += i.stack; });
            return num;
        }
        public static void GiveItemEX(this TSPlayer plr, int type, int stack, int prefix)
        {
            var item = new Item();
            item.SetDefaults(type);
            int num = 0;

            while (num < stack)
            {
                int itemID;
                if (stack - num > item.maxStack)
                {
                    itemID = Item.NewItem(plr.TPlayer.position, item.height, item.width, type, item.maxStack, false, prefix);
                    num += item.maxStack;
                }
                else
                {
                    itemID = Item.NewItem(plr.TPlayer.position, item.height, item.width, type, stack - num, false, prefix);
                    num = stack;
                }
                NetMessage.SendData(90, -1, -1, null, itemID, 0);
            }
        }
        /// <summary>
        /// 玩家背包是否能装下指定数量物品
        /// </summary>
        /// <param name="plr"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool IsInventoryAviliable(this TSPlayer plr, Item item, int stack)
        {
            int num = 0;
            plr.TPlayer.inventory.ForEach(i =>
            {
                if (i != null && i.type != 0)
                {
                    if (i.type == item.type)
                    {
                        num += item.maxStack - i.stack;
                    }
                }
                else num += item.maxStack;
            });
            if (stack <= num) return true;
            return false;
        }
        /// <summary>
        /// 是否成功从玩家背包删除指定数量物品
        /// </summary>
        /// <param name="plr"></param>
        /// <param name="item"></param>
        public static bool DelItemFromInventory(this TSPlayer plr, Item item, int stack) => plr.DelItemFromInventory(item.type, stack, item.prefix);
        public static bool DelItemFromInventory(this TSPlayer plr, int type, int num, int prefix)
        {
            var tempnum = 0;
            int slot = 0;
            plr.TPlayer.inventory.ForEach(i =>
            {
                if (i != null && i.type == type && (prefix == 0 ? true : i.prefix == prefix))
                {
                    if (tempnum < num)
                    {
                        if (num - tempnum >= i.stack)
                        {
                            tempnum += i.stack;
                            i.SetDefaults(0);
                            plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, slot); //移除玩家背包内的物品
                        }
                        else
                        {
                            i.stack -= num - tempnum;
                            tempnum = num;
                            plr.SendData(PacketTypes.PlayerSlot, null, plr.Index, slot); //移除玩家背包内的物品
                        }
                    }

                }
                slot++;
            });
            if (tempnum == num) return true;
            return false;
        }
        public static bool TakeMoney(this TSPlayer plr, long num) => UnifiedEconomyFramework.UEF.MoneyDown(plr.Name, num);
        public static bool GiveMoney(this TSPlayer plr, long num) => UnifiedEconomyFramework.UEF.MoneyUp(plr.Name, num);
        public static long Balance(this TSPlayer plr) => UnifiedEconomyFramework.UEF.Balance(plr.Name);
        public static void BuyItem(this TSPlayer plr, UserAccount owner, PSSign sign, Item item, int stack, int cost)
        {
            if (sign.ChestID != -1)
            {
                if (sign.Inventory >= stack)
                {
                    if (plr.Balance() >= cost)
                    {
                        if (plr.IsInventoryAviliable(item, stack))
                        {
                            if (plr.DelItemFromInventory(item, stack) && sign.DelItemFromChest(item, stack) && plr.TakeMoney(cost) && UEF.MoneyUp(owner.Name, cost))
                            {
                                plr.SendSuccessMessage($"[C/66D093:<PowerfulSign>] 成功购买 {stack} 个 {item.Name}, 花费 {cost} {PSPlugin.Config.MoneyName}.");
                                plr.GiveItemEX(item.type, stack, item.prefix);
                            }
                            else
                            {
                                plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 发生错误.");
                            }
                        }
                        else
                        {
                            plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 背包空间不足, 无法装下 {stack} 个 {item.Name}.");
                        }
                    }
                    else
                    {
                        plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 你的余额不足. 当前剩余 {plr.Balance()}.");
                    }
                }
                else
                {
                    plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 商店库存不足, 无法购买. 当前剩余: {sign.Inventory}.");
                }
            }
            else
            {
                plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 未发现此商店的附属储存空间.");
            }
        }
        public static void SellItem(this TSPlayer plr, UserAccount owner, PSSign sign, Item item, int stack, int cost)
        {
            if (sign.ChestID != -1)
            {
                if (sign.AviliableSlot >= stack)
                {
                    if (UEF.Balance(owner.Name) >= cost)
                    {
                        if (plr.ItemNumInInventory(item.type, item.prefix) >= stack)
                        {
                            if (sign.AddItemToChest(item, stack) && plr.DelItemFromInventory(item, stack) && plr.GiveMoney(cost) && UEF.MoneyDown(owner.Name, cost))
                            {
                                plr.SendSuccessMessage($"[C/66D093:<PowerfulSign>] 成功出售 {stack} 个 {item.Name}, 获得 {cost} {PSPlugin.Config.MoneyName}.");
                            }
                            else
                            {
                                plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 发生错误.");
                            }
                        }
                        else
                        {
                            plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 未在你的背包中发现足够的 {item.Name}. 已找到 {plr.ItemNumInInventory(item.type, item.prefix)} 个.");
                        }
                    }
                    else
                    {
                        plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 卖家余额不足以支付此次交易.");
                    }
                }
                else
                {
                    plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 商店储存空间不足, 无法装下 {stack} 个 {item.Name}. 当前剩余空间 {sign.AviliableSlot}.");
                }
            }
            else
            {
                plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 未发现此商店的附属储存空间.");
            }
        }
        public static void SendCombatText(this TSPlayer plr, string text, Color color)
        {
            plr.SendData(PacketTypes.CreateCombatTextExtended, text, (int)color.PackedValue, plr.X, plr.Y);
        }
        public static void SendCombatText(this TSPlayer plr, string text, Color color, int tilex, int tiley)
        {
            plr.SendData(PacketTypes.CreateCombatTextExtended, text, (int)color.PackedValue, tilex * 16, tiley * 16);
        }
        public static void SendCombatText(this TSPlayer plr, string text, Color color, float x, float y)
        {
            plr.SendData(PacketTypes.CreateCombatTextExtended, text, (int)color.PackedValue, x, y);
        }
        public static string SearchString(this string s, string s1, string s2)  //获取搜索到的数目  
        {
            int n1, n2;
            n1 = s.IndexOf(s1, 0) + s1.Length;   //开始位置  
            n2 = s.IndexOf(s2, n1);               //结束位置    
            return s.Substring(n1, n2 - n1);   //取搜索的条数，用结束的位置-开始的位置,并返回    
        }
        #endregion
    }
}
