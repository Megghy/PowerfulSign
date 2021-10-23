using System.Collections.Generic;
using TShockAPI;

namespace PowerfulSign.Models
{
    public class ComandSign : SignBase
    {
        public ComandSign(int id, int x, int y, string text, int ownerID, List<int> friends = null, bool canEdit = true) : base(id, x, y, text, ownerID, friends, canEdit)
        {
        }
        public const int CLICK = 0;
        public const int CLOST = 1;
        public const int BOTH = 2;
        public override string Type => "command";
        public int CommandType { get; set; } = 0;
        public bool IgnorePermissions { get; set; }
        public List<string> Commands { get; set; }
        public long CoolDown { get; set; }
        public int Cost { get; set; }
        public override bool CheckText(string text)
        {
            return base.CheckText(text);
        }
        public override string ReplaceVariable(string text)
        {
            base.ReplaceVariable(text);
            return (text ?? Config.Instance.DefaultCombatText.Command)
                    .Replace("{command.cost}", Cost.ToString())
                    .Replace("{command.cooldown}", (CoolDown / (double)1000).ToString("0.00"))
                    .Replace("{command.count}", Commands.Count.ToString());
        }
        public override void OnUse(TSPlayer user)
        {
            if (user.Account is null || Owner != user.Account.ID)
            {
                if (CommandType == CLICK || CommandType == BOTH)
                    user.PSPlayer().UseCommandSign(this);
                else
                    user.SendErrorMessage($"你没有编辑此标牌的权限.");
            }
            else
                user.SendSignDataVisiting(this);
        }
    }
}
