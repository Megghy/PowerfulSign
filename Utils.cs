using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerfulSign
{
    static class Utils
    {
        public static void ImportSign()
        {
            TShock.Log.ConsoleInfo("<PowerfulSign> 首次进入地图, 正在导入本地标牌数据...");
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
            TShock.Log.ConsoleInfo($"<PowerfulSign> 完成, 共导入 {PSPlugin.SignList.Count - 1} 条标牌数据.");
        }
        public static PSSign GetSign(int x, int y) => PSPlugin.SignList.Where(s => s.X == x && s.Y == y).FirstOrDefault();
        public static bool TryGetSign(int x, int y, out PSSign sign)
        {
            sign = PSPlugin.SignList.FirstOrDefault(s => s.X == x && s.Y == y);
            if (sign == null) return false; 
            else return true;
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
        public static List<string>Split(this string text, string splitText) => text.Split(new[] { splitText }, StringSplitOptions.None).ToList();
        public async static void SendSignDataInCircle(this TSPlayer plr, int radius)
        {
            await Task.Run(() => {
                plr.GetData<PSPlayer>("PSPlayer").LastSignIndex = -1;
                PSPlugin.SignList.Where(s => IsPointInCircle(s.X, s.Y, plr.TileX, plr.TileY, radius)).ForEach(s => plr.SendSignData(s, false));
            });
        }
        public static void SendSignData(this TSPlayer plr, PSSign sign, bool open, int? index = null)
        {
            var psp = plr.GetData<PSPlayer>("PSPlayer");
            if (index != null)
            {
                if (psp.LastSignIndex > 998) psp.LastSignIndex = -1;
                psp.LastSignIndex++;
            }
            plr.SendRawData(new RawDataBuilder(PacketTypes.SignNew).PackInt16((short)(index ?? psp.LastSignIndex)).PackInt16((short)sign.X).PackInt16((short)sign.Y).PackString(((plr.Account ?? new UserAccount() { ID = -2 }).ID == sign.Owner || sign.Owner == -1) ? sign.Text : sign.PromptText).PackByte((byte)plr.Index).PackByte(new BitsByte(!open)).GetByteData());
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
