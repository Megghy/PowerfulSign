using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;

namespace PowerfulSign.Core
{
    public class PowerfulSignAPI
    {
        public static bool AddSign(int x, int y, TSPlayer owner, string text = "", bool send = true)
        {
            var psp = owner.PSPlayer();
            if (!TryGetSign(x, y, out _) && owner.Account is { })
            {
                psp.VisitingSign = null;
                var sign = DB.AddSign(new(x, y, text, owner.Account.ID));
                if (send)
                    owner.SendSignData(sign);
                TShock.Log.ConsoleInfo($"<PowerfulSign> 新增标牌 <{x}, {y}>. {text}");
                return true;
            }
            return false;
        }
        public static bool UpdateSign(int x, int y, TSPlayer user, string text)
        {
            var id = user.Account is null ? -1 : user.Account.ID;
            if (TryGetSign(x, y, out var sign) && !Utils.IsShop(text, user, sign.To<Models.ShopSign>()))
            {
                if (sign.Text != text)
                {
                    if (sign.Owner == id || sign.Owner == -1 || sign.Friends.Contains(id) || user.HasPermission("ps.admin.edit"))
                    {
                        var oldText = sign.Text;
                        sign.Text = text;
                        sign.Update();
                        user.SendSignDataVisiting(sign); //编辑者要多发一次来关掉牌子
                        if (sign.CheckText(text))
                            sign.Owner = id;
                        TShock.Log.ConsoleInfo($"<PowerfulSign> 标牌已更新 <{x}, {y}>.\r\n{oldText}\r\n↓↓↓↓↓\r\n{text}");
                        return true;
                    }
                    else
                    {
                        //user.SendErrorMessage($"你没有编辑此标牌的权限.");
                    }
                }
                else
                    AddSign(x, y, user);
            }
            return false;
        }
        public static bool DelSign(int x, int y)
        {
            if (TryGetSign(x, y, out var sign))
            {
                Data.Signs.Remove(sign);
                //Utils.DropItem(x, y, Utils.GetItemFromTile(x, y, Main.tile[x, y]).Result[0]);
                DB.DelSign(sign);
                TShock.Log.ConsoleInfo($"<PowerfulSign> 标牌已移除, 位于 <{x}, {y}>");
                return true;
            }
            return false;
        }
        public static void SendSignData(TSPlayer plr, Models.SignBase sign) => plr.SendSignDataDirect(sign, true, 0);
        public static bool TryGetSign(int x, int y, out Models.SignBase sign)
        {
            sign = Data.Signs.FirstOrDefault(s => s.X == x && s.Y == y);
            return sign != null;
        }
        public static bool TryGetSignByGuess(int x, int y, out Models.SignBase sign)
        {
            sign = Data.Signs.FirstOrDefault(s => new Rectangle(s.X, s.Y, 2, 2).Contains(x, y));
            return sign != null;
        }
    }
}
