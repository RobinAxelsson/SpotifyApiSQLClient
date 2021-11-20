using Newtonsoft.Json;
using System.Collections.Generic;

namespace Spotify_with_EF.Models
{
    public class Track
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("duration_ms")]
        public int? Duration_ms { get; set; }

        [JsonProperty("popularity")]
        public int? Popularity { get; set; }

        [JsonProperty("explicit")]
        public bool Explicit_lyrics { get; set; }
        //public List<string> ArtistID { get; set; }
        //public List<string> AlbumID { get; set; }
    }
}
