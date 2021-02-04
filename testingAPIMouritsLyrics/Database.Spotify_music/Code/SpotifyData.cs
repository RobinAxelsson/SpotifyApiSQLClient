using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace musicAPI
{
    //These classes are an experiment with creating a big JSON-object-database from the incoming Spotify JSON-files.
    public class Track
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public int? Duration_ms { get; set; }
        public int? Popularity { get; set; }
        public bool Explicit_lyrics { get; set; }
        public List<string> ArtistID { get; set; }
        public List<string> AlbumID { get; set; }
        public Track() { }
        public Track(JObject track)
        {
            ID = (string)track["id"];
            Name = (string)track["name"];
            Duration_ms = (int?)track["duration_ms"];
            Popularity = (int?)track["popularity"];
            Explicit_lyrics = (bool)track["explicit"];
            ArtistID = new List<string>();
            AlbumID = new List<string>();
        }
    }
    public class Album
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public string Release_Date { get; set; }
        public int? Popularity { get; set; }
        public string Image_Url { get; set; }
        public List<string> ArtistID { get; set; }
        public List<string> TrackID { get; set; }
        public Album() { }
        public Album(JObject album)
        {
            ID = (string)album["id"];
            Name = (string)album["name"];
            Label = (string)album["label"];
            Release_Date = album["release_date"].ToString();
            Popularity = (int?)album["popularity"];
            Image_Url = (string)album["images"][0]["url"];
            ArtistID = new List<string>();
            TrackID = new List<string>();
        }
    }
    public class Artist
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public int? Followers { get; set; }
        public int? Popularity { get; set; }
        public string Image_Url { get; set; }
        public List<string> AlbumID { get; set; }
        public List<string> TrackID { get; set; }
        public Artist() { }
        public Artist(JObject artist)
        {
            ID = (string)artist["id"];
            Name = (string)artist["name"];
            Followers = (int?)(artist["followers"]["total"]);
            Popularity = (int?)(artist["popularity"]);
            try
            {
                Image_Url = (string)(artist["images"][0]["url"]);
            }
            catch (Exception)
            {}
            TrackID = new List<string>();
            AlbumID = new List<string>();
        }
    }
    public class SpotifyData
    {
        public List<Track> tracks { get; set; } = new List<Track>();
        public List<Artist> artists { get; set; } = new List<Artist>();
        public List<Album> albums { get; set; } = new List<Album>();
        public void AddRawTrack(JObject track)
        {
            var newTrack = new Track(track);
            if (newTrack.ID != null && newTrack.Name != null)
            {
                tracks.Add(newTrack);
            }
            else
            {
                return;
            }
            var jReader = track.CreateReader();
            while (jReader.Read())
            {
                if (jReader != null && jReader.Path.Split('.')[^1] == "uri")
                {
                    string uri = track.Root.SelectToken(jReader.Path).ToString();
                    var uriArray = uri.Split(':');
                    if (uriArray[1] == "album" && !newTrack.AlbumID.Contains(uriArray[2]))
                    {
                        newTrack.AlbumID.Add(uriArray[2]);                        
                    }
                    if (uriArray[1] == "artist" && !newTrack.ArtistID.Contains(uriArray[2]))
                    {
                        newTrack.ArtistID.Add(uriArray[2]);
                    }
                }
            }
        }
        public void AddRawAlbum(JObject album)
        {
            var newAlbum = new Album(album);
            if (newAlbum.ID != null && newAlbum.Name != null)
            {
                albums.Add(newAlbum);
            }
            else
            {
                return;
            }
            
            var jReader = album.CreateReader();
            while (jReader.Read())
            {
                if (jReader != null && jReader.Path.Split('.')[^1] == "uri")
                {
                    string uri = album.Root.SelectToken(jReader.Path).ToString();
                    var uriArray = uri.Split(':');
                    if (uriArray[1] == "track" && !newAlbum.TrackID.Contains(uriArray[2]))
                    {
                        newAlbum.TrackID.Add(uriArray[2]);
                    }
                    if (uriArray[1] == "artist" && !newAlbum.ArtistID.Contains(uriArray[2]))
                    {
                        newAlbum.ArtistID.Add(uriArray[2]);
                    }
                }
            }
        }
        public void AddRawArtist(JObject artist)
        {
            var newArtist = new Artist(artist);
            if (newArtist.ID != null && newArtist.Name != null)
            {
                artists.Add(newArtist);
            }
            else
            {
                return;
            }
            var jReader = artist.CreateReader();
            while (jReader.Read())
            {
                if (jReader != null && jReader.Path.Split('.')[^1] == "uri")
                {
                    string uri = artist.Root.SelectToken(jReader.Path).ToString();
                    var uriArray = uri.Split(':');
                    if (uriArray[1] == "track" && !newArtist.TrackID.Contains(uriArray[2]))
                    {
                        newArtist.TrackID.Add(uriArray[2]);
                    }
                    if (uriArray[1] == "album" && !newArtist.AlbumID.Contains(uriArray[2]))
                    {
                        newArtist.AlbumID.Add(uriArray[2]);
                    }
                }
            }
        }
        public void MergeOrNew(string filePath)
        {

            if (!File.Exists(filePath))
            {
                this.artists = this.artists.OrderBy(x => x.Name).ToList();
                this.tracks = this.tracks.OrderBy(x => x.Name).ToList();
                this.albums = this.albums.OrderBy(x => x.Name).ToList();
                //this.tracks = this.tracks.GroupBy(x => x.Name).Select(x => x.Last()).OrderBy(x => x.Name).ToList();
                //this.albums = this.albums.GroupBy(x => x.Name).Select(x => x.Last()).OrderBy(x => x.Name).ToList();
                DataConversion.JsonSerialize(this, filePath);
                return;
            }
            try
            {
                SpotifyData oldSpotify = JsonConvert.DeserializeObject<SpotifyData>(File.ReadAllText(filePath));
                oldSpotify.albums.AddRange(albums);
                oldSpotify.artists.AddRange(artists);
                oldSpotify.tracks.AddRange(tracks);

                //oldSpotify.tracks = oldSpotify.tracks.GroupBy(x => x.ID + x.Popularity).Select(x => x.First()).OrderBy(x => x.Name).ToList();
                oldSpotify.tracks = oldSpotify.tracks.GroupBy(x => x.ID).Select(x => x.First()).OrderBy(x => x.Name).ToList();
                oldSpotify.artists = oldSpotify.artists.GroupBy(x => x.ID).Select(x => x.First()).OrderBy(x => x.Name).ToList();
                oldSpotify.albums = oldSpotify.albums.GroupBy(x => x.ID).Select(x => x.First()).OrderBy(x => x.Name).ToList();

                string json = JsonConvert.SerializeObject(oldSpotify, Formatting.Indented);
                File.WriteAllText(filePath, json);
                //DataConversion.JsonSerialize(oldSpotify, filePath);
            }
            catch (Exception)
            {
                DataConversion.JsonSerialize(this, filePath + "_CATCH_DATA.json");
            }
        }
        public void Append(Program.SpotifyType spotifyType, string filePath)
        {
            if (!File.Exists(filePath)) throw new Exception("File doesent exist");
            JObject jObject = JObject.Parse(File.ReadAllText(filePath));

            switch (spotifyType)
            {
                case Program.SpotifyType.artists:
                    JArray artists = (JArray)jObject["artists"];
                    foreach (JObject artist in artists)
                    {
                        this.AddRawArtist(artist);
                    }
                    break;
                case Program.SpotifyType.albums:
                    JArray albums = (JArray)jObject["albums"];
                    if (albums == null)
                    {
                        albums = (JArray)jObject["items"];
                    }
                    foreach (JObject album in albums)
                    {
                        this.AddRawAlbum(album);
                    }
                    break;
                case Program.SpotifyType.tracks:
                    JArray tracks = (JArray)jObject["tracks"];
                    foreach (JObject track in tracks)
                    {
                        this.AddRawTrack(track);
                    }
                    break;
                default:
                    break;
            }
        }
        public List<string> GetNullAlbums()
        {
            var query =
                from t in this.albums
                where t.Popularity == null || t.Label == null
                select t.ID;
            return query.ToList();
        }
        public List<string> GetNullTracks()
        {
            var query =
                from t in this.tracks
                where t.Popularity == null
                select t.ID;
            return query.ToList();
        }
        public static SpotifyData LoadFromFile(string spotifyDataFilePath)
        {
            if (File.Exists(spotifyDataFilePath))
            {

                SpotifyData spotify = JsonConvert.DeserializeObject<SpotifyData>(File.ReadAllText(spotifyDataFilePath));
                return spotify;
            }
            else
            {
                return null;
            }
        }
    }
}
