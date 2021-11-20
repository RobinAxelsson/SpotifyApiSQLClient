# Spotify API Client with free text SQL

> "It is a crazy Spotify Client/JSon-SQL ORM/SQL-Server Client Console App in C# that have no purpose what so ever //Robin

This project is a free time project that I did during my Database Course (during my vocational studies) during December 2020, when we were learning about SQL-Server, SQL-Server management studio and T-SQL language. It is a freestyle project just for fun.

## Features

- The code consumes [Spotify Web API](https://developer.spotify.com/documentation/web-api/)

- Input is a Spotify playlist JSon-object (as a file)

- The code strips off the id:s of artists and albums from the input file and fetches the data from each id and saves all in JSon files (you get a lot).

- The Rest Client used is RestSharp and it is used in a recursive manner to never request more than 50 objects at the time.

- It makes recursive requests to the API (50 at the time) if you input a JSon file with Spotify JSon data (it strips of the id:s in the new requests)

- The other part of the program maps the JSon objects to Relational objects to insert the artist, albums and songs to a SQL server.

## Note

It started out with another API from RapidAPI called [MouritsLyrics](https://rapidapi.com/PlanetTeamSpeak/api/mourits-lyrics) and there is a beginning of a newer version with Entity Framework which I never used before at the time.

https://robinaxelsson.github.io/