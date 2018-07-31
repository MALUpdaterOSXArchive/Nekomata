/* ListNormalizer.cs
 * This class retrieves lists and normalizes them.
 * 
 * Copyright (c) 2018 MAL Updater OS X Group, a division of Moy IT Solutions
 * Licensed under MIT License
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace Nekomata
{
    class ListNormalizer
    {
        private List<Dictionary<String, Object>> tmplist;
        private List<Dictionary<String, Object>> attributes;
        RestClient krestclient;
        RestClient arestclient;
        private int currentuserid;
        private EntryType currenttype;
        public bool erroredout;
        public TitleIDConverter tconverter;

        public ListNormalizer()
        {
            arestclient = new RestClient("https://graphql.anilist.co");
            krestclient = new RestClient("https://kitsu.io/api/edge");
            tmplist = new List<Dictionary<string, object>>();
            attributes = new List<Dictionary<string, object>>();
        }

        public List<ListEntry> RetrieveAniListList(EntryType type, String Username)
        {
            this.erroredout = false;
            this.currentuserid = this.GetAniListUserID(Username);
            if (this.currentuserid > 0)
            {
                this.currenttype = type;
                return this.PerformRetrieveAniListList(0);
            }
            else
            {
                this.erroredout = true;
                return new List<ListEntry>();
            }
        }

        private List<ListEntry> PerformRetrieveAniListList(int page)
        {
            RestRequest request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            GraphQLQuery gquery = new GraphQLQuery();
            switch (currenttype)
            {
                case EntryType.Anime:
                    gquery.query = "query ($id : Int!, $page: Int) {\n  AnimeList: Page (page: $page) {\n    mediaList(userId: $id, type: ANIME) {\n      id :media{id\n idMal}\n      entryid: id\n      title: media {title {\n        title: userPreferred\n      }}\n      episodes: media{episodes}\n      duration: media{duration}\n      image_url: media{coverImage {\n        large\n        medium\n      }}\n        type: media{format}\n      status: media{status}\n      score: score(format: POINT_100)\n      watched_episodes: progress\n      watched_status: status\n      rewatch_count: repeat\n      private\n      notes\n      watching_start: startedAt {\n        year\n        month\n        day\n      }\n      watching_end: completedAt {\n        year\n        month\n        day\n      }\n    }\n    pageInfo {\n      total\n      currentPage\n      lastPage\n      hasNextPage\n      perPage\n    }\n  }\n}";
                    break;
                case EntryType.Manga:
                    gquery.query = "query ($id : Int!, $page: Int) {\n  MangaList: Page (page: $page) {\n    mediaList(userId: $id, type: MANGA) {\n      id :media{id\n idMal}\n      entryid: id\n      title: media {title {\n        title: userPreferred\n      }}\n      chapters: media{chapters}\n      volumes: media{volumes}\n      image_url: media{coverImage {\n        large\n        medium\n      }}\n      type: media{format}\n      status: media{status}\n      score: score(format: POINT_100)\n      read_chapters: progress\n      read_volumes: progressVolumes\n      read_status: status\n      reread_count: repeat\n      private\n      notes\n      read_start: startedAt {\n        year\n        month\n        day\n      }\n      read_end: completedAt {\n        year\n        month\n        day\n      }\n    }\n        pageInfo {\n      total\n      currentPage\n      lastPage\n      hasNextPage\n      perPage\n    }\n  }\n}";
                    break;
                default:
                    return new List<ListEntry>();
            }
            gquery.variables = new Dictionary<string, object> { { "id", currentuserid.ToString() }, { "page", page.ToString() } };
            request.AddJsonBody(gquery);
            IRestResponse response = arestclient.Execute(request);
            Thread.Sleep(1000);
            if (response.StatusCode.GetHashCode() == 200)
            {
                Dictionary<string, object> jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                Dictionary<string, object> list;
                switch (currenttype)
                {
                    case EntryType.Anime:
                        list = JObjectToDictionary((JObject)JObjectToDictionary((JObject)jsonData["data"])["AnimeList"]);
                        break;
                    case EntryType.Manga:
                        list = JObjectToDictionary((JObject)JObjectToDictionary((JObject)jsonData["data"])["MangaList"]);
                        break;
                    default:
                        return new List<ListEntry>();
                }
                Dictionary<string, object> pageData = JObjectToDictionary((JObject)list["pageInfo"]);
                tmplist.AddRange(((JArray)list["mediaList"]).ToObject<List<Dictionary<string, object>>>());
                bool nextpage = (bool)pageData[@"hasNextPage"];
                if (nextpage)
                {
                    int newpage = page + 1;
                    return this.PerformRetrieveAniListList(newpage);
                }
                else
                {
                    // Convert List
                    switch (currenttype)
                    {
                        case EntryType.Anime:
                            return this.NormalizeAniListAnimeList();
                        case EntryType.Manga:
                            return this.NormalizeAniListMangaList();
                        default:
                            return new List<ListEntry>();
                    }
                }
            }
            else
            {
                this.erroredout = true;
                return new List<ListEntry>();
            }
        }

        private List<ListEntry> NormalizeAniListAnimeList()
        {
            // Create temp list
            List<ListEntry> tmplistentries = new List<ListEntry>();

            foreach (Dictionary<String, Object> entry in this.tmplist)
            {
                int titleId = Convert.ToInt32((long)JObjectToDictionary((JObject)entry["id"])["id"]);
                int idMal = !object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["id"])["idMal"]) ? Convert.ToInt32((long)JObjectToDictionary((JObject)entry["id"])["idMal"]) : -1;
                if (idMal > 0 && tconverter.RetreiveSavedMALIDFromServiceID(Service.AniList, titleId, EntryType.Anime) < 0)
                {
                    tconverter.SaveIDtoDatabase(Service.AniList, idMal, titleId, EntryType.Anime);
                }
                String title = (String)(JObjectToDictionary((JObject)(JObjectToDictionary((JObject)entry["title"]))["title"]))["title"];
                String tmpstatus = (String)entry["watched_status"];
                bool reconsuming = false;
                EntryStatus eStatus;
                switch (tmpstatus)
                {
                    case "PAUSED":
                        eStatus = EntryStatus.paused;
                        break;
                    case "PLANNING":
                        eStatus = EntryStatus.planning;
                        break;
                    case "CURRENT":
                        eStatus = EntryStatus.current;
                        break;
                    case "REPEATING":
                        reconsuming = true;
                        eStatus = EntryStatus.current;
                        break;
                    case "COMPLETED":
                        eStatus = EntryStatus.completed;
                        break;
                    case "DROPPED":
                        eStatus = EntryStatus.dropped;
                        break;
                    default:
                        eStatus = EntryStatus.current;
                        break;
                }
                int progress = Convert.ToInt32((long)entry["watched_episodes"]);
                ListEntry newentry = new ListEntry(titleId, title, eStatus, progress);
                newentry.totalSegment = (!object.ReferenceEquals(null, (JObjectToDictionary((JObject)entry["episodes"]))["episodes"])) ? Convert.ToInt32((long)(JObjectToDictionary((JObject)entry["episodes"]))["episodes"]) : 0;
                newentry.mediaFormat = (String)(JObjectToDictionary((JObject)entry["type"]))["format"];
                newentry.repeating = reconsuming;
                newentry.repeatCount = Convert.ToInt32((long)entry["rewatch_count"]);
                newentry.personalComments = (String)entry["notes"];
                newentry.rating = Convert.ToInt32((long)entry["score"]);
                if (!object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["watching_start"])["year"]) && !object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["watching_start"])["month"]) && !object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["watching_start"])["day"]))
                {
                    newentry.startDate = (long)JObjectToDictionary((JObject)entry["watching_start"])["year"] + "-" + ((long)JObjectToDictionary((JObject)entry["watching_start"])["month"] < 10 ? "0" + ((long)JObjectToDictionary((JObject)entry["watching_start"])["month"]).ToString() : ((long)JObjectToDictionary((JObject)entry["watching_start"])["month"]).ToString()) + "-" + ((long)JObjectToDictionary((JObject)entry["watching_start"])["day"] < 10 ? "0" + ((long)JObjectToDictionary((JObject)entry["watching_start"])["day"]).ToString() : ((long)JObjectToDictionary((JObject)entry["watching_start"])["day"]).ToString());
                }
                else
                {
                    newentry.startDate = "0000-00-00";
                }
                if (!object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["watching_end"])["year"]) && !object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["watching_end"])["month"]) && !object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["watching_end"])["day"]))
                {
                    newentry.endDate = (long)JObjectToDictionary((JObject)entry["watching_end"])["year"] + "-" + ((long)JObjectToDictionary((JObject)entry["watching_end"])["month"] < 10 ? "0" + ((long)JObjectToDictionary((JObject)entry["watching_end"])["month"]).ToString() : ((long)JObjectToDictionary((JObject)entry["watching_end"])["month"]).ToString()) + "-" + ((long)JObjectToDictionary((JObject)entry["watching_end"])["day"] < 10 ? "0" + ((long)JObjectToDictionary((JObject)entry["watching_end"])["day"]).ToString() : ((long)JObjectToDictionary((JObject)entry["watching_end"])["day"]).ToString());
                }
                else
                {
                    newentry.endDate = "0000-00-00";
                }
                // Add entry
                tmplistentries.Add(newentry);
            }
            return tmplistentries;
        }

        private List<ListEntry> NormalizeAniListMangaList()
        {
            // Create temp list
            List<ListEntry> tmplistentries = new List<ListEntry>();

            foreach (Dictionary<String, Object> entry in this.tmplist)
            {
                int titleId = Convert.ToInt32((long)JObjectToDictionary((JObject)entry["id"])["id"]);
                int idMal = !(object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["id"])["idMal"])) ? Convert.ToInt32((long)JObjectToDictionary((JObject)entry["id"])["idMal"]) : -1;
                if (idMal > 0 && tconverter.RetreiveSavedMALIDFromServiceID(Service.AniList, titleId, EntryType.Manga) < 0)
                {
                    tconverter.SaveIDtoDatabase(Service.AniList, idMal, titleId, EntryType.Manga);
                }
                String title = (String)(JObjectToDictionary((JObject)(JObjectToDictionary((JObject)entry["title"]))["title"]))["title"];
                String tmpstatus = (String)entry["read_status"];
                bool reconsuming = false;
                EntryStatus eStatus;
                switch (tmpstatus)
                {
                    case "PAUSED":
                        eStatus = EntryStatus.paused;
                        break;
                    case "PLANNING":
                        eStatus = EntryStatus.planning;
                        break;
                    case "CURRENT":
                        eStatus = EntryStatus.current;
                        break;
                    case "REPEATING":
                        reconsuming = true;
                        eStatus = EntryStatus.current;
                        break;
                    case "COMPLETED":
                        eStatus = EntryStatus.completed;
                        break;
                    case "DROPPED":
                        eStatus = EntryStatus.dropped;
                        break;
                    default:
                        eStatus = EntryStatus.current;
                        break;
                }
                int progress = Convert.ToInt32((long)entry["read_chapters"]);
                int progressVolumes = Convert.ToInt32((long)entry["read_volumes"]);
                ListEntry newentry = new ListEntry(titleId, title, eStatus, progress, progressVolumes);
                newentry.totalSegment = (!object.ReferenceEquals(null, (JObjectToDictionary((JObject)entry["chapters"]))["chapters"])) ? Convert.ToInt32((long)(JObjectToDictionary((JObject)entry["chapters"]))["chapters"]) : 0;
                newentry.totalVolumes = (!object.ReferenceEquals(null, (JObjectToDictionary((JObject)entry["volumes"]))["volumes"])) ? Convert.ToInt32((long)(JObjectToDictionary((JObject)entry["volumes"]))["volumes"]) : 0;
                newentry.mediaFormat = (String)(JObjectToDictionary((JObject)entry["type"]))["format"];
                newentry.repeating = reconsuming;
                newentry.repeatCount = Convert.ToInt32((long)entry["reread_count"]);
                newentry.personalComments = (String)entry["notes"];
                newentry.rating = Convert.ToInt32((long)entry["score"]);
                if (!object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["read_start"])["year"]) && !object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["read_start"])["month"]) && !object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["read_start"])["day"]))
                {
                    newentry.startDate = (long)JObjectToDictionary((JObject)entry["read_start"])["year"] + "-" + ((long)JObjectToDictionary((JObject)entry["read_start"])["month"] < 10 ? "0" + ((long)JObjectToDictionary((JObject)entry["read_start"])["month"]).ToString() : ((long)JObjectToDictionary((JObject)entry["read_start"])["month"]).ToString()) + "-" + ((long)JObjectToDictionary((JObject)entry["read_start"])["day"] < 10 ? "0" + ((long)JObjectToDictionary((JObject)entry["read_start"])["day"]).ToString() : ((long)JObjectToDictionary((JObject)entry["read_start"])["day"]).ToString());
                }
                else
                {
                    newentry.startDate = "0000-00-00";
                }
                if (!object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["read_end"])["year"]) && !object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["read_end"])["month"]) && !object.ReferenceEquals(null, JObjectToDictionary((JObject)entry["read_end"])["day"]))
                {
                    newentry.endDate = (long)JObjectToDictionary((JObject)entry["read_end"])["year"] + "-" + ((long)JObjectToDictionary((JObject)entry["read_end"])["month"] < 10 ? "0" + ((long)JObjectToDictionary((JObject)entry["read_end"])["month"]).ToString() : ((long)JObjectToDictionary((JObject)entry["read_end"])["month"]).ToString()) + "-" + ((long)JObjectToDictionary((JObject)entry["read_end"])["day"] < 10 ? "0" + ((long)JObjectToDictionary((JObject)entry["read_end"])["day"]).ToString() : ((long)JObjectToDictionary((JObject)entry["read_end"])["day"]).ToString());
                }
                else
                {
                    newentry.endDate = "0000-00-00";
                }
                // Add entry
                tmplistentries.Add(newentry);
            }
            return tmplistentries;
        }
        /*
         * Kitsu
         */

        public List<ListEntry> RetrieveKitsuList(EntryType type, String Username)
        {
            this.erroredout = false;
            this.currentuserid = this.GetKitsuUserID(Username);
            if (this.currentuserid > 0)
            {
                this.currenttype = type;
                return this.PerformRetrieveKitsuList(0);
            }
            else
            {
                this.erroredout = true;
                return new List<ListEntry>();
            }
        }
        private List<ListEntry> PerformRetrieveKitsuList(int page)
        {
            String listtype = "";
            String includes = "";
            switch (currenttype)
            {
                case EntryType.Anime:
                    listtype = "anime";
                    includes = "canonicalTitle,episodeCount,episodeLength,showType,posterImage,status,mappings";
                    break;
                case EntryType.Manga:
                    listtype = "manga";
                    includes = "canonicalTitle,chapterCount,volumeCount,mangaType,posterImage,status,mappings";
                    break;
                default:
                    return new List<ListEntry>();
            }
            RestRequest request = new RestRequest("/library-entries?filter[userId]=" + this.currentuserid + "&filter[kind]=" + listtype + "&include=" + listtype + "," + listtype + ".mappings" + "&fields[" + listtype + "]=" + includes + "&fields[mappings]=externalSite,externalId&page[limit]=500&page[offset]=" + page, Method.GET);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Accept", "application/vnd.api+json");

            IRestResponse response = krestclient.Execute(request);
            if (response.StatusCode.GetHashCode() == 200)
            {
                Dictionary<string, object> jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                List<Dictionary<string, object>> list;
                List<Dictionary<string, object>> metadata;
                list = ((JArray)jsonData["data"]).ToObject<List<Dictionary<string, object>>>();
                if (!object.ReferenceEquals(null, jsonData["data"]))
                {
                    metadata = ((JArray)jsonData["included"]).ToObject<List<Dictionary<string, object>>>();
                    attributes.AddRange(metadata);
                }
                tmplist.AddRange(list);
                Dictionary<string, object> links = ((JObject)jsonData["links"]).ToObject<Dictionary<string, object>>();
                bool nextpage = links.ContainsKey("next");
                if (nextpage)
                {
                    int newpage = page + 500;
                    return this.PerformRetrieveAniListList(newpage);
                }
                else
                {
                    // Convert List
                    switch (currenttype)
                    {
                        case EntryType.Anime:
                            return this.NormalizeKitsuAnimeList();
                        case EntryType.Manga:
                            return this.NormalizeKitsuMangaList();
                        default:
                            return new List<ListEntry>();
                    }
                }
            }
            else
            {
                this.erroredout = true;
                return new List<ListEntry>();
            }
        }

        private List<ListEntry> NormalizeKitsuAnimeList()
        {
            // Create temp list
            List<ListEntry> tmplistentries = new List<ListEntry>();

            foreach (Dictionary<String, Object> entry in this.tmplist)
            {
                int entryId = int.Parse((string)entry["id"]);
                int titleId = int.Parse((string)((JObject)((JObject)(((JObject)entry["relationships"]).ToObject<Dictionary<string, object>>()["anime"])).ToObject<Dictionary<string, object>>()["data"]).ToObject<Dictionary<string, object>>()["id"]);
                Dictionary<string, object> eattributes = ((JObject)entry["attributes"]).ToObject<Dictionary<string, object>>();
                Dictionary<string, object> attributes = JObjectToDictionary((JObject)FindMetaData(titleId, EntryType.Anime)["attributes"]);
                RecordMappingIds(titleId, EntryType.Anime);
                String title = (String)attributes["canonicalTitle"];
                String tmpstatus = (String)eattributes["status"];
                bool reconsuming = (bool)eattributes["reconsuming"];
                EntryStatus eStatus;
                switch (tmpstatus)
                {
                    case "on_hold":
                        eStatus = EntryStatus.paused;
                        break;
                    case "planned":
                        eStatus = EntryStatus.planning;
                        break;
                    case "current":
                        eStatus = EntryStatus.current;
                        break;
                    case "completed":
                        eStatus = EntryStatus.completed;
                        break;
                    case "dropped":
                        eStatus = EntryStatus.dropped;
                        break;
                    default:
                        eStatus = EntryStatus.current;
                        break;
                }
                int progress = Convert.ToInt32((long)eattributes["progress"]);
                ListEntry newentry = new ListEntry(titleId, title, eStatus, progress);
                newentry.totalSegment = !(object.ReferenceEquals(null, (attributes["episodeCount"]))) ? Convert.ToInt32((long)attributes["episodeCount"]) : 0;
                newentry.mediaFormat = (String)attributes["showType"];
                newentry.repeating = reconsuming;
                newentry.repeatCount = Convert.ToInt32((long)eattributes["reconsumeCount"]);
                newentry.personalComments = (String)eattributes["notes"];
                newentry.rating = !object.ReferenceEquals(null, eattributes["ratingTwenty"]) ? ConvertRatingTwentyToRawScore(Convert.ToInt32((long)eattributes["ratingTwenty"])) : 0;
                if (!object.ReferenceEquals(null, eattributes["startedAt"]))
                {
                    DateTime startDate = (DateTime)eattributes["startedAt"];
                    newentry.startDate = startDate.Year + "-" + (startDate.Month < 10 ? "0" + startDate.Month.ToString() : startDate.Month.ToString()) + "-" + (startDate.Day < 10 ? "0" + startDate.Day.ToString() : startDate.Day.ToString());
                }
                else
                {
                    newentry.startDate = "0000-00-00";
                }
                if (!object.ReferenceEquals(null, eattributes["finishedAt"]))
                {
                    DateTime finishDate = (DateTime)eattributes["finishedAt"];
                    newentry.endDate = finishDate.Year + "-" + (finishDate.Month < 10 ? "0" + finishDate.Month.ToString() : finishDate.Month.ToString()) + "-" + (finishDate.Day < 10 ? "0" + finishDate.Day.ToString() : finishDate.Day.ToString());
                }
                else
                {
                    newentry.endDate = "0000-00-00";
                }
                // Add entry
                tmplistentries.Add(newentry);
            }
            return tmplistentries;
        }

        private List<ListEntry> NormalizeKitsuMangaList()
        {
            // Create temp list
            List<ListEntry> tmplistentries = new List<ListEntry>();

            foreach (Dictionary<String, Object> entry in this.tmplist)
            {
                int entryId = int.Parse((string)entry["id"]);
                int titleId = int.Parse((string)((JObject)((JObject)(((JObject)entry["relationships"]).ToObject<Dictionary<string, object>>()["manga"])).ToObject<Dictionary<string, object>>()["data"]).ToObject<Dictionary<string, object>>()["id"]);
                Dictionary<string, object> eattributes = ((JObject)entry["attributes"]).ToObject<Dictionary<string, object>>();
                Dictionary<string, object> attributes = JObjectToDictionary((JObject)FindMetaData(titleId, EntryType.Manga)["attributes"]);
                RecordMappingIds(titleId, EntryType.Manga);
                String title = (String)attributes["canonicalTitle"];
                String tmpstatus = (String)eattributes["status"];
                bool reconsuming = (bool)eattributes["reconsuming"];
                EntryStatus eStatus;
                switch (tmpstatus)
                {
                    case "on_hold":
                        eStatus = EntryStatus.paused;
                        break;
                    case "planned":
                        eStatus = EntryStatus.planning;
                        break;
                    case "current":
                        eStatus = EntryStatus.current;
                        break;
                    case "completed":
                        eStatus = EntryStatus.completed;
                        break;
                    case "dropped":
                        eStatus = EntryStatus.dropped;
                        break;
                    default:
                        eStatus = EntryStatus.current;
                        break;
                }
                int progress = Convert.ToInt32((long)eattributes["progress"]);
                int progressVolumes = Convert.ToInt32((long)eattributes["volumesOwned"]);
                ListEntry newentry = new ListEntry(titleId, title, eStatus, progress, progressVolumes);
                newentry.totalSegment = !(object.ReferenceEquals(null, (attributes["chapterCount"]))) ? Convert.ToInt32((long)attributes["chapterCount"]) : 0;
                newentry.totalVolumes = !(object.ReferenceEquals(null, (attributes["volumeCount"]))) ? Convert.ToInt32((long)attributes["volumeCount"]) : 0;
                newentry.mediaFormat = (String)attributes["mangaType"];
                newentry.repeating = reconsuming;
                newentry.repeatCount = Convert.ToInt32((long)eattributes["reconsumeCount"]);
                newentry.personalComments = (String)eattributes["notes"];
                newentry.rating = !object.ReferenceEquals(null, eattributes["ratingTwenty"]) ? ConvertRatingTwentyToRawScore(Convert.ToInt32((long)eattributes["ratingTwenty"])) : 0;
                if (!object.ReferenceEquals(null, eattributes["startedAt"]))
                {
                    DateTime startDate = (DateTime)eattributes["startedAt"];
                    newentry.startDate = startDate.Year + "-" + (startDate.Month < 10 ? "0" + startDate.Month.ToString() : startDate.Month.ToString()) + "-" + (startDate.Day < 10 ? "0" + startDate.Day.ToString() : startDate.Day.ToString());
                }
                else
                {
                    newentry.startDate = "0000-00-00";
                }
                if (!object.ReferenceEquals(null, eattributes["finishedAt"]))
                {
                    DateTime finishDate = (DateTime)eattributes["finishedAt"];
                    newentry.endDate = finishDate.Year + "-" + (finishDate.Month < 10 ? "0" + finishDate.Month.ToString() : finishDate.Month.ToString()) + "-" + (finishDate.Day < 10 ? "0" + finishDate.Day.ToString() : finishDate.Day.ToString());
                }
                else
                {
                    newentry.endDate = "0000-00-00";
                }
                // Add entry
                tmplistentries.Add(newentry);
            }
            return tmplistentries;
        }

        /* 
         * Helpers
         */
        private int GetAniListUserID(String username)
        {
            // This methods find a user id associated with a username
            RestRequest request = new RestRequest("/", Method.POST);
            request.RequestFormat = DataFormat.Json;
            GraphQLQuery query = new GraphQLQuery();
            query.query = "query ($name: String) {\n  User (name: $name) {\n    id\n    name\n    mediaListOptions {\n      scoreFormat\n    }\n }\n}";
            query.variables = new Dictionary<string, object> { { "name", username } };
            request.AddJsonBody(query);
            IRestResponse response = arestclient.Execute(request);
            if (response.StatusCode.GetHashCode() == 200)
            {
                Dictionary<string, object> jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                Dictionary<string, object> data = JObjectToDictionary((JObject)jsonData["data"]);
                int userid = Convert.ToInt32((long)JObjectToDictionary((JObject)data["User"])["id"]);
                return userid;
            }
            else
            {
                return -1;
            }
        }

        private int GetKitsuUserID(String username)
        {
            // This methods find a user id associated with a username
            RestRequest request = new RestRequest("/users?filter[slug]=" + username, Method.GET);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Accept", "application/vnd.api+json");
            IRestResponse response = krestclient.Execute(request);
            if (response.StatusCode.GetHashCode() == 200)
            {
                Dictionary<string, object> jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                List<Dictionary<string, object>> data = ((JArray)jsonData["data"]).ToObject<List<Dictionary<string, object>>>();
                if (data.Count > 0) {
                    Dictionary<string, object> user = data[0];
                    int userid = int.Parse(((string)user["id"]));
                    return userid;
                }
                else
                {
                    return -1;
                }

            }
            else
            {
                return -1;
            }
        }

        private Dictionary<string, object> FindMetaData(int titleid, EntryType type)
        {
            // This methods finds the metadata that is associated with a title id
            String typestring = "";
            switch (type)
            {
                case EntryType.Anime:
                    typestring = "anime";
                    break;
                case EntryType.Manga:
                    typestring = "manga";
                    break;
            }
            Dictionary<string, object> searchpattern = new Dictionary<string, object>();
            searchpattern["id"] = titleid.ToString();
            searchpattern["type"] = typestring;
            return attributes.FirstOrDefault(x => searchpattern.All(x.Contains));
        }
        private Dictionary<string, object> FindMapping(int mappingid)
        {
            // This methods finds the metadata that is associated with a title id
            Dictionary<string, object> searchpattern = new Dictionary<string, object>();
            searchpattern["id"] = mappingid.ToString();
            return attributes.FirstOrDefault(x => searchpattern.All(x.Contains));
        }
        private void RecordMappingIds(int titleid, EntryType type)
        {
            List<Dictionary<string, object>> attributes = ((JArray)JObjectToDictionary((JObject)JObjectToDictionary((JObject)FindMetaData(titleid, type)["relationships"])["mappings"])["data"]).ToObject<List<Dictionary<string,object>>>();
            foreach (Dictionary<string, object> mapping in attributes)
            {
                int mappingid = int.Parse((string)mapping["id"]);
                Dictionary<string,object> titleidmap = FindMapping(mappingid);
                Dictionary<string, object> mapattributes = ((JObject)titleidmap["attributes"]).ToObject<Dictionary<string, object>>();
                if (mapattributes.ContainsKey("externalSite"))
                {
                    if ((string)mapattributes["externalSite"] == "myanimelist/" + (type == EntryType.Anime ? "anime" : "manga"))
                    {
                        int mtitleid = int.Parse((string)mapattributes["externalId"]);
                        if (tconverter.RetreiveSavedMALIDFromServiceID(Service.Kitsu, titleid, type) < 0)
                        {
                            tconverter.SaveIDtoDatabase(Service.Kitsu, mtitleid, titleid, type);
                        }
                    }
                }
            }
        }
        private int ConvertRatingTwentyToRawScore(int ratingTwenty)
        {
            double advrating = 0.0;
            switch (ratingTwenty)
            {
                case 2:
                    advrating = 1.0;
                    break;
                case 3:
                    advrating = 1.5;
                    break;
                case 4:
                    advrating = 2.0;
                    break;
                case 5:
                    advrating = 2.5;
                    break;
                case 6:
                    advrating = 3.0;
                    break;
                case 7:
                    advrating = 3.5;
                    break;
                case 8:
                    advrating = 4.0;
                    break;
                case 9:
                    advrating = 4.5;
                    break;
                case 10:
                    advrating = 5.0;
                    break;
                case 11:
                    advrating = 5.5;
                    break;
                case 12:
                    advrating = 6.0;
                    break;
                case 13:
                    advrating = 6.5;
                    break;
                case 14:
                    advrating = 7.0;
                    break;
                case 15:
                    advrating = 7.5;
                    break;
                case 16:
                    advrating = 8.0;
                    break;
                case 17:
                    advrating = 8.5;
                    break;
                case 18:
                    advrating = 9.0;
                    break;
                case 19:
                    advrating = 9.5;
                    break;
                case 20:
                    advrating = 10.0;
                    break;
                default:
                    break;
            }
            return (int)(advrating * 10);
        }
        private Dictionary<string,object> JObjectToDictionary(JObject jobject)
        {
            return jobject.ToObject<Dictionary<string, object>>();
        }
        public void cleanup()
        {
            tmplist = new List<Dictionary<string, object>>();
            attributes = new List<Dictionary<string, object>>();
        }
    }
}
