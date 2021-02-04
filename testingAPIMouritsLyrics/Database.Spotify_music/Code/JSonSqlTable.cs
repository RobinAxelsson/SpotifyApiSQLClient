using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace musicAPI
{
    //This class is an experiment to create a JSON data-format that parses easily to a SQL-table
    public class JSonSqlTable
    {
        public string[] ColumnNames { get; set; }
        public Program.SpotifyType _SpotifyType;
        public List<List<object>> RowList { set; get; } = new List<List<object>>();
        public static List<object> GetSpotifyValues(JObject data, Program.SpotifyType spotifyType)
        {
            var objects = new List<object>();

            switch (spotifyType)
            {
                case Program.SpotifyType.artists:
                    objects.Add(data["id"]);
                    objects.Add(data["name"]);
                    objects.Add(data["followers"]["total"]);
                    objects.Add(data["popularity"]);
                    try
                    {
                        objects.Add(data["images"][0]["url"]);
                    }
                    catch (Exception)
                    {
                        object obj = new object();
                        obj = null;
                        objects.Add(obj);
                    }
                    break;
                case Program.SpotifyType.albums:
                   objects.Add(data["id"]);
                    objects.Add(data["name"]);
                    objects.Add(data["label"]);
                    objects.Add(data["release_date"]);
                    objects.Add(data["popularity"]);
                    objects.Add(data["images"][0]["url"]);
                    break;
                case Program.SpotifyType.tracks:
                    objects.Add(data["id"]);
                    objects.Add(data["name"]);
                    objects.Add(data["duration_ms"]);
                    objects.Add(data["popularity"]);
                    objects.Add(data["explicit"]);
                    break;
                default:
                    break;
            }

            return objects;
        }
        //public string CommandsToFile_INSERT(string TableName)
        //{
        //    if (ColumnNames == null || TableName == null) throw new Exception("ColumnNames or TableName is null!");

        //    string filePath = $@"C:\Users\axels\source\repos\testingAPIMouritsLyrics\testingAPIMouritsLyrics\Database.Spotify_music\Table.{_SpotifyType.ToString()}\3. insert.{_SpotifyType.ToString()}\insert.{_SpotifyType.ToString()}.txt";
        //    filePath = Program.GetUniqueFilePath(filePath);
        //    string text = String.Empty;

        //    foreach (var row in RowList)
        //    {
        //        text += Program.CreateInsertCommand(TableName, ColumnNames.ToList(), row)+ Environment.NewLine;
        //    }
        //    File.WriteAllText(filePath, text);
        //    return filePath;
        //}
        public JSonSqlTable() { }
        public JSonSqlTable(string directoryPath, Program.SpotifyType spotifyType)
        {
            if (!Directory.Exists(directoryPath)) throw new Exception("Directory does not exist!");
            DirectoryInfo targetFolder = new DirectoryInfo(directoryPath);
            var files = targetFolder.GetFiles();

            if (spotifyType == Program.SpotifyType.albums)
            {
                ColumnNames = new[] {"[ID]","[Name]", "[Label]", "[Release_Date]", "[Popularity]", "[Image_Url]" };
            }
            if (spotifyType == Program.SpotifyType.artists)
            {
                ColumnNames = new[] { "[ID]", "[Name]", "[Followers]", "[Popularity]", "[Image_Url]" };
            }
            {
            if (spotifyType == Program.SpotifyType.tracks)
                ColumnNames = new[] { "[ID]", "[Name]", "[Duration_ms]", "[Popularity]", "[Explicit_Lyrics]" };
            }

            var sqlRows = new List<List<object>>();
            foreach (var file in files)
            {
                JObject jObject = JObject.Parse(File.ReadAllText(file.FullName));
                JArray jArray = (JArray)jObject[spotifyType.ToString()];
                foreach (JObject jRow in jArray)
                {                 
                    sqlRows.Add(GetSpotifyValues(jRow, spotifyType));
                }
            }
            RowList = sqlRows;
            _SpotifyType = spotifyType;
        }
        public void SaveToJsonTable()
        {
            string filepath = Program.GetUniqueFilePath($@"C:\Users\axels\source\repos\testingAPIMouritsLyrics\testingAPIMouritsLyrics\Database.Spotify_music\Table.{_SpotifyType.ToString()}\2. JSonTable.{_SpotifyType.ToString()}\Refined.{_SpotifyType.ToString()}.json");
            DataConversion.JsonSerialize(this, filepath);
        }
        public static JSonSqlTable LoadFromFile(string filePath)
        {
            return (JSonSqlTable)DataConversion.JsonDeserializeToType(typeof(JSonSqlTable), filePath);
        }
        //public SqlConnection CreateConnecton()
        //{
        //    return new SqlConnection($@"Data Source=(local)\SQLExpress;Initial Catalog={DatabaseName};Integrated Security=SSPI");
        //}
        public List<string> SqlCsvRows()
        {
            var CsvRows = new List<string>();

            string row = string.Empty;
            var vals = new List<string>();
            foreach (List<object> rowObjects in RowList)
            {
                foreach (var obj in rowObjects)
                {
                    string val = string.Empty;
                    if (obj.GetType() == typeof(string))
                    {
                        val = $"\'{obj}\'";
                    }
                    else
                    {
                        val = obj.ToString();
                    }
                    vals.Add(val);
                }
                CsvRows.Add(String.Join(',', vals));
            }
            return CsvRows;
        }
        //public void INSERT_ALL_IN_DATABASE()
        //{
        //    using SqlConnection connection = CreateConnecton();
        //    {
        //        string script = String.Empty;
        //        foreach (var row in RowList)
        //        {
        //            script = $"";
        //            SqlCommand command = new SqlCommand(script, connection);
        //            connection.Open();
        //            Console.WriteLine($"Number of rows affected: {command.ExecuteNonQuery()}.");
        //            connection.Close();
        //        }
        //    }
        //}
        
    }
}