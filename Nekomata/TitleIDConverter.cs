using System;
using System.Collections.Generic;
using System.IO;
using RestSharp;
using Newtonsoft.Json;
using System.Data.SQLite;

namespace Nekomata
{
    enum Service
    {
        Kitsu,
        AniList
    }

    class TitleIDConverter
    {
        RestClient rclient;
        SQLiteConnection sqlitecon;

        public TitleIDConverter()
        {
            rclient = new RestClient();
            this.initalizeDatabase();
        }
        public int GetMALIDFromKitsuID(int kitsuid, EntryType type)
        {
            int titleid = this.RetreiveSavedMALIDFromServiceID(Service.Kitsu, kitsuid, type);
            if (titleid > -1)
            {
                return titleid;
            }
            String typestr = "";
            switch (type)
            {
                case EntryType.Anime:
                    typestr = "anime";
                    break;
                case EntryType.Manga:
                    typestr = "manga";
                    break;
                default:
                    break;
            }

            String filterstr = "myanimelist/" + typestr;
   
            RestRequest request = new RestRequest("https://kitsu.io/api/edge/" + typestr + "/" + kitsuid.ToString() + "?include=mappings&fields[anime]=id", Method.GET);
    
            IRestResponse response = rclient.Execute(request);
            if (response.StatusCode.GetHashCode() == 200)
            {
                Dictionary<string, object> jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                List<Dictionary<string, object>> included = (List<Dictionary<string, object>>)jsonData["included"];
                foreach (Dictionary<string,object> map in included)
                {
                    Dictionary<string, object> attr = (Dictionary<string, object>)map["attributes"];
                    if (String.Equals(((String)attr["externalSite"]),typestr,StringComparison.OrdinalIgnoreCase))
                    {
                        int malid = (int)attr["externalId"];
                        this.SaveIDtoDatabase(Service.Kitsu, malid, kitsuid, type);
                        return malid;
                    }
                }
                return -1;
            }
            else
            {
                return -1;
            }

        }

        public int GetMALIDFromAniListID(int anilistid, EntryType type)
        {
            int titleid = this.RetreiveSavedMALIDFromServiceID(Service.AniList, anilistid, type);
            if (titleid > -1)
            {
                return titleid;
            }
            String typestr = "";
            switch (type)
            {
                case EntryType.Anime:
                    typestr = "ANIME";
                    break;
                case EntryType.Manga:
                    typestr = "MANGA";
                    break;
                default:
                    break;
            }

            RestRequest request = new RestRequest("https://graphql.anilist.co", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody("{ \"query\" : \"query ($id: Int!, $type: MediaType) {\n  Media(id: $id, type: $type) {\n    id\n    idMal\n  }\n}\", \"variables\" : { \"id\" :" + anilistid.ToString() + ", \"type\" : " + type + "} }");

            IRestResponse response = rclient.Execute(request);
            if (response.StatusCode.GetHashCode() == 200)
            {
                Dictionary<string, object> jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                Dictionary<string, object> data = (Dictionary<string, object>)jsonData["data"];
                Dictionary<string, object> media = (Dictionary<string, object>)jsonData["Media"];
                int malid = (int)media["idMAL"];
                this.SaveIDtoDatabase(Service.AniList, malid, anilistid, type);
                return malid;
            }
            else
            {
                return -1;
            }

        }

        private int RetreiveSavedMALIDFromServiceID(Service listService, int titleid, EntryType type)
        {
            String sql = "";
            int mediatype = type == EntryType.Anime ? 0 : 1;
            switch (listService)
            {
                case Service.AniList:
                    sql = "SELECT malid FROM tableids WHERE anilist_id = " + titleid.ToString() + " AND mediatype = " + mediatype.ToString();
                    break;
                case Service.Kitsu:
                    sql = "SELECT malid FROM tableids WHERE kitsu_id = " + titleid.ToString() + " AND mediatype = " + mediatype.ToString();
                    break;
                default:
                    break;
            }
            SQLiteCommand cmd = new SQLiteCommand(sql, sqlitecon);
            SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                return (int)reader[@"malid"];
            }

            return -1;
        }

        private void SaveIDtoDatabase(Service listservice, int malid, int servicetitleid, EntryType type)
        {
           if (this.CheckIfEntryExists(malid,type))
           {
               // Update entry
               String sql = "";
               int mediatype = type == EntryType.Anime ? 0 : 1;
               switch (listservice)
               {
                   case Service.AniList:
                        sql = "INSERT INTO tableids (malid,anilist_id,mediatype) VALUES (" + malid.ToString() + "," + servicetitleid.ToString() + "," + type.ToString() + ")";
                        break;
                   case Service.Kitsu:
                        sql = "INSERT INTO tableids (malid,kitsu_id,mediatype) VALUES (" + malid.ToString() + "," + servicetitleid.ToString() + "," + type.ToString() + ")";
                        break;
                   default:
                        return;
               }
               SQLiteCommand cmd = new SQLiteCommand(sql, sqlitecon);
               cmd.ExecuteNonQuery();
           }
           else
            {
                // Insert entry
                this.InsertIDtoDatabase(listservice, malid, servicetitleid, type);
            }

        }

        private void InsertIDtoDatabase(Service listservice, int malid, int servicetitleid, EntryType type)
        {
            String sql = "";
            int mediatype = type == EntryType.Anime ? 0 : 1;
            switch (listservice)
            {
                case Service.AniList:
                    sql = "UPDATE titleids SET anilist_id = " + servicetitleid.ToString() + " WHERE mediatype = " + mediatype.ToString() + " AND malid = " + malid.ToString();
                    break;
                case Service.Kitsu:
                    sql = "UPDATE titleids SET kitsu_id = " + servicetitleid.ToString() + " WHERE mediatype = " + mediatype.ToString() + " AND malid = " + malid.ToString();
                    break;
                default:
                    return;
            }
            SQLiteCommand cmd = new SQLiteCommand(sql, sqlitecon);
            cmd.ExecuteNonQuery();

        }

        private bool CheckIfEntryExists(int malid, EntryType type)
        {
            int mediatype = type == EntryType.Anime ? 0 : 1;
            String sql = "SELECT malid FROM tableids WHERE malid = " + malid.ToString() + " AND mediatype = " + mediatype.ToString();
         
            SQLiteCommand cmd = new SQLiteCommand(sql, sqlitecon);
            SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                return true;
            }

            return false;
        }

        private void initalizeDatabase()
        {
            if (File.Exists("Nekomata.sqlite"))
            {
                sqlitecon = new SQLiteConnection("Data Source=Nekomata.sqlite;Version=3;");
                sqlitecon.Open();
            }
            else
            {
                // Create database and tables
                SQLiteConnection.CreateFile("Nekomata.sqlite");
                sqlitecon = new SQLiteConnection("Data Source=Nekomata.sqlite;Version=3;");
                sqlitecon.Open();
                SQLiteCommand createtable = new SQLiteCommand("CREATE TABLE titleids (anidb_id INT, anilist_id INT, kitsu_id INT, malid INT, animeplanet_id VARCHAR(50), mediatype INT");
                createtable.ExecuteNonQuery();
            }
        }
    }
}
