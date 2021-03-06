using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TShockAPI;
using TShockAPI.DB;

namespace PowerfulSign.Core
{
    public class PSPlayer
    {
        public PSPlayer(TSPlayer plr)
        {
            Player = plr;
            Initialize();
        }
        TSPlayer Player { get; set; }
        UserAccount Account { get { return Player.Account ?? new UserAccount() { ID = -2 }; } }
        public int LastSignIndex;
        public Models.SignBase VisitingSign;

        Models.ShopSign _WannaTrade;
        int TradeTimer = 0;
        public Models.ShopSign WannaTrade
        {
            get
            {
                return _WannaTrade;
            }
            set { _WannaTrade = value; TradeTimer = 0; }
        }
        public void Initialize()
        {
            LastSignIndex = -1;
            Loop();
        }
        async void Loop()
        {
            await Task.Run(() =>
            {
                int autorefreshCount = 0;
                int errorCount = 0;
                Dictionary<int, int> combatCount = new Dictionary<int, int>();
                while (Player.Active)
                {
                    try
                    {
                        var position = Player.TPlayer.position;
                        var combatrect = new Rectangle((int)(position.X / 16 - Config.Instance.CombatTextRange), (int)(position.Y / 16 - Config.Instance.CombatTextRange), 2 + Config.Instance.CombatTextRange * 2, 2 + Config.Instance.CombatTextRange * 2);
                        if (VisitingSign != null && !new Rectangle(VisitingSign.X, VisitingSign.Y, 2, 2).Intersects(new Rectangle((int)(position.X / 16 - 5), (int)(position.Y / 16 - 5), 2 + Config.Instance.CombatTextRange * 12, 12))) VisitingSign = null; //超出范围则未在编辑标牌.
                        Data.Signs.Where(s => combatrect.Intersects(new Rectangle(s.X, s.Y, 2, 2))).ForEach(s =>
                        {
                            if (!combatCount.ContainsKey(s.ID))
                            {
                                Player.SendCombatText(s.CombatText, s.Color, s.X, s.Y);
                                combatCount.Add(s.ID, 0);
                            }
                            if (s.To<Models.ComandSign>() is { } cmdSign)
                            {
                                UseCommandSign(cmdSign);
                            }
                        });
                        if (autorefreshCount >= Config.Instance.AutoRefreshLevel && Player.Active)
                        {
                            Player.SendSignDataInCircle(Config.Instance.RefreshRadius);
                            autorefreshCount = 0;
                        }
                        else autorefreshCount++;

                        if (errorCount >= 4)
                        {
                            Data.Signs.Where(s => s.Owner == Account.ID && s.HasError).ForEach(s => Player.SendCombatText($"<PowerfulSign>\n标牌无效", Color.Red, s.X, s.Y));
                            errorCount = 0;
                        }
                        else autorefreshCount++;

                        foreach (var item in combatCount.ToList())
                        {
                            if (combatCount[item.Key] >= Config.Instance.CombatTextSendLevel) combatCount.Remove(item.Key);
                            else combatCount[item.Key]++;
                        }

                        if (_WannaTrade != null)
                        {
                            if (TradeTimer < 10) TradeTimer++;
                            else
                            {
                                Player.SendInfoMessage($"购买/出售请求已超时.");
                                _WannaTrade = null;
                                TradeTimer = 0;
                            }
                        }

                        Task.Delay(500).Wait();
                    }
                    catch (Exception ex)
                    {
                        TShock.Log.ConsoleError(ex.Message);
                    }
                }
            });
        }
        public void UseCommandSign(Models.ComandSign sign)
        {
            if (Player.ContainsData($"PSCommandCoolDown_{sign.ID}"))
            {
                var time = (DateTime.Now - Player.GetData<DateTime>($"PSCommandCoolDown_{sign.ID}")).TotalMilliseconds / 1000.0;
                if (time > sign.CoolDown)
                {
                    Player.SendCombatText($"命令标牌尚未冷却. 剩余 {sign.CoolDown - time:0.0} 秒.", Color.White);
                    return;
                }
                else
                {
                    Player.RemoveData($"PSCommandCoolDown_{sign.ID}");
                }
            }
            else
            {
                Player.SetData($"PSCommandCoolDown_{sign.ID}", DateTime.Now);
                return;
            }
            if (sign.Cost != 0)
            {
                if (UnifiedEconomyFramework.UEF.Balance(Player.Name) < sign.Cost)
                {
                    Player.SendCombatText($"你的余额不足以支付此命令标牌使用费用.", Color.White);
                    return;
                }
                else UnifiedEconomyFramework.UEF.MoneyDown(Player.Name, sign.Cost);
            }
            var group = Player.Group;
            if (sign.IgnorePermissions)
                Player.Group = new SuperAdminGroup();
            sign.Commands.ForEach(c =>
            {
                Commands.HandleCommand(Player, c.Replace("{name}", Player.Name));
            });
            Player.Group = group;
        }
    }
}
