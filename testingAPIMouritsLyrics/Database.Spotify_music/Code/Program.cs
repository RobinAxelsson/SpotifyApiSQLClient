using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace musicAPI
{
    //This file contains the logic for the main application.
    //FullSpotifySequence() is the method with highest abstraction
    public class SqlTable
    {
        //private static void SQL()
        //{
        //    var sqlConnenction = new SqlConnection();
        //    var webClient = new WebClient();
        //    var manualResetEvent = new ManualResetEvent(false);
        //    var fileStream = new FileStream();
        //    var command = new SqlCommand("select * from Customers", sqlConnenction);
        //    var reader = new SqlDataReader();
        //    var adapter = new SqlDataAdapter();
        //    command.ExecuteNonQuery();
        //    SqlParameter param = new SqlParameter();
        //    param.ParameterName = "@City";
        //    param.Value = inputCity;
        //    var stream = new Stream(); abstract class
        //    querystring, connectionsstring
        //}
        public List<string> DataProperties { set; get; } = new List<string>();
        public List<List<object>> DataBodies { set; get; } = new List<List<object>>();
        public static void CallAndSaveTo(string testJsonPath)
        {
            var sqlTable = CallToSqlTable("SELECT * FROM SONG", new List<int> { 2 });
            DataConversion.JsonSerialize(sqlTable, testJsonPath);
        }
        public static SqlConnection sqlConnection = new SqlConnection(@"Data Source=(local)\SQLExpress;Initial Catalog=Music;Integrated Security=SSPI");
        public static SqlTable CallToSqlTable(string sqlCommand, List<int> skipColumnIndexes = null, int? rowLimit = null)
        {
            sqlConnection.Open();
            SqlCommand command = new SqlCommand(sqlCommand, sqlConnection);
            SqlDataReader reader = command.ExecuteReader();

            var sqlTable = new SqlTable();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (skipColumnIndexes.Contains(i)) continue;
                sqlTable.DataProperties.Add($"{reader.GetName(i)}");
            }

            int readRows = 0;
            while (reader.Read() && (rowLimit == null || readRows < rowLimit))
            {
                var newObjList = new List<object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (skipColumnIndexes.Contains(i)) continue;
                    newObjList.Add(reader[i]);
                }
                sqlTable.DataBodies.Add(newObjList);
                readRows++;
            }
            return sqlTable;
        }

    }
    public class DataConversion
    {
        public static void JsonSerialize(object data, string filePath)
        {
            var jsonSerializer = new JsonSerializer();
            if (File.Exists(filePath)) File.Delete(filePath);
            using var sw = new StreamWriter(filePath);
            using var jsonWriter = new JsonTextWriter(sw);
            jsonSerializer.Serialize(jsonWriter, data);
        }
        public static object JsonDeserializeToType(Type dataType, string filePath)
        {
            JObject obj = null;
            JsonSerializer jsonSerializer = new JsonSerializer();
            if (File.Exists(filePath))
            {
                using StreamReader sr = new StreamReader(filePath);
                using JsonReader jsonReader = new JsonTextReader(sr);
                obj = jsonSerializer.Deserialize(jsonReader) as JObject;
            }
            else
            {
                throw new Exception("No file");
            }
            return obj.ToObject(dataType);
        }
        public static void SqlStringifyJson(string filePath)
        {
            if (!File.Exists(filePath)) throw new Exception("Invalid filePath");
            string input = File.ReadAllText(filePath);
            string output = input.Replace("'", "''");
            int count = 0;
            string suffix = "SQLstring";
            string newPath = filePath + suffix + ".json";
            while (File.Exists(newPath))
            {
                count++;
                suffix = "SQLstring" + count;
                newPath = filePath + suffix + ".json";
            }
            File.WriteAllText(newPath, output);
        }
    }
    public static class Program
    {
        public static SqlConnection sqlConnection = new SqlConnection(@"Data Source=(local)\SQLExpress;Initial Catalog=Music;Integrated Security=SSPI");
        public static string ReplaceTextInFile(List<(string oldString, string newString)> replace, string fileSource)
        {
            if (!File.Exists(fileSource)) throw new Exception("File not found!");
            string text = File.ReadAllText(fileSource);

            foreach (var pair in replace)
            {
                text = text.Replace(pair.oldString, pair.newString);
            }
            string outPutPath = GetUniqueFilePath(fileSource, ".replace");
            File.WriteAllText(outPutPath, text);
            return outPutPath;
        }
        public static List<object> GetValuesFromJObject(JObject data, Program.SpotifyType spotifyType)
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
        public static string CreateInsertCommandFromCsv(string table, List<string> columns, string filePath, string outputDirectory)
        {
            var lines = File.ReadAllLines(filePath);
            string insertCommands = String.Empty;
            foreach (var line in lines)
            {
                var ids = line.Split(';');
                insertCommands += $"INSERT {table}(";
                insertCommands += String.Join(", ", columns) + ") VALUES(";

                for (int i = 0; i < ids.Length; i++)
                {
                    ids[i] = ids[i].Replace("'", "''");
                    ids[i] = $"N'{ids[i]}'";
                }
                insertCommands += String.Join(", ", ids) + ')' + Environment.NewLine;
            }
            string outputFilepath = Path.Combine(outputDirectory, $"{table}.INSERT.csv");
            outputFilepath = GetUniqueFilePath(outputFilepath);
            File.WriteAllText(outputFilepath, insertCommands);

            return outputFilepath;
        }
        public static string[] Spotify_Artist_Columns = new[] { "[ID]", "[Name]", "[Followers]", "[Popularity]", "[Image_Url]" };
        public static string[] Spotify_Track_Columns = new[] { "[ID]", "[Name]", "[Duration_ms]", "[Popularity]", "[Explicit_Lyrics]" };
        public static string[] Spotify_Album_Columns = new[] { "[ID]", "[Name]", "[Label]", "[Release_Date]", "[Popularity]", "[Image_Url]" };
        public enum DB_Term
        {
            ArtistTrack,
            TrackAlbum,
            Spotify_Track,
            Spotify_Artist,
            Spotify_Album,
            Spotify_Music,
            ICAStore
        }
        public static List<List<object>> GetObjectRowsFromJson(SpotifyType spotifyType, string directoryFilePath)
        {
            if (!Directory.Exists(directoryFilePath)) throw new Exception("Directory does not exist!");
            DirectoryInfo inputfolder = new DirectoryInfo(directoryFilePath);
            var files = inputfolder.GetFiles();
            var sqlRows = new List<List<object>>();
            foreach (var file in files)
            {
                JObject jObject = JObject.Parse(File.ReadAllText(file.FullName));
                JArray jArray = (JArray)jObject[spotifyType.ToString()];
                foreach (JObject jRow in jArray)
                {
                    sqlRows.Add(GetValuesFromJObject(jRow, spotifyType));
                }
            }
            return sqlRows;
        }
        public static string CreateInsertCommands(DB_Term table, string[] columns, List<List<object>> objectRows, string outputFilePath)
        {
            string Text = String.Empty;
            string insertCommand = String.Empty;
            foreach (var row in objectRows)
            {
                if (columns.Length != row.Count) throw new Exception("Columns and object-count doesn't match");

                insertCommand = $"INSERT {table.ToString()}(";
                insertCommand += String.Join(", ", columns) + ") VALUES(";
                string value = String.Empty;
                var values = new List<string>();
                foreach (var obj in row)
                {
                    if (obj == null)
                    {
                        values.Add("NULL");
                        continue;
                    }
                    try
                    {
                        var type = ((JValue)obj).Type;
                        if (type == JTokenType.String)
                        {

                            if (int.TryParse(obj.ToString().Replace("-", ""), out int result))
                            {
                                value = $"CAST(N'{obj.ToString()}' AS Date)";
                                values.Add(value);
                                continue;
                            }

                            value = obj.ToString().Replace("'", "''");
                            value = $"N'{value}'";
                        }
                        else if (type == JTokenType.Boolean)
                        {
                            if (value.ToString() == "true")
                            {
                                value = "1";
                            }
                            else
                            {
                                value = "0";
                            }
                        }
                        else
                        {
                            value = obj.ToString();
                        }
                    }
                    catch (Exception)
                    {

                        if (obj.GetType() == typeof(string))
                        {
                            value = obj.ToString().Replace("'", "''");
                            value = $"N'{value}'";
                        }
                        if (obj.GetType() == typeof(bool))
                        {
                            if ((bool)obj)
                            {
                                value = "1";
                            }
                            else
                            {
                                value = "0";
                            }
                        }
                        else
                        {
                            value = obj.ToString();
                        }
                    }

                    values.Add(value);
                }
                insertCommand += String.Join(", ", values.ToArray()) + ')' + Environment.NewLine;
                Text += insertCommand;
            }
            outputFilePath = GetUniqueFilePath(outputFilePath);
            File.WriteAllText(outputFilePath, Text);
            return outputFilePath;
        }
        public static void ExecuteSqlLines(string[] commandLines, DB_Term database)
        {
            string errorLine;
            int rowsEffected = 0;
            var errorlog = new List<string>();
            foreach (string line in commandLines)
            {
                using SqlConnection connection = new SqlConnection($@"Data Source=(local)\SQLExpress;Initial Catalog={database};Integrated Security=SSPI");
                {
                    SqlCommand command = new SqlCommand(line, connection);
                    connection.Open();
                    try
                    {
                        rowsEffected += command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        errorLine = $"ERROR with: {line}";
                        errorlog.Add(ex + Environment.NewLine + errorLine);
                        Console.WriteLine(ex);
                        Console.WriteLine(errorLine + Environment.NewLine);
                    }
                }
            }
            string header = $"{rowsEffected} rows effected of {commandLines.Length}.";
            Console.WriteLine(header);
            Console.WriteLine("Save to log? Y/N");
            string Y_N = Console.ReadLine();
            if (Y_N.ToUpper() == "Y" || Y_N.ToUpper() == "YES")
            {
                errorlog.Insert(0, database + ": " + header + Environment.NewLine + DateTime.Now);
                errorlog.Add(Environment.NewLine);
                errorlog.Add(Environment.NewLine);
                File.AppendAllLines(@"C:\Users\axels\source\repos\testingAPIMouritsLyrics\testingAPIMouritsLyrics\Database.Spotify_music\InsertLOG.txt", errorlog);
            }

        }
        public static void CreateAndExecuteSqlInsertCommand(DB_Term database, DB_Term table, SpotifyType spotifyType, string rawDataFolder, string outputFilePath)
        {
            string[] columntype;
            switch (spotifyType)
            {
                case SpotifyType.artists:
                    columntype = Spotify_Artist_Columns;
                    break;
                case SpotifyType.albums:
                    columntype = Spotify_Album_Columns;
                    break;
                case SpotifyType.tracks:
                    columntype = Spotify_Track_Columns;
                    break;
                default:
                    return;
            }
            string outputFilepath = CreateInsertCommands(table, columntype, GetObjectRowsFromJson(spotifyType, rawDataFolder), outputFilePath);
            ExecuteSqlLines(File.ReadAllLines(outputFilepath), database);
        }
        public static string SqlBoolConversion(string filePath)
        {
            return ReplaceTextInFile(new List<(string, string)> { ("True", "1"), ("False", "0") }, filePath);
        }
        public static void SpotifyListRequest(string title, string folderPath, string[] IdsInput, SpotifyType listRequest, out List<string> FilePaths)
        {
            FilePaths = new List<string>();

            if (!Directory.Exists(folderPath)) throw new Exception("Directory does not exist!");
            string listRequestString;
            string filePath = Path.Combine(folderPath, title + ".json");
            switch (listRequest)
            {
                case SpotifyType.artists:
                    listRequestString = "artists";
                    break;
                case SpotifyType.albums:
                    listRequestString = "albums";
                    break;
                case SpotifyType.tracks:
                    listRequestString = "tracks";
                    break;
                case SpotifyType.playlists:
                    listRequestString = "playlists";
                    break;
                default:
                    throw new Exception("Switch error?");
            }
            var Ids = IdsInput.ToList().Distinct().ToList();

            var client = new RestClient();

            if (Ids.Count == 1)
            {
                client.BaseUrl = new Uri($@"https://api.spotify.com/v1/{listRequestString}/{Ids[0]}");
            }
            else
            {
                while (Ids.Count > 50 || listRequest == SpotifyType.albums && Ids.Count > 20)
                {
                    if (listRequest == SpotifyType.albums)
                    {
                        var first20 = Ids.Take(20).ToList();
                        Ids = Ids.Except(first20).ToList();
                        SpotifyListRequest(title, folderPath, first20.ToArray(), listRequest, out FilePaths);
                    }
                    else
                    {
                        var first50 = Ids.Take(50).ToList();
                        Ids = Ids.Except(first50).ToList();
                        SpotifyListRequest(title, folderPath, first50.ToArray(), listRequest, out FilePaths);
                    }
                }
                var idConcat = String.Join("%2C", Ids.ToArray());
                client.BaseUrl = new Uri($@"https://api.spotify.com/v1/{listRequestString}?ids={idConcat}");
            }
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", SpotifyAuth);
            IRestResponse response = client.Execute(request);
            if (SpotifyRequestLog(response, title, out string log) != null)
            {
                Console.WriteLine(log);
                return;
            }
            filePath = GetUniqueFilePath(filePath);
            File.WriteAllText(filePath, response.Content);
            FilePaths.Add(filePath);            
        }
        public static JObject PingSpotify(string pingUri = @"https://api.spotify.com/v1/me")
        {
            string errorCode = String.Empty;
            string writtenCommand = String.Empty;
            string jSonResponse = String.Empty;
            string OAuth = string.Empty;

            while(errorCode != null)
            {
                var client = new RestClient { BaseUrl = new Uri(pingUri) };
                var request = new RestRequest(Method.GET);
                try
                {
                    OAuth = SpotifyAuth;
                }
                catch (Exception)
                {
                    Console.WriteLine("Hidden file is missing for oauth. Try changing absolute path or type manually.");
                }
                request.AddHeader("Authorization", OAuth);
                IRestResponse response = client.Execute(request);
                errorCode = SpotifyRequestLog(response, "Ping me", out string log, AppendToTextFile: false);
                jSonResponse = response.Content;

                if (errorCode != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("Type Spotify OAuth");
                    writtenCommand = Console.ReadLine();

                    if (writtenCommand.ToUpper() == "RETURN")
                    {
                        return null;
                    }
                    else if (writtenCommand.ToUpper() == "EXIT")
                    {
                        Environment.Exit(0);
                    }
                    else
                    {
                        SpotifyAuth = @$"Bearer {writtenCommand}";
                    }
                }
            }
            JObject jObj = new JObject();
            try
            {
                jObj = JObject.Parse(jSonResponse);
                Console.WriteLine();
                Console.WriteLine($"Hello {jObj["display_name"]}");
                Console.WriteLine($"Your ID: {jObj["id"]}");
            }
            catch (Exception)
            {
                jObj = null;
            }
            
           

            Console.WriteLine();
            File.WriteAllText(@"C:\Users\axels\source\repos\testingAPIMouritsLyrics\testingAPIMouritsLyrics\Database.Spotify_music\hidden.csv", SpotifyAuth);

            return jObj;
        }
        public static void OpenSpotifyPlayer(JObject jObj)
        {
            Console.WriteLine($"Open player? Y/N");
            string writtenCommand = Console.ReadLine();
            if (writtenCommand.ToUpper() == "Y" || writtenCommand.ToUpper() == "YES")
            {
                string url = jObj["external_urls"]["spotify"].ToString();
                OpenWeb(url);
            }
        }
        public static void OpenWeb(string url)
        {
            var psi = new ProcessStartInfo(@"C:\Program Files\Google\Chrome\Application\chrome.exe");
            psi.Arguments = url;
            Process.Start(psi);
        }
        public static JObject ArtistAlbumListRequest(string folderPath, string title, string artistId)
        {
            string filePath = Path.Combine(folderPath, title + ".json");
            var client = new RestClient($@"https://api.spotify.com/v1/artists/{artistId}/albums");
            var request = new RestRequest(Method.GET);
            request.AddHeader("Authorization", SpotifyAuth);
            IRestResponse response = client.Execute(request);
            if(SpotifyRequestLog(response, title, out string log) != null)
            {
                Console.WriteLine(log);
                return null;
            }
            filePath = GetUniqueFilePath(filePath);
            File.WriteAllText(filePath, response.Content);
            try
            {
                JObject jObject = JObject.Parse(response.Content);
                return jObject;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static string GetUniqueFilePath(string fileUri, string suffix = null)
        {
            int count = 1;
            string working = fileUri;
            if (suffix != null)
            {
                working = fileUri.Insert(fileUri.LastIndexOf('.'), suffix);
            }
            while (File.Exists(working))
            {
                working = fileUri.Insert(fileUri.LastIndexOf('.'), count.ToString());
                count++;
            }
            return working;
        }
        public static string CreateUniqueDirectory(string directoryUri)
        {
            int count = 1;
            directoryUri = directoryUri.Trim('\\');

            string working = directoryUri;
            while (Directory.Exists(working + '\\'))
            {
                working = directoryUri + count + '\\';
                count++;
            }
            working += '\\';
            var directory = new DirectoryInfo(working);
            directory.Create();
            return working;
        }
        public static string CreateSpotifyDataBase(string sourceFilePath)
        {
            string newFilePath = GetUniqueFilePath(sourceFilePath);

            var spotify = new SpotifyData();

            var albumDirectory = new DirectoryInfo(@"C:\Users\axels\source\repos\testingAPIMouritsLyrics\testingAPIMouritsLyrics\Spotify Raw Data\Clean Requests\Albums\");
            var artistDirectory = new DirectoryInfo(@"C:\Users\axels\source\repos\testingAPIMouritsLyrics\testingAPIMouritsLyrics\Spotify Raw Data\Clean Requests\Artists\");
            var trackDirectory = new DirectoryInfo(@"C:\Users\axels\source\repos\testingAPIMouritsLyrics\testingAPIMouritsLyrics\Spotify Raw Data\Clean Requests\Tracks\");

            foreach (var filePath in albumDirectory.GetFiles())
            {
                spotify.Append(SpotifyType.albums, filePath.FullName);
            }
            foreach (var filePath in artistDirectory.GetFiles())
            {
                spotify.Append(SpotifyType.artists, filePath.FullName);
            }
            foreach (var filePath in trackDirectory.GetFiles())
            {
                spotify.Append(SpotifyType.tracks, filePath.FullName);
            }
            spotify.MergeOrNew(newFilePath);
            return newFilePath;
        }
        public static void TrackJoinsID(string directoryPath, out List<string> trackArtistPairs, out List<string> trackAlbumPairs)
        {
            var trackArtistIDs = new List<string>();
            var trackAlbumIDs = new List<string>();

            if (!Directory.Exists(directoryPath)) throw new Exception("Directory does not exist!");
            var files = new List<FileInfo>();
            GetAllSubFiles(directoryPath, files);

            foreach (var file in files)
            {
                if (file.Extension != ".json") continue;
                JArray tracks;
                JObject jObject;
                try
                {
                    jObject = JObject.Parse(File.ReadAllText(file.FullName));
                    tracks = (JArray)jObject["tracks"];
                }
                catch (Exception)
                {
                    continue;
                }
                
                foreach (JObject track in tracks)
                {
                    JsonReader jReader = track.CreateReader();

                    List<string> artistIds = new List<string>();
                    List<string> albumIds = new List<string>();
                    string trackId = null;

                    while (jReader.Read())
                    {
                        if (jReader != null && jReader.Path.Split('.')[^1] == "uri")
                        {
                            string uri = track.Root.SelectToken(jReader.Path).ToString();
                            var uriArray = uri.Split(':');
                            if (uriArray[1] == "album")
                            {
                                albumIds.Add(uriArray[^1]);
                            }
                            if (uriArray[1] == "artist")
                            {
                                artistIds.Add(uriArray[^1]);
                            }
                            if (uriArray[1] == "track" && trackId == null) throw new Exception("Double track IDs?");
                            {
                                trackId = uriArray[^1];
                            }
                        }
                    }
                    albumIds.Distinct().ToList().ForEach(x => trackArtistIDs.Add(trackId + ';' + x));
                    artistIds.Distinct().ToList().ForEach(x => trackAlbumIDs.Add(trackId + ';' + x));
                }
            }
            trackArtistPairs = trackArtistIDs;
            trackAlbumPairs = trackAlbumIDs;
        }
        public static List<string> GetSpotifyTypeIDsFromJSonFile(string filePath, SpotifyType spotifytype)
        {
                var IDs = new List<string>();

                string Json = File.ReadAllText(filePath);
                JObject jObj = JObject.Parse(Json);
                JsonReader jReader = jObj.CreateReader();

                switch (spotifytype)
                {
                    case SpotifyType.artists:
                        while (jReader.Read())
                        {
                            if (jReader != null && jReader.Path.Split('.')[^1] == "uri")
                            {
                                string uri = jObj.SelectToken(jReader.Path).ToString();
                                var uriArray = uri.Split(':');
                                if (uriArray[1] == "artist")
                                {
                                    IDs.Add(uriArray[^1]);
                                }
                            }
                        }
                        break;
                    case SpotifyType.albums:
                        while (jReader.Read())
                        {
                            if (jReader != null && jReader.Path.Split('.')[^1] == "uri")
                            {
                                string uri = jObj.SelectToken(jReader.Path).ToString();
                                var uriArray = uri.Split(':');
                                if (uriArray[1] == "album")
                                {
                                    IDs.Add(uriArray[^1]);
                                }
                            }
                        }
                        break;
                    case SpotifyType.tracks:
                        while (jReader.Read())
                        {
                            if (jReader != null && jReader.Path.Split('.')[^1] == "uri")
                            {
                                string uri = jObj.SelectToken(jReader.Path).ToString();
                                var uriArray = uri.Split(':');
                                if (uriArray[1] == "track")
                                {
                                    IDs.Add(uriArray[^1]);
                                }
                            }
                        }
                        break;
                    default:
                        return null;
                }
            IDs = IDs.Distinct().ToList();
            return IDs;
        }
        public static void InsertDirectoryJoins(string directoryPath)
        {
            var trackArtistPairs = new List<string>();
            var trackAlbumPairs = new List<string>();
            TrackJoinsID(directoryPath, out trackArtistPairs, out trackAlbumPairs);
            Console.WriteLine("Track-Artist binds: {0}", trackArtistPairs.Count);
            Console.WriteLine("Track-Album binds: {0}", trackAlbumPairs.Count);


        }
        public static List<string> GetSpotifyTypeIDsFromJSonDirectory(string folderPath, string outputPath, SpotifyType spotifytype)
        {
            var folder = new DirectoryInfo(folderPath);
            var files = folder.GetFiles();
            var IDs = new List<string>();
            outputPath = GetUniqueFilePath(outputPath);

            foreach (var file in files)
            {
                var listIDs = GetSpotifyTypeIDsFromJSonFile(file.FullName, spotifytype);
                if (listIDs == null) continue;
                IDs.AddRange(listIDs);
            }
            IDs = IDs.Distinct().ToList();
            File.WriteAllLines(outputPath, IDs);
            return IDs;
        }
        public static List<string> GetSpotifyLocalTableIDs(DB_Term table)
        {
            var IDs = new List<string>();
            using SqlConnection sqlConnection = new SqlConnection($@"Data Source=(local)\SQLExpress;Initial Catalog={DB_Term.Spotify_Music};Integrated Security=SSPI");
            {
                string stringCommand = @$"SELECT ID FROM {table}";
                sqlConnection.Open();
                SqlCommand command = new SqlCommand(stringCommand, sqlConnection);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    IDs.Add(reader[0].ToString());
                }
            }
            return IDs;
        }
        public static void GetAllSubFiles(string uri, List<FileInfo> files)
        {
            DirectoryInfo directory = new DirectoryInfo(uri);
            files.AddRange(directory.GetFiles().ToList());

            foreach (var dir in directory.GetDirectories())
            {
                GetAllSubFiles(dir.FullName, files);
            }
        }
        public static void ExtractAllIDs(string searchDirectoryUri, string outputDirectoryUri)
        {
            outputDirectoryUri = CreateUniqueDirectory(outputDirectoryUri);

            string _b = Environment.NewLine;
            var files = new List<FileInfo>();
            GetAllSubFiles(searchDirectoryUri, files);

            int countArtist = 0;
            int countTrack = 0;
            int countAlbum = 0;
            int countPlaylist = 0;

            string artistString = string.Empty;
            string trackString = string.Empty;
            string playlistString = string.Empty;
            string albumString = string.Empty;

            foreach (var file in files)
            {
                try
                {
                    JObject jObj = JObject.Parse(File.ReadAllText(file.FullName));

                    var jReader = jObj.CreateReader();
                    while (jReader.Read())
                    {
                        if (jReader != null && jReader.Path.Split('.')[^1] == "uri")
                        {
                            string uri = jObj.Root.SelectToken(jReader.Path).ToString();
                            var uriArray = uri.Split(':');
                            if (uriArray[1] == "album")
                            {
                                var id = uriArray[^1] + _b;
                                if (!albumString.Contains(id))
                                {
                                    albumString += id;
                                    countAlbum++;
                                }
                            }
                            if (uriArray[1] == "artist")
                            {
                                var id = uriArray[^1] + _b;
                                if (!artistString.Contains(id))
                                {
                                    artistString += id;
                                    countArtist++;
                                }
                            }
                            if (uriArray[1] == "track")
                            {
                                var id = uriArray[^1] + _b;
                                if (!trackString.Contains(id))
                                {
                                    trackString += id;
                                    countTrack++;
                                }
                            }
                            if (uriArray[1] == "playlist")
                            {
                                var id = uriArray[^1] + _b;
                                if (!playlistString.Contains(id))
                                {
                                    playlistString += id;
                                    countPlaylist++;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            var localArtistIDs = GetSpotifyLocalTableIDs(DB_Term.Spotify_Artist);
            var localTrackIDs = GetSpotifyLocalTableIDs(DB_Term.Spotify_Track);
            var localAlbumIDs = GetSpotifyLocalTableIDs(DB_Term.Spotify_Album);

            string report = $"JSon/Sql IDs {DateTime.Now.Date}:{_b}ArtistIDs: {countArtist}/{localArtistIDs.Count}{_b}TrackIDs: {countTrack}/{localTrackIDs.Count}{_b}AlbumIDs: {countAlbum}/{localAlbumIDs.Count}{_b}";       

            var newArtistIDs = artistString.Split(_b).ToList().Except(localArtistIDs).ToList();
            var newTrackIDs = trackString.Split(_b).ToList().Except(localTrackIDs).ToList();
            var newAlbumIDs = albumString.Split(_b).ToList().Except(localAlbumIDs).ToList();

            Console.WriteLine("Unique to server");
            Console.WriteLine("Arists:");
            foreach (var line in newArtistIDs)
            {
                Console.WriteLine(line);
            }
            Console.WriteLine("Track:");
            foreach (var line in newTrackIDs)
            {
                Console.WriteLine(line);
            }
            Console.WriteLine("Album:");
            foreach (var line in newAlbumIDs)
            {
                Console.WriteLine(line);
            }

            Console.WriteLine("Following playlists found");
            Console.WriteLine(playlistString);

            string newToSqlArtistsPath = Path.Combine(outputDirectoryUri, "NewToSqlArtistIDs.csv");
            string newToSqlTracksPath = Path.Combine(outputDirectoryUri, "NewToSqlTrackIDs.csv");
            string newToSqlAlbumsPath = Path.Combine(outputDirectoryUri, "NewToSqlAlbumIDs.csv");
            
            string localArtistsPath = Path.Combine(outputDirectoryUri, "SqlArtistIDs.csv");
            string localTracksPath = Path.Combine(outputDirectoryUri, "SqlTrackIDs.csv");
            string localAlbumsPath = Path.Combine(outputDirectoryUri, "SqlAlbumIDs.csv");
            
            Console.WriteLine(report);

            File.WriteAllLines(newToSqlArtistsPath, newArtistIDs);
            File.WriteAllLines(newToSqlTracksPath, newTrackIDs);
            File.WriteAllLines(newToSqlAlbumsPath, newAlbumIDs);

            File.WriteAllLines(localArtistsPath, localArtistIDs);
            File.WriteAllLines(localTracksPath, localTrackIDs);
            File.WriteAllLines(localAlbumsPath, localAlbumIDs);
        }

        public static void FullSpotifySequence(string playlistID, string title)
        {
            var paths = SpotifyBounceRequest(playlistID, title);
            InsertAlbumTrackArtistFromDirectory(paths.albumfolder, paths.trackfolder, paths.artistfolder);
        }
        public static void InsertAlbumTrackArtistFromDirectory(string albumfolder, string trackfolder, string artistfolder)
        {
            DirectoryInfo directory = new DirectoryInfo(albumfolder);
            directory = directory.Parent;
            var insertDir = directory.CreateSubdirectory("InsertCommands");
            string insertDirectory = insertDir.FullName;

            AlbumSql(albumfolder, Path.Combine(insertDirectory, "Insert-Albums.json"));
            TrackSql(trackfolder, Path.Combine(insertDirectory, "Insert-Tracks.json"));
            ArtistSql(artistfolder, Path.Combine(insertDirectory, "Insert-Artists.json"));
        }
        public static (string albumfolder, string trackfolder, string artistfolder) SpotifyBounceRequest(string playlistID, string title)
        {
            PingSpotify();
            PingSpotify(@$"https://api.spotify.com/v1/playlists/{playlistID}");
            string rootDirectory = CreateUniqueDirectory(@$"C:\Users\axels\source\repos\testingAPIMouritsLyrics\testingAPIMouritsLyrics\Database.Spotify_music\PlaylistSeeding\{title}");
            string inputDirectory = CreateUniqueDirectory(rootDirectory + $@"\1. Input");

            var requestPaths = new List<string>();
            SpotifyListRequest($"playlist.{title}", inputDirectory, new[] { playlistID }, SpotifyType.playlists, out requestPaths);
            string playlistRequestPath = requestPaths[0];

            var albumIDs = GetSpotifyLocalTableIDs(DB_Term.Spotify_Album);
            File.WriteAllLines(GetUniqueFilePath(inputDirectory + @"\Current_AlbumIDs.csv"), albumIDs.ToArray());

            var trackIDs = GetSpotifyLocalTableIDs(DB_Term.Spotify_Track);
            File.WriteAllLines(GetUniqueFilePath(inputDirectory + @"\Current_TrackIDs.csv"), trackIDs.ToArray());

            var artistIDs = GetSpotifyLocalTableIDs(DB_Term.Spotify_Artist);
            File.WriteAllLines(GetUniqueFilePath(inputDirectory + @"\Current_ArtistIDs.csv"), artistIDs.ToArray());

            string albumRequestDirectory = CreateUniqueDirectory(rootDirectory + $@"\2. Album Requests");
            var newAlbumIDs = GetSpotifyTypeIDsFromJSonFile(playlistRequestPath, SpotifyType.albums);
            newAlbumIDs = newAlbumIDs.Except(albumIDs).ToList();

            PingSpotify();
            SpotifyListRequest(@$"{title}.AlbumRequest", albumRequestDirectory, newAlbumIDs.ToArray(), SpotifyType.albums, out requestPaths);
            
            var newTrackIDs = GetSpotifyTypeIDsFromJSonDirectory(albumRequestDirectory, inputDirectory + @"\Album-TrackIDs.csv", SpotifyType.tracks);
            newTrackIDs = newTrackIDs.Except(trackIDs).ToList();

            PingSpotify();
            string trackRequestDirectory = CreateUniqueDirectory(rootDirectory + $@"\3. Track Requests");
            SpotifyListRequest($"{title}.TrackRequest", trackRequestDirectory, newTrackIDs.ToArray(), SpotifyType.tracks, out requestPaths);

            var newArtistIDs = GetSpotifyTypeIDsFromJSonDirectory(trackRequestDirectory, inputDirectory + @"\Track-ArtistIDs.csv", SpotifyType.artists);
            newArtistIDs = newArtistIDs.Except(artistIDs).ToList();

            PingSpotify();
            string artistRequestDirectory = CreateUniqueDirectory(rootDirectory + $@"\4. Artist Requests");
            SpotifyListRequest(title, artistRequestDirectory, newArtistIDs.ToArray(), SpotifyType.artists, out requestPaths);

            return (albumfolder: albumRequestDirectory, trackfolder: trackRequestDirectory, artistfolder: artistRequestDirectory);
        }
        public static void AlbumSql(string rawDataFolder, string outPutFilePath)
        {
            CreateAndExecuteSqlInsertCommand(DB_Term.Spotify_Music, DB_Term.Spotify_Album, SpotifyType.albums, rawDataFolder, outPutFilePath);
        }
        public static void TrackSql(string rawDataFolder, string outPutFilePath)
        {
            CreateAndExecuteSqlInsertCommand(DB_Term.Spotify_Music, DB_Term.Spotify_Track, SpotifyType.tracks, rawDataFolder, outPutFilePath);
        }
        public static void ArtistSql(string rawDataFolder, string outPutFilePath)
        {
            CreateAndExecuteSqlInsertCommand(DB_Term.Spotify_Music, DB_Term.Spotify_Artist, SpotifyType.artists, rawDataFolder, outPutFilePath);
        }
        public static List<object> GetColumnValuesFromQueryFile(string filePath, string database)
        {
            var objects = new List<object>();
            using SqlConnection sqlConnection = new SqlConnection($@"Data Source=(local)\SQLExpress;Initial Catalog={database};Integrated Security=SSPI");
            {
                string stringCommand = File.ReadAllText(filePath);
                sqlConnection.Open();
                SqlCommand command = new SqlCommand(stringCommand, sqlConnection);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    objects.Add(reader[0]);
                }
            }
            return objects;
        }
        public static List<int> GetIDs(string database, string table)
        {
            var IDs = new List<int>();
            using SqlConnection sqlConnection = new SqlConnection($@"Data Source=(local)\SQLExpress;Initial Catalog={database};Integrated Security=SSPI");
            {
                string stringCommand = @$"SELECT ID FROM {table}";
                sqlConnection.Open();
                SqlCommand command = new SqlCommand(stringCommand, sqlConnection);
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    IDs.Add((int)reader[0]);
                }
            }
            return IDs;
        }
        public static void WriteDbIDs()
        {
            int Counter = 0;
            foreach (var id in GetSpotifyLocalTableIDs(DB_Term.Spotify_Track))
            {
                Console.WriteLine($"{Counter}. {id}");
            }
            Console.WriteLine();
            Counter = 0;
            foreach (var id in GetSpotifyLocalTableIDs(DB_Term.Spotify_Artist))
            {
                Console.WriteLine($"{Counter}. {id}");
            }
            Counter = 0;
            foreach (var id in GetSpotifyLocalTableIDs(DB_Term.Spotify_Album))
            {
                Console.WriteLine($"{Counter}. {id}");
            }
        }
        public static void AddStoreShifts()
        {
            string shift = String.Empty;
            var shifts = new List<string>();
            for (int StoreID = 1; StoreID <= 3; StoreID++)
            {
                for (int day = 0; day <= 4; day++)
                {
                    for (int i = 0; i < 2; i++)
                    {
                    if (i == 0)
                    {
                        shift = "CAST(N'07:00:00' AS Time), CAST(N'16:00:00' AS Time)";
                    }
                    else
                    {
                        shift = "CAST(N'10:00:00' AS Time), CAST(N'19:00:00' AS Time)";
                    }
                        shifts.Add($"INSERT[dbo].[Shift]([StoreID], [Day], [StartTime], [EndTime]) VALUES({StoreID}, {day}, {shift})");
                    }
                }
            }
            ExecuteSqlLines(shifts.ToArray(), DB_Term.ICAStore);
        }
        public static void AddEmployeeShift()
        {
            var lateShift = GetColumnValuesFromQueryFile(@"C:\Users\axels\source\repos\testingAPIMouritsLyrics\testingAPIMouritsLyrics\STORE_DB\TESTscript.csv", "ICAStore");
            var commandList = new List<string>();
            foreach (var shift in lateShift)
            {
                commandList.Add($"INSERT ShiftEmployee([ShiftID], [EmployeeID]) values({shift},3);");
                commandList.Add($"INSERT ShiftEmployee([ShiftID], [EmployeeID]) values({shift},4);");
            }
            ExecuteSqlLines(commandList.ToArray(), DB_Term.ICAStore);
        }
        private static string SpotifyAuth = $"{File.ReadAllText(@"C:\Users\axels\source\repos\testingAPIMouritsLyrics\testingAPIMouritsLyrics\Database.Spotify_music\hidden.csv")}";
        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            
            FullSpotifySequence("37i9dQZF1DX186v583rmzp", "I love my 90s Hip-Hop");
            //var tuplePaths = SpotifyBounceRequest(@"5fATvNzsBQIzch7C6n22ss", "Svenska hiphoplåtar");

        }

        
        public static void ManipulateTestJson(string filePath)
        {
            var jObject = JObject.Parse(File.ReadAllText(filePath));
            JArray dataProp = (JArray)jObject["DataProperties"];
            dataProp[0] = ((string)dataProp[0]) + 's';

            JArray dataBodies = (JArray)jObject["DataBodies"];
            foreach (JArray Song in dataBodies)
            {
                for (int i = 0; i < Song.Count; i++)
                {
                    Song[0].AddBeforeSelf("hello");
                    if (Song[i].Type == JTokenType.String)
                    {
                        Song[i] = ((string)Song[i]).ToUpper();
                    }
                    if (Song[i].Type == JTokenType.Integer)
                    {
                        Song[i] = (int)Song[i] * 10;
                    }
                    if (Song[i].ToString() == "EMINEM" && Song[1].Type == JTokenType.Boolean)
                    {
                        Song[1] = !((bool)Song[1]);
                    }
                }
            }

            DataConversion.JsonSerialize(jObject, filePath);
        }


        public static string SpotifyRequestLog(IRestResponse response, string title, out string log, bool AppendToTextFile = true, string filePath = @"C:\Users\axels\source\repos\testingAPIMouritsLyrics\testingAPIMouritsLyrics\Database.Spotify_music\Spotify.Request.LOG.txt")
        {
            var _b = Environment.NewLine;
            log = title + _b +
               DateTime.Now.ToString() + _b +
               $"Request: {response.Request.Method}" + _b +
               $"Is Response Successfull: {response.IsSuccessful}" + _b +
               $"RestSharp Response Code: {response.StatusCode}" + _b;

            string errorCode = String.Empty;

            if (!response.IsSuccessful)
            {
                JObject errorObj = JObject.Parse(response.Content);
                errorCode = errorObj["error"]["status"].ToString();
                log += $"Status code: {errorCode}" + _b +
                $"Error message: {errorObj["error"]["message"]}" + _b +
                $"content type: {response.ContentType}" + _b +
                 $"server: {response.Server}" + _b;

                Console.WriteLine(response.Content);
            }
            log += $"Response Uri: {response.ResponseUri.AbsoluteUri}" + _b + _b;

            if(AppendToTextFile) File.AppendAllText(filePath, log);

            if (response.IsSuccessful)
            {
                return null;
            }
            else
            {
                return errorCode;
            }
        }
        public class Person
        {
            public string Name { get; set; }
            public string LastName { get; set; }
            private static List<string> Names = new List<string> { "anne", "anne", "carl", "Lisa", "carl", "Bengt", "Ivar", "Bertil", "Lisa" };
            private static List<string> LastNames = new List<string> { "Lise", "Lise", "Adamsson", "Aronsson", "Bentsson", "Bengtsson", "Al", "Ek", "Kuk" };
            public Person(string name, string lastName)
            {
                Name = name;
                LastName = lastName;
            }
            public static List<Person> CreatePeople()
            {
                var people = new List<Person>();
                for (int i = 0; i < Names.Count; i++)
                {
                    people.Add(new Person(Names[i], LastNames[i]));
                }
                return people;
            }
            public static void QueryTests()
            {
                var people = CreatePeople();
                var people2 = CreatePeople();
                var people3 = CreatePeople();
                var list = new List<List<Person>> { people, people2, people3 };
                var query = from crowd in list
                            from peps in crowd
                            orderby peps.LastName
                            where peps.LastName.Length < 5 & peps.LastName.Length >2
                            select peps;
                            //group peps by $"{peps.Name} {peps.LastName}" into dist
                            //select new Person(dist.Key.Split(' ')[0], dist.Key.Split(' ')[1]);

                //query.ToList().ForEach(x => Console.WriteLine(x));
                query.ToList().ForEach(x => Console.WriteLine(x.Name + " " + x.LastName));


                //people = people.Distinct().ToList();
                //var people2 = people.GroupBy(x => x.Name + " " + x.LastName).Select(x => x.First()).OrderBy(x => x.LastName).ToList();
                //var people3 = people2.Take(3).ToList();
                //people2 = people2.Except(people3).ToList();

                //people.ForEach(x => Console.WriteLine(x.Name + " " + x.LastName));
                //Console.WriteLine();
                //people2.ForEach(x => Console.WriteLine(x.Name + " " + x.LastName));
                //Console.WriteLine();
                //people3.ForEach(x => Console.WriteLine(x.Name + " " + x.LastName));
                //Console.WriteLine();
                //var test = typeof(Person).GetProperties();
                //foreach (var p in test)
                //{
                //    Console.WriteLine(p.Name);
                //}
                //Console.ReadLine();
            }
        }
        public enum SpotifyType
        {
            artists,
            albums,
            tracks,
            playlists
        }
    }
    [TestClass]
    public static class Short
    {

        [TestInitialize]
        public static void TestInit()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        [TestMethod]
        public static void ClearTemp()
        {

        }
    }

}