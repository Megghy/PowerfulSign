using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace PowerfulSign
{
    class DB
    {
        public static void TryCreateTable()
        {
            try
            {
                SqlTable sqlTable = new SqlTable("PowerfulSign", new SqlColumn[]
                {
                    new SqlColumn("SignID", MySql.Data.MySqlClient.MySqlDbType.Int32)
                    {
                        Primary = true,
                        AutoIncrement = true
                    },
                    new SqlColumn("Owner", MySql.Data.MySqlClient.MySqlDbType.Int32),
                    new SqlColumn("Friends", MySql.Data.MySqlClient.MySqlDbType.Text),
                    new SqlColumn("Text", MySql.Data.MySqlClient.MySqlDbType.Text),
                    new SqlColumn("CanEdit", MySql.Data.MySqlClient.MySqlDbType.Int32){ DefaultValue = "1"}, //0为true, 1为false
                    new SqlColumn("X", MySql.Data.MySqlClient.MySqlDbType.Int32),
                    new SqlColumn("Y", MySql.Data.MySqlClient.MySqlDbType.Int32),
                    new SqlColumn("WorldID", MySql.Data.MySqlClient.MySqlDbType.Int32)
                });
                IDbConnection db = TShock.DB;
                IQueryBuilder queryBuilder2;
                if (DbExt.GetSqlType(TShock.DB) != SqlType.Sqlite)
                {
                    IQueryBuilder queryBuilder = new MysqlQueryCreator();
                    queryBuilder2 = queryBuilder;
                }
                else
                {
                    IQueryBuilder queryBuilder = new SqliteQueryCreator();
                    queryBuilder2 = queryBuilder;
                }
                if (new SqlTableCreator(db, queryBuilder2).EnsureTableStructure(sqlTable)) Utils.ImportSign(); //导入本地已有标牌
                else CheckSignImport(); //读取所有标牌或新地图导入
            }
            catch (Exception ex) { TShock.Log.Error(ex.Message); }
        }
        public static bool CheckSignImport()
        {
            if (RunSQL($"SELECT * FROM PowerfulSign WHERE WorldID='{Main.worldID}'", new object[] {  }).Read())
            {
                GetAllSign();
                return true;
            }
            else
            {
                Utils.ImportSign();
                return false;
            }
        }
        public static QueryResult RunSQL(string sql, object[] args = null)
        {
            return args == null ? DbExt.QueryReader(TShock.DB, sql) : DbExt.QueryReader(TShock.DB, sql, args);
        }
        public static List<PSSign> GetAllSign()
        {
            TShock.Log.ConsoleInfo($"[C/66D093:<PowerfulSign>] 正在读入标牌数据...");
            var reader = RunSQL($"SELECT * FROM PowerfulSign WHERE WorldID='{Main.worldID}';");
            var list = new List<PSSign>();
            while (reader.Read())
            {
                try
                {
                    var friends = reader.Get<string>("Friends") ?? "";
                    var friendsList = new List<int>();
                    if (friends.Contains(",")) friends.Split(',').ForEach(f => friendsList.Add(int.Parse("f")));
                    else if (int.TryParse(friends, out int i)) friendsList.Add(i);
                    list.Add(new PSSign(reader.Get<int>("Owner"), friendsList, reader.Get<int>("X"), reader.Get<int>("Y"), reader.Get<string>("Text"), reader.Get<int>("SignID"), reader.Get<int>("CanEdit") == 0));
                }
                catch (Exception ex) { TShock.Log.ConsoleError(ex.Message); }
            }
            PSPlugin.SignList = list;
            TShock.Log.ConsoleInfo($"[C/66D093:<PowerfulSign>] 载入 {PSPlugin.SignList.Count} 条标牌数据.");
            return list;
        }
        public static long AddSign(PSSign sign)
        {
            try
            {
                if (Utils.TryGetSign(sign.X, sign.Y, out var temp))
                {
                    return -1;
                }
                using (RunSQL($"INSERT INTO PowerfulSign (Owner,Friends,X,Y,Text,CanEdit,WorldID) VALUES (@0,@1,@2,@3,@4,@5,@6)", new object[] {
                    sign.Owner,
                    string.Join(",", sign.Friends),
                    sign.X,
                    sign.Y,
                    sign.Text ?? "",
                    sign.CanEdit ? 0 : 1,
                    Main.worldID
                })) { }
                using (var reader = RunSQL($"SELECT MAX(SignID) FROM PowerfulSign;"))
                {
                    reader.Read();
                    sign.ID = reader.Get<int>("MAX(SignID)");
                    PSPlugin.SignList.Add(sign);
                    return sign.ID;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
                return -1;
            }
        }
        public static void DelSign(PSSign sign)
        {
            try {
                RunSQL($"DELETE FROM PowerfulSign WHERE SignID={sign.ID};");
                PSPlugin.SignList.Remove(sign);
            }
            catch { }
        }
        public static void UpdateSign(PSSign sign)
        {
            try
            {
                RunSQL($"UPDATE PowerfulSign SET Text=@0,Friends=@1,CanEdit=@2 WHERE SignID='{sign.ID}';", new object[] {
                    sign.Text ?? "",
                    string.Join(",", sign.Friends),
                    sign.CanEdit ? 0 : 1,
                });
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }
    }
}
