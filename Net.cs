using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

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
                    if (plr.Account == null) return;
                    switch (args.MsgID)
                    {
                        case PacketTypes.SignRead:
                            args.Handled = true;
                            var x = reader.ReadInt16();
                            var y = reader.ReadInt16();
                            if (Utils.TryGetSign(x, y, out var sign))
                            {
                                if (sign.Owner != plr.Account.ID && !plr.HasPermission("ps.use.edit") && sign.Owner != -1 && !sign.CanEdit)
                                {
                                    plr.SendErrorMessage($"你没有编辑此标牌的权限.");
                                    args.Handled = true;
                                }
                                else
                                {
                                    plr.SendSignData(sign, true, 0);
                                }
                            }
                            else
                            {
                                var tempSign = new PSSign(plr.Account.ID, new List<int>(), x, y, "");
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
                            if (Utils.TryGetSign(x, y, out sign))
                            {
                                if (sign.Text != text)
                                {
                                    if (sign.Owner == plr.Account.ID || sign.Owner == -1)
                                    {
                                        sign.Text = text;
                                        sign.Update();
                                    }
                                    else
                                    {
                                        plr.SendErrorMessage($"<PowerfulSign> 你没有编辑此标牌的权限.");
                                    }
                                }
                                else
                                {

                                }
                            }
                            else
                            {
                                var tempSign = new PSSign(owner, new List<int>(), x, y, text);
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
                                var tempSign = new PSSign(plr.Account.ID, new List<int>(), x, y, "");
                                DB.AddSign(tempSign);
                                plr.SendSignData(tempSign, true);
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
                    PSPlugin.SignList.Where(s => new Rectangle(s.X, s.Y, 2, 2).Contains(args.X, args.Y)).ForEach(s => DB.DelSign(s));
                    Utils.DropItem(args.X, args.Y, Utils.GetItemFromTile(args.X, args.Y, Main.tile[args.X, args.Y]).Result[0]);
                }
            }
            catch { }
        }
    }
}
