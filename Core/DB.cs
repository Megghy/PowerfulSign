using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using PowerfulSign.Models;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace PowerfulSign.Core
{
    public class DB
    {
        public static void TryCreateTable()
        {
            try
            {
                SqlTable sqlTable = new("PowerfulSign", new SqlColumn[]
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
                new SqlTableCreator(db, queryBuilder2).EnsureTableStructure(sqlTable);
                CheckSignImport();
            }
            catch (Exception ex) { TShock.Log.Error(ex.Message); }
            //Main.sign = new Sign[1000];
        }
        public static bool CheckSignImport()
        {
            if (RunSQL($"SELECT * FROM PowerfulSign WHERE WorldID='{Main.worldID}'").Read())
            {
                TShock.Log.ConsoleInfo($"<PowerfulSign> 正在读入标牌数据...");
                Data.Signs = GetAllSign();
                TShock.Log.ConsoleInfo($"<PowerfulSign> 载入 {Data.Signs.Count} 条标牌数据.");
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
        public static List<SignBase> GetAllSign()
        {
            var reader = RunSQL($"SELECT * FROM PowerfulSign WHERE WorldID='{Main.worldID}';");
            var list = new List<SignBase>();
            while (reader.Read())
            {
                try
                {
                    var friends = reader.Get<string>("Friends") ?? "";
                    var friendsList = new List<int>();
                    if (friends.Contains(",")) friends.Split(',').ForEach(f => friendsList.Add(int.Parse("f")));
                    else if (int.TryParse(friends, out int i)) friendsList.Add(i);
                    list.Add(new(reader.Get<int>("SignID"), reader.Get<int>("X"), reader.Get<int>("Y"), reader.Get<string>("Text"), reader.Get<int>("Owner"), friendsList, reader.Get<int>("CanEdit") == 0));
                }
                catch (Exception ex) { TShock.Log.ConsoleError(ex.Message); }
            }
            return list;
        }
        public static SignBase AddSign(SignBase sign)
        {
            try
            {
                if (PowerfulSignAPI.TryGetSign(sign.X, sign.Y, out var temp))
                    return temp;
                using (RunSQL($"INSERT INTO PowerfulSign (Owner,Friends,X,Y,Text,CanEdit,WorldID) VALUES (@0,@1,@2,@3,@4,@5,@6)", new object[] {
                    sign.Owner,
                    string.Join(",", sign.Friends),
                    sign.X,
                    sign.Y,
                    sign.Text ?? "",
                    sign.CanEdit ? 0 : 1,
                    Main.worldID
                })) ;
                using (var reader = RunSQL($"SELECT MAX(SignID) FROM PowerfulSign;"))
                {
                    reader.Read();
                    sign.ID = reader.Get<int>("MAX(SignID)");
                    if (!Data.Signs.Any(s => s.ID == sign.ID))
                        Data.Signs.Add(sign);
                    return sign;
                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.InnerException is null ? ex.Message : ex.InnerException.Message);
                return null;
            }
        }
        public static void DelSign(Models.SignBase sign)
        {
            try
            {
                RunSQL($"DELETE FROM PowerfulSign WHERE SignID={sign.ID};");
                if (Data.Signs.Any(s => s.ID == sign.ID))
                    Data.Signs.Remove(sign);
            }
            catch { }
        }
        public static void UpdateSign(Models.SignBase sign)
        {
            try
            {
                RunSQL($"UPDATE PowerfulSign SET Text=@0,Friends=@1,CanEdit=@2,Owner=@3 WHERE SignID='{sign.ID}';", new object[] {
                    sign.Text ?? "",
                    string.Join(",", sign.Friends),
                    sign.CanEdit ? 0 : 1,
                    sign.Owner
                });
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.InnerException == null ? ex.Message : ex.InnerException.Message);
            }
        }
    }
}
