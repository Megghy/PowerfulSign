using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TShockAPI;

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
        public int LastSignIndex;
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
                while (TSP.Active)
                {
                    try
                    {
                        var position = TSP.TPlayer.position;
                        var plrrect = new Rectangle((int)(position.X / 16 - 2), (int)(position.Y / 16 - 2), 4, 5);
                        PSPlugin.SignList.Where(s => plrrect.Intersects(new Rectangle(s.X, s.Y, 2, 2))).ForEach(s =>
                        {
                            //TSP.SendCombatText(s.Owner == (TSP.Account ?? new TShockAPI.DB.UserAccount() { ID = -2}).ID ? s._CombatText : s.CombatText, Color.White, s.X, s.Y);
                            TSP.SendCombatText(s.CombatText, Color.White, s.X, s.Y);
                        });
                        if (autorefreshCount >= PSPlugin.Config.AutoRefreshLevel)
                        {
                            TSP.SendSignDataInCircle(PSPlugin.Config.RefreshRadius);
                            autorefreshCount = 0;
                        }
                        else autorefreshCount++;
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
