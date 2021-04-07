using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Xna.Framework;
using TShockAPI;
using TShockAPI.DB;

namespace PowerfulSign
{
    public class PSPlayer
    {
        public PSPlayer(TSPlayer plr)
        {
            TSP = plr;
            Initialize();
        }
        TSPlayer TSP { get; set; }
        UserAccount Account { get { return TSP.Account ?? new UserAccount() { ID = -1 }; } }
        public int LastSignIndex;
        public PSSign VisitingSign;

        PSSign _WannaTrade;
        int TradeTimer = 0;
        public PSSign WannaTrade
        {
            get
            {
                return _WannaTrade;
            }
            set { _WannaTrade = value; TradeTimer = 0; }
        }
        public void Initialize()
        {
            TSP.SendSignDataInCircle(PSPlugin.Config.RefreshRadius);
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
                while (TSP.Active)
                {
                    try
                    {
                        var position = TSP.TPlayer.position;
                        var plrrect = new Rectangle((int)(position.X / 16 - PSPlugin.Config.CombatTextRange), (int)(position.Y / 16 - PSPlugin.Config.CombatTextRange), 2 + PSPlugin.Config.CombatTextRange * 2, 3 + PSPlugin.Config.CombatTextRange * 2);
                        PSPlugin.SignList.Where(s => plrrect.Intersects(new Rectangle(s.X, s.Y, 2, 2))).ForEach(s =>
                        {
                            if (!combatCount.ContainsKey(s.ID))
                            {
                                TSP.SendCombatText(s.CombatText, Color.White, s.X, s.Y);
                                combatCount.Add(s.ID, 0);
                            }
                        });
                        if (autorefreshCount >= PSPlugin.Config.AutoRefreshLevel)
                        {
                            TSP.SendSignDataInCircle(PSPlugin.Config.RefreshRadius);
                            autorefreshCount = 0;
                        }
                        else autorefreshCount++;

                        if (errorCount >= 4)
                        {
                            PSPlugin.SignList.Where(s => s.Owner == Account.ID && s.Error).ForEach(s => TSP.SendCombatText($"[C/66D093:<PowerfulSign>]\n标牌无效", Color.Red, s.X, s.Y));
                            errorCount = 0;
                        }
                        else autorefreshCount++;

                        foreach (var item in combatCount.ToList())
                        {
                            if (combatCount[item.Key] >= PSPlugin.Config.CombatTextSendLevel) combatCount.Remove(item.Key);
                            else combatCount[item.Key]++;
                        }

                        if (_WannaTrade != null)
                        {
                            if (TradeTimer < 10) TradeTimer++;
                            else
                            {
                                TSP.SendInfoMessage($"[C/66D093:<PowerfulSign>] 购买/出售请求已超时.");
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
    }
}
