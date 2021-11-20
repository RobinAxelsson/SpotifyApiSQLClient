using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spotify_with_EF.Database;
using Spotify_with_EF.Models;
using System;
using System.IO;

namespace Spotify_with_EF
{
    class Program
    {
        public static string ConnectionString = @"Server=DESKTOP-4AJB8DD\SQLEXPRESS;Database=SpotifyDb_0.1;Trusted_Connection=True;";
        static void Main(string[] args)
        {
            var tracksString = File.ReadAllText(@"local-json/tracks.json");
            var tracks = JsonConvert.DeserializeObject<Track[]>(tracksString);

            var dbHandler = new Spotify_DbContext { ConnectionString = ConnectionString };

            foreach (var track in tracks)
            {
                dbHandler.Tracks.Add(track);
            }
            dbHandler.SaveChanges();
        }
    }
}
