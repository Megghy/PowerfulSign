using System;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.Hooks;

namespace PowerfulSign
{
    public class Config
    {
        private static Config _instance;
        public static Config Instance
        {
            get
            {
                _instance ??= Read();
                return _instance;
            }
        }
        public static Config Read(ReloadEventArgs args = null)
        {
            _instance = null;
            if (!File.Exists(Path.Combine(TShock.SavePath, "PowerfulSign.json")))
                FileTools.CreateIfNot(Path.Combine(TShock.SavePath, "PowerfulSign.json"), JsonConvert.SerializeObject(new(), Formatting.Indented));
            try
            {
                TShock.Log.ConsoleInfo($"<PowerfulSign> 成功读取配置文件.");
                return JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path.Combine(TShock.SavePath, "PowerfulSign.json")));
            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.Message);
                TShock.Log.ConsoleError("读取配置文件失败.");
                return null;
            }
        }
        [JsonProperty]
        public string MoneyName = "$";
        [JsonProperty]
        public int RefreshRadius = 200;
        [JsonProperty]
        public bool AutoRefresh = true;
        [JsonProperty]
        public int AutoRefreshLevel = 30;
        [JsonProperty]
        public int CombatTextRange = 1;
        [JsonProperty]
        public int CombatTextSendLevel = 5;
        public class PromptTexts
        {
            public string Normal = "{text}";
            public string Shop = "[商店]\n{type}\n{shop.name}, 每组({shop.stack}个) {shop.price} {moneyname}.";
            public string Command = "这是一个命令标牌,包含 {command.count} 条命令.\n冷却时间 {command.cooldown} s.\n使用需要耗费 {command.cost} {moneyname}.";
        }
        [JsonProperty]
        public PromptTexts DefaultPromptText = new PromptTexts();
        public class CombatTexts
        {
            public string Normal = "{text}";
            public string Shop = "[商店]\n{type}\n{shop.name}, 每组({shop.stack}个) {shop.price} {moneyname}.\n当前库存剩余 {shop.inventory} 个.";
            public string Command = "这是一个命令标牌,包含 {command.count} 条命令.\n冷却时间 {command.cooldown} s.\n使用需要耗费 {command.cost} {moneyname}.";
        }
        [JsonProperty]
        public CombatTexts DefaultCombatText = new CombatTexts();
    }
}
