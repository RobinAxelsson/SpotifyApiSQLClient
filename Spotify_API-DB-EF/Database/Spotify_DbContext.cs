using Microsoft.EntityFrameworkCore;
using Spotify_with_EF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_with_EF.Database
{
    public class Spotify_DbContext : DbContext
    {
        public string ConnectionString = @"Server=DESKTOP-4AJB8DD\SQLEXPRESS;Database=SpotifyDb_0.1;Trusted_Connection=True;";
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<Album> Albums { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
        }

        //build
        //add-migration x
        //update-database
    }
}
