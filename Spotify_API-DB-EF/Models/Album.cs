using Newtonsoft.Json;
using System.Collections.Generic;

namespace Spotify_with_EF.Models
{
    public class Album
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("release_date")]
        public string Release_Date { get; set; }

        [JsonProperty("release_date")]
        public int? Popularity { get; set; }

        //public string Image_Url { get; set; }
        //public List<string> ArtistID { get; set; }
        //public List<string> TrackID { get; set; }
    }
}
