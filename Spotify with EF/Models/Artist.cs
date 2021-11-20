using Newtonsoft.Json;
using System.Collections.Generic;

namespace Spotify_with_EF.Models
{
    public class Artist
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        //["followers"]["total"]
        //public int? Followers { get; set; }

        [JsonProperty("popularity")]
        public int? Popularity { get; set; }
        //["images"][0]["url"]
        //public string Image_Url { get; set; }
        //public List<string> AlbumID { get; set; }
        //public List<string> TrackID { get; set; }
    }
}
