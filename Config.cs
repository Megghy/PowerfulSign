using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TShockAPI;
using TShockAPI.Hooks;

namespace PowerfulSign
{
    public class Config
    {
        public void Load(ReloadEventArgs args = null)
        {
            if (!File.Exists(Path.Combine(TShock.SavePath, "PowerfulSign.json")))
            {
                JsonSerializer serializer = new JsonSerializer();
                TextReader tr = new StringReader(JsonConvert.SerializeObject(PSPlugin.Config));
                JsonTextReader jtr = new JsonTextReader(tr);
                object obj = serializer.Deserialize(jtr);
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,//格式化缩进
                    Indentation = 4,  //缩进四个字符
                    IndentChar = ' '  //缩进的字符是空格
                };
                serializer.Serialize(jsonWriter, obj);
                FileTools.CreateIfNot(Path.Combine(TShock.SavePath, "PowerfulSign.json"), textWriter.ToString());
            }
            try
            {
                PSPlugin.Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path.Combine(TShock.SavePath, "PowerfulSign.json")));
                TShock.Log.ConsoleInfo($"[C/66D093:<PowerfulSign>] 成功读取配置文件.");
            }
            catch (Exception ex) { TShock.Log.Error(ex.Message); TShock.Log.ConsoleError("[C/66D093:<PowerfulSign>] 读取配置文件失败."); }
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
            public string Shop = "[商店]\n{type}\n{item.name}, 每组({item.stack}个) {price} {moneyname}.\n当前库存剩余 {inventory} 个.";
            public string Command = "这是一个命令标牌. 使用需要耗费 {cost} 经验.";
        }
        [JsonProperty]
        public PromptTexts DefaultPromptText = new PromptTexts();
        public class CombatTexts
        {
            public string Normal = "{text}";
            public string Shop = "[商店]\n{type}\n{item.name}, 每组({item.stack}个) {price} {moneyname}.\n当前库存剩余 {inventory} 个.";
            public string Command = "这是一个命令标牌. 使用需要耗费 {cost} 经验.";
        }
        [JsonProperty]
        public CombatTexts DefaultCombatText = new CombatTexts();
    }
}
