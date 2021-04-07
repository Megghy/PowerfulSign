using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using UnifiedEconomyFramework;

namespace PowerfulSign
{
    class Net
    {
        public static void OnGetData(GetDataEventArgs args)
        {
            using (MemoryStream r = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length - 1))
            {
                using (var reader = new BinaryReader(r))
                {
                    var plr = TShock.Players[args.Msg.whoAmI] ?? new TSPlayer(-1);
                    var psp = plr.GetData<PSPlayer>("PSPlayer");
                    var userID = (plr.Account ?? new TShockAPI.DB.UserAccount() { ID = -1 }).ID;
                    switch (args.MsgID)
                    {
                        case PacketTypes.SignRead:
                            args.Handled = true;
                            var x = reader.ReadInt16();
                            var y = reader.ReadInt16();
                            if (Utils.TryGetSign(x, y, out var sign))
                            {
                                switch (sign.Type)
                                {
                                    case PSSign.Types.Normal:
                                        if (sign.Owner != userID && sign.Owner != -1 && !sign.CanEdit && !sign.Friends.Contains(userID))
                                        {
                                            plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 你没有编辑此标牌的权限.");
                                        }
                                        else
                                        {
                                            plr.SendSignData(sign, true, 0);
                                        }
                                        break;
                                    case PSSign.Types.Shop:
                                        if (sign.Owner != userID)
                                        {
                                            if (sign.Error)
                                            {
                                                plr.SendInfoMessage("[C/66D093:<PowerfulSign>] 此标牌商店暂时无法使用.");
                                            }
                                            else
                                            {
                                                if (sign.Shop.CanBuySet)
                                                {
                                                    plr.SendShopText(sign);
                                                }
                                                else
                                                {
                                                    if (psp.WannaTrade == null)
                                                    {
                                                        plr.SendInfoMessage($"[C/66D093:<PowerfulSign>] 确定要{(sign.Shop.Type == PSSign_Shop.SELL ? "购买" : "出售")} {sign.Shop.Stack} 个 {sign.Shop.Item.Name} ? 你将{(sign.Shop.Type == PSSign_Shop.SELL ? "失去" : "获得")} {sign.Shop.Price} {PSPlugin.Config.MoneyName}.\n请在5秒内再次点击此标牌来确认{(sign.Shop.Type == PSSign_Shop.SELL ? "购买" : "出售")}.");
                                                        psp.WannaTrade = sign;
                                                    }
                                                    else
                                                    {
                                                        var tempowner = TShock.UserAccounts.GetUserAccountByID(sign.Owner) ?? new TShockAPI.DB.UserAccount();
                                                        if (sign.Shop.Type == PSSign_Shop.SELL) plr.BuyItem(tempowner, sign, sign.Shop.Item, sign.Shop.Stack, sign.Shop.Price); //卖家卖就是玩家买
                                                        else plr.SellItem(tempowner, sign, sign.Shop.Item, sign.Shop.Stack, sign.Shop.Price);
                                                        psp.WannaTrade = null;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            plr.SendSignData(sign, true, 0);
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                var tempSign = new PSSign(userID, new List<int>(), x, y, "");
                                DB.AddSign(tempSign);
                                plr.SendSignData(tempSign, true);
                            }
                            break;
                        case PacketTypes.SignNew:
                            args.Handled = true;
                            reader.ReadInt16();
                            x = reader.ReadInt16();
                            y = reader.ReadInt16();
                            var text = reader.ReadString();
                            var owner = reader.ReadInt16();
                            if (Utils.TryGetSign(x, y, out sign) && CheckShopText(text, plr, sign))
                            {
                                psp.VisitingSign = null;
                                if (sign.Text != text)
                                {
                                    if (sign.Owner == userID || sign.Owner == -1 || sign.Friends.Contains(userID) || plr.HasPermission("ps.use.edit"))
                                    {
                                        sign.Text = text;
                                        sign.Update();
                                        if (sign.ProcessText())
                                        {
                                            sign.Owner = plr.Account.ID;
                                            plr.SendSuccessMessage("[C/66D093:<PowerfulSign>] 已成功创建特殊标牌.");
                                        }
                                    }
                                    else
                                    {
                                        plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 你没有编辑此标牌的权限.");
                                    }
                                }
                                else
                                {

                                }
                            }
                            else
                            {
                                var tempSign = new PSSign(userID, new List<int>(), x, y, text);
                                DB.AddSign(tempSign);
                                plr.SendSignData(tempSign, true);
                            }
                            break;
                        case PacketTypes.PlaceObject:
                            x = reader.ReadInt16();
                            y = reader.ReadInt16();
                            var type = reader.ReadInt16();
                            if (Main.tileSign[type] && !Utils.TryGetSign(x, y, out sign))
                            {
                                var tempSign = new PSSign(userID, new List<int>(), x, y, "");
                                DB.AddSign(tempSign);
                                plr.SendSignData(tempSign, true);
                            }
                            break;
                        case PacketTypes.ChestGetContents:
                            x = reader.ReadInt16();
                            y = reader.ReadInt16();
                            if (Utils.TryGetSign(x - 2, y, out sign))
                            {
                                if (sign.Owner != userID && !plr.HasPermission("ps.admin.open"))
                                {
                                    plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 此箱子已被作为标牌商店容器, 无法打开.");
                                    args.Handled = true;
                                }
                            }
                            break;
                        case PacketTypes.PlaceChest:
                            var status = reader.ReadByte();
                            x = reader.ReadInt16();
                            y = reader.ReadInt16();
                            var chestID = Utils.FindChestByGuessing(x, y);
                            if (chestID != -1 && Utils.TryGetSign(Main.chest[chestID].x - 2, Main.chest[chestID].y, out sign))
                            {
                                if (status == 1)
                                {
                                    if (sign.Owner != userID && !plr.HasPermission("ps.admin.destroy"))
                                    {
                                        plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 此箱子已被作为标牌商店容器, 无法摧毁.");
                                        WorldGen.SquareTileFrame(x, y);
                                        args.Handled = true;
                                    }
                                    else
                                    {
                                        sign.Error = true;
                                    }
                                }
                                else if (status == 0)
                                {
                                    sign.Error = false;
                                    plr.SendSuccessMessage($"[C/66D093:<PowerfulSign>] 已为标牌商店放置容器.");
                                }
                            }
                            break;
                    }
                }
            }
        }
        public static bool CheckShopText(string text, TSPlayer plr, PSSign sign)
        {
            try
            {
                if (text.Contains("\n"))
                {
                    var lines = text.Split("\n");
                    if (lines[0].StartsWith("[") && lines[0].EndsWith("]"))
                    {
                        var maintype = lines[0].SearchString("[", "]").ToLower();
                        if (maintype == "customer")
                        {
                            if (!text.StartsWith("[Customer]\n请勿修改所有已存在文本, 直接输入要购买/出售的数量.\n"))
                            {
                                plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 购买/出售格式无效, 请勿随意改动原有文本.");
                                plr.SendSignData(sign, true, 0);
                                return true;
                            }
                            if (int.TryParse(lines[2], out int num) && num > 0)
                            {
                                var owner = TShock.UserAccounts.GetUserAccountByID(sign.Owner) ?? new TShockAPI.DB.UserAccount();
                                var cost = sign.Shop.Price * num;
                                var item = sign.Shop.Item;
                                var stack = sign.Shop.Stack * num;
                                if (sign.Shop.Type == PSSign_Shop.SELL) plr.BuyItem(owner, sign, item, stack, cost); //卖家卖就是玩家买
                                else plr.SellItem(owner, sign, item, stack, cost);
                                plr.SendShopText(sign);
                            }
                            else
                            {
                                plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 购买/出售数量无效, 请输入大于0的正整数.");
                            }
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex){ TShock.Log.ConsoleError(ex.Message); }
            return false;
        }
        public async static void OnTileEdit(object o, GetDataHandlers.TileEditEventArgs args)
        {
            try
            {
                if (Main.tileSign[Main.tile[args.X, args.Y].type] && args.Action == GetDataHandlers.EditAction.KillTile && args.EditData == 0)
                {
                    await Task.Run(() =>
                    {
                        var plr = args.Player;
                        PSPlugin.SignList.ToList().Where(s => s.X >= 0 && s.X < Main.maxTilesX && s.Y >= 0 && s.Y < Main.maxTilesY && !Main.tileSign[Main.tile[s.X, s.Y].type]).ForEach(s => {
                            if (s.Owner != (args.Player.Account ?? new TShockAPI.DB.UserAccount()).ID && !plr.HasPermission("ps.admin.destroy"))
                            {
                                plr.SendErrorMessage($"[C/66D093:<PowerfulSign>] 你没有权限摧毁此标牌.");
                                plr.SendSignData(s, false);
                                WorldGen.SquareTileFrame(args.X, args.Y);
                                args.Handled = true;
                            }
                            else
                            {
                                PSPlugin.SignList.Remove(s);
                                Utils.DropItem(args.X, args.Y, Utils.GetItemFromTile(args.X, args.Y, Main.tile[args.X, args.Y]).Result[0]);
                            }
                        });
                    });
                }
            }
            catch { }
        }
    }
}
