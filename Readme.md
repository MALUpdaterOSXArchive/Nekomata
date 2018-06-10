# Nekomata
Nekomata is a crossplatform .NET program that allows users to export their AniList and Kitsu lists to MyAnimeList XML format to import to any list service or use as a local backup.

Nekomata is written in C# and will work on Windows and eventually Linux. No macOS version is planned since Shukofukurou have these functions built in.

Nekomata (猫又) means a forked tail cat in Japanese folklore.

# How it works
First, you enter your username, select the service to convert the list from, select the type of list and click export. Nekomata will look up MyAnimeList Title IDs that corresponds to the title on AniList or Kitsu.

Note that not all titles are on MyAnimeList and Nekomata will show a list of titles that it couldn't find a MyAnimeList title id on before giving you the opertunity to save the converted XML list.

# Limitations
Nekomata only exports public entries for now since the application obtains the list without an OAuth2 token. Since MyAnimeList doesn't have an option for private entries, I decided to leave out all of these entries.

Also, advanced scoring is not supported. Scores are converted to a 1-10 scale with 0 indicating no rating.

Option for a universal format will come at a later date (with the ability to import these lists) along with Anime-Planet support.

# Donate
Nekomata is open source freeware. If you find this piece of software useful and want to see further development, feel free to leave a donation. Every bit helps!

[Donate](https://malupdaterosx.moe/donate/)

# License
Nekomata is licensed under the MIT License.