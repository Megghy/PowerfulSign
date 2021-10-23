using System;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace PowerfulSign.Core
{
    class Net
    {
        public static void OnGetData(GetDataEventArgs args)
        {
            using (MemoryStream r = new(args.Msg.readBuffer, args.Index, args.Length - 1))
            {
                using (var reader = new BinaryReader(r))
                {
                    var plr = TShock.Players[args.Msg.whoAmI] ?? new TSPlayer(-1);
                    var psp = plr.GetData<PSPlayer>("PSPlayer");
                    var userID = plr.AccountEX().ID;
                    switch (args.MsgID)
                    {
                        case PacketTypes.SignRead:
                            args.Handled = true;
                            var x = reader.ReadInt16();
                            var y = reader.ReadInt16();
                            if (PowerfulSignAPI.TryGetSign(x, y, out var sign))
                                sign.OnUse(plr);
                            else
                                plr.SendSignDataVisiting(DB.AddSign(new(x, y, "", userID)));
                            break;
                        case PacketTypes.SignNew:
                            args.Handled = true;
                            reader.ReadInt16();
                            x = reader.ReadInt16();
                            y = reader.ReadInt16();
                            var text = reader.ReadString();
                            var owner = reader.ReadInt16();
                            if (!PowerfulSignAPI.UpdateSign(x, y, plr, text))
                                plr.SendErrorMessage($"你没有编辑此标牌的权限.");
                            break;
                        case PacketTypes.PlaceObject:
                            x = reader.ReadInt16();
                            y = reader.ReadInt16();
                            var type = reader.ReadInt16();
                            if (Main.tileSign[type] && !PowerfulSignAPI.TryGetSign(x, y, out _))
                                PowerfulSignAPI.AddSign(x, y, plr);
                            break;
                        case PacketTypes.ChestGetContents:
                            x = reader.ReadInt16();
                            y = reader.ReadInt16();
                            if (PowerfulSignAPI.TryGetSign(x - 2, y, out sign))
                            {
                                if (sign.Owner != userID && sign.To<Models.ShopSign>() is { } && !plr.HasPermission("ps.admin.open"))
                                {
                                    plr.SendErrorMessage($"此箱子已被作为标牌商店容器, 无法打开.");
                                    args.Handled = true;
                                }
                            }
                            break;
                        case PacketTypes.PlaceChest:
                            var status = reader.ReadByte();
                            x = reader.ReadInt16();
                            y = reader.ReadInt16();
                            var chestID = Utils.FindChestByGuessing(x, y);
                            if (chestID != -1 && Core.PowerfulSignAPI.TryGetSign(Main.chest[chestID].x - 2, Main.chest[chestID].y, out sign))
                            {
                                if (status == 1)
                                {
                                    if (sign.Owner != userID && sign.To<Models.ShopSign>() is { } && !plr.HasPermission("ps.admin.destroy"))
                                    {
                                        plr.SendErrorMessage($"此箱子已被作为标牌商店容器, 无法摧毁.");
                                        WorldGen.SquareTileFrame(x, y);
                                        args.Handled = true;
                                    }
                                    else
                                    {
                                        sign.HasError = true;
                                    }
                                }
                                else if (status == 0)
                                {
                                    sign.HasError = false;
                                    plr.SendSuccessMessage($"已为标牌商店放置容器.");
                                }
                            }
                            break;
                    }
                }
            }
        }
        public static void OnTileEdit(object o, GetDataHandlers.TileEditEventArgs args)
        {
            try
            {
                if (Main.tileSign[Main.tile[args.X, args.Y].type] && args.Action == GetDataHandlers.EditAction.KillTile && args.EditData == 0)
                {
                    var plr = args.Player;
                    if (PowerfulSignAPI.TryGetSignByGuess(args.X, args.Y, out var s))
                    {
                        if (s.Owner != args.Player.AccountEX().ID && !plr.HasPermission("ps.admin.destroy") && s.ID != -1)
                        {
                            plr.SendErrorMessage($"你没有权限摧毁此标牌.");
                            plr.SendTileSquare(s.X, s.Y, 2);
                            plr.SendSignData(s);
                            args.Handled = true;
                        }
                        else
                            PowerfulSignAPI.DelSign(args.X, args.Y);
                    }
                }
            }
            catch (Exception ex){ TShock.Log.ConsoleError(ex.ToString()); }
        }
    }
}
