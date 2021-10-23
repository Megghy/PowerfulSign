using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using PowerfulSign.Core;
using TShockAPI;

namespace PowerfulSign.Models
{
    public class SignBase
    {
        public SignBase(int x, int y, string text, int ownerID)
        {
            X = x;
            Y = y;
            Text = text;
            Owner = ownerID;
            Friends = new();
        }
        public SignBase(int id, int x, int y, string text, int ownerID, List<int> friends = null, bool canEdit = true)
        {
            ID = id;
            X = x;
            Y = y;
            Text = text;
            Owner = ownerID;
            Friends = friends ?? new();
            CanEdit = canEdit;
            CheckText(text);
        }
        #region 基本变量
        public int ID { get; set; }
        public int Owner { get; set; }
        public List<int> Friends { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Color Color { get; set; } = Color.White;
        public string Text { get; set; }
        public bool CanEdit { get; set; }
        public bool HasError { get; set; }
        internal string OriginPromptText;
        public string PromptText => ReplaceVariable(OriginPromptText);
        public string OriginCombatText;
        public string CombatText => ReplaceVariable(OriginCombatText);
        #endregion
        public virtual string Type => "normal";
        public void Update()
        {
            DB.UpdateSign(this);
            TShock.Players.Where(p => p != null).ForEach(p => p.SendSignDataDirect(this, false, 0));
        }
        public virtual bool CheckText(string text)
        {
            return true;
        }
        public bool IsSpecialText(string text, out List<string> lines, out string type, out TSPlayer owner)
        {
            lines = Text.Split("\n");
            type = "";
            owner = null;
            if (lines[0].StartsWith("[") && lines[0].EndsWith("]"))
            {
                type = lines[0].SearchString("[", "]").ToLower();
                owner = TShock.Players.FirstOrDefault(p => p != null && p.AccountEX().ID == Owner) ?? new TSPlayer(-1) { };
                return true;
            }
            return false;
        }
        public virtual string ReplaceVariable(string text)
        {
            text = (text ?? Config.Instance.DefaultPromptText.Normal)
                    .Replace("\\n", "\n")
                    .Replace("{moneyname}", Config.Instance.MoneyName)
                    .Replace("{owner}", TShock.UserAccounts.GetUserAccountByID(Owner) is { } account ? account.Name : "Unknown")
                    .Replace("{text}", Text);
            return text;
        }
        public virtual void OnUse(TSPlayer user)
        {
            if ((user.Account is null && Owner != -1) || Owner != user.Account.ID || (!CanEdit && !Friends.Contains(user.Account.ID) && !user.HasPermission("ps.admin.edit")))
                user.SendErrorMessage($"你没有打开此标牌的权限.");
            else
                user.SendSignDataVisiting(this);
        }
        public T To<T>() where T : SignBase => this as T;
    }
}
