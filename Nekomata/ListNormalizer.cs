using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;

namespace Nekomata
{
    class ListNormalizer
    {
        private List<Dictionary<String, Object>> tmplist;
        private List<Dictionary<String, Object>> attributes;
        RestClient restclient;
        private int currentuserid;
        private EntryType currenttype;

        public ListNormalizer()
        {
            restclient = new RestClient();
            tmplist = new List<Dictionary<string, object>>();
            attributes = new List<Dictionary<string, object>>();
        }
        
        public List<ListEntry> RetrieveAniListList(EntryType type, String Username)
        {
            this.currentuserid = this.GetAniListUserID(Username);
            if (this.currentuserid > 0)
            {
                this.currenttype = type;
                return this.PerformRetrieveAniListList(0);
            }
            else
            {
                return new List<ListEntry>();
            }
        }
    
        private List<ListEntry> PerformRetrieveAniListList(int page)
        {
            RestRequest request = new RestRequest("https://graphql.anilist.co", Method.POST);
            request.RequestFormat = DataFormat.Json;
            switch (currenttype)
            {
                case EntryType.Anime:
                    request.AddBody("{ \"query\" : \"query ($id : Int!, $page: Int) {\n  AnimeList: Page (page: $page) {\n    mediaList(userId: $id, type: ANIME) {\n      id :media{id}\n      entryid: id\n      title: media {title {\n        title: userPreferred\n      }}\n      episodes: media{episodes}\n      duration: media{duration}\n      image_url: media{coverImage {\n        large\n        medium\n      }}\n        type: media{format}\n      status: media{status}\n      score: score(format: POINT_100)\n      watched_episodes: progress\n      watched_status: status\n      rewatch_count: repeat\n      private\n      notes\n      watching_start: startedAt {\n        year\n        month\n        day\n      }\n      watching_end: completedAt {\n        year\n        month\n        day\n      }\n    }\n    pageInfo {\n      total\n      currentPage\n      lastPage\n      hasNextPage\n      perPage\n    }\n  }\n}\", \"variables\" : { \"id\" : " + currentuserid.ToString() + ", \"page\" : " + page.ToString() + "} }");
                    break;
                case EntryType.Manga:
                    request.AddBody("{ \"query\" : \"query ($id : Int!, $page: Int) {\n  AnimeList: Page (page: $page) {\n    mediaList(userId: $id, type: ANIME) {\n      id :media{id}\n      entryid: id\n      title: media {title {\n        title: userPreferred\n      }}\n      episodes: media{episodes}\n      duration: media{duration}\n      image_url: media{coverImage {\n        large\n        medium\n      }}\n        type: media{format}\n      status: media{status}\n      score: score(format: POINT_100)\n      watched_episodes: progress\n      watched_status: status\n      rewatch_count: repeat\n      private\n      notes\n      watching_start: startedAt {\n        year\n        month\n        day\n      }\n      watching_end: completedAt {\n        year\n        month\n        day\n      }\n    }\n    pageInfo {\n      total\n      currentPage\n      lastPage\n      hasNextPage\n      perPage\n    }\n  }\n}\", \"variables\" : { \"id\" : " + currentuserid.ToString() + ", \"page\" : " + page.ToString() + "} }");
                    break;
                default:
                    return new List<ListEntry>();
            }

            IRestResponse response = restclient.Execute(request);
            if (response.StatusCode.GetHashCode() == 200)
            {
                Dictionary<string, object> jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                Dictionary<string, object> list;
                switch (currenttype)
                {
                    case EntryType.Anime:
                        list = (Dictionary<string, object>)((Dictionary<string, object>)jsonData["data"])["AnimeList"];
                        break;
                    case EntryType.Manga:
                        list = (Dictionary<string, object>)((Dictionary<string, object>)jsonData["data"])["MangaList"];
                        break;
                    default:
                        return new List<ListEntry>();
                }
                Dictionary<string, object> pageData = (Dictionary<string, object>)list["page"];
                tmplist.AddRange((List<Dictionary<string, object>>)list["mediaList"]);
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
                return new List<ListEntry>();
            }
        }

        private List<ListEntry> NormalizeAniListAnimeList()
        {
            // Create temp list
            List<ListEntry> tmplistentries = new List<ListEntry>();

            foreach (Dictionary<String, Object> entry in this.tmplist)
            {
                int titleId = (int)((Dictionary<string, object>)entry["id"])["id"];
                String title = (String)((Dictionary<string, object>)((Dictionary<string, object>)entry["title"])["title"])["title"];
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
                int progress = (int)entry["watched_episodes"];
                ListEntry newentry = new ListEntry(titleId, title, eStatus, progress);
                newentry.totalSegment = (int)((Dictionary<string, object>)entry["episodes"])["episodes"];
                newentry.mediaFormat = (String)((Dictionary<string, object>)entry["type"])["format"];
                newentry.repeating = reconsuming;
                newentry.repeatCount = (int)entry["rewatch_count"];
                newentry.personalComments = (String)entry["notes"];
                newentry.rating = (int)entry["score"];
                if (!object.ReferenceEquals(null, ((Dictionary<string, object>)entry["watching_start"])["year"]) && !object.ReferenceEquals(null, ((Dictionary<string, object>)entry["watching_start"])["month"]) && !object.ReferenceEquals(null, ((Dictionary<string, object>)entry["watching_start"])["day"]))
                {
                    newentry.startDate = (int)((Dictionary<string, object>)entry["watching_start"])["year"] + "-" + (int)((Dictionary<string, object>)entry["watching_start"])["month"] + (int)((Dictionary<string, object>)entry["watching_start"])["day"];
                }
                else
                {
                    newentry.startDate = "0000-00-00";
                }
                if (!object.ReferenceEquals(null, ((Dictionary<string, object>)entry["watching_end"])["year"]) && !object.ReferenceEquals(null, ((Dictionary<string, object>)entry["watching_end"])["month"]) && !object.ReferenceEquals(null, ((Dictionary<string, object>)entry["watching_end"])["day"]))
                {
                    newentry.endDate = (int)((Dictionary<string, object>)entry["watching_end"])["year"] + "-" + (int)((Dictionary<string, object>)entry["watching_end"])["month"] + (int)((Dictionary<string, object>)entry["watching_end"])["day"];
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
                int titleId = (int)((Dictionary<string, object>)entry["id"])["id"];
                String title = (String)((Dictionary<string, object>)((Dictionary<string, object>)entry["title"])["title"])["title"];
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
                int progress = (int)entry["read_chapters"];
                int progressVolumes = (int)entry["read_volumes"];
                ListEntry newentry = new ListEntry(titleId,title,eStatus,progress,progressVolumes);
                newentry.totalSegment = (int)((Dictionary<string, object>)entry["chapters"])["chapters"];
                newentry.totalVolumes = (int)((Dictionary<string, object>)entry["volumes"])["volumes"];
                newentry.mediaFormat = (String)((Dictionary<string, object>)entry["type"])["format"];
                newentry.repeating = reconsuming;
                newentry.repeatCount = (int)entry["rewatch_count"];
                newentry.personalComments = (String)entry["notes"];
                newentry.rating = (int)entry["score"];
                if (!object.ReferenceEquals(null, ((Dictionary<string, object>)entry["read_start"])["year"]) && !object.ReferenceEquals(null, ((Dictionary<string, object>)entry["read_start"])["month"]) && !object.ReferenceEquals(null, ((Dictionary<string, object>)entry["read_start"])["day"]))
                {
                    newentry.startDate = (int)((Dictionary<string, object>)entry["read_start"])["year"] + "-" + (int)((Dictionary<string, object>)entry["read_start"])["month"] + (int)((Dictionary<string, object>)entry["read_start"])["day"];
                }
                else
                {
                    newentry.startDate = "0000-00-00";
                }
                if (!object.ReferenceEquals(null, ((Dictionary<string, object>)entry["read_end"])["year"]) && !object.ReferenceEquals(null, ((Dictionary<string, object>)entry["read_end"])["month"]) && !object.ReferenceEquals(null, ((Dictionary<string, object>)entry["read_end"])["day"]))
                {
                    newentry.endDate = (int)((Dictionary<string, object>)entry["read_end"])["year"] + "-" + (int)((Dictionary<string, object>)entry["read_end"])["month"] + (int)((Dictionary<string, object>)entry["read_end"])["day"];
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
            this.currentuserid = this.GetKitsuUserID(Username);
            if (this.currentuserid > 0)
            {
                this.currenttype = type;
                return this.PerformRetrieveKitsuList(0);
            }
            else
            {
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
                    includes = "canonicalTitle,episodeCount,episodeLength,showType,posterImage,status";
                    break;
                case EntryType.Manga:
                    listtype = "manga";
                    includes = "canonicalTitle,chapterCount,volumeCount,mangaType,posterImage,status";
                    break;
                default:
                    return new List<ListEntry>();
            }
            RestRequest request = new RestRequest("https://kitsu.io/api/edge/library-entries?filter[userId]=" + this.currentuserid + "&filter[kind]=" + listtype +"&include=" + listtype + "&fields[" + listtype + "]=" + includes + "&page[limit]=500&page[offset]=" + page, Method.GET);
            request.RequestFormat = DataFormat.Json;

            IRestResponse response = restclient.Execute(request);
            if (response.StatusCode.GetHashCode() == 200)
            {
                Dictionary<string, object> jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                List<Dictionary<string, object>> list;
                List<Dictionary<string, object>> metadata;
                list = (List<Dictionary<string, object>>)jsonData["data"];
                if (!object.ReferenceEquals(null,jsonData["data"]))
                {
                    metadata = (List<Dictionary<string, object>>)jsonData["data"];
                    attributes.AddRange(metadata);
                }
                tmplist.AddRange(list);
                bool nextpage = (!object.ReferenceEquals(null,(Dictionary<string, object>)((Dictionary<string, object>)jsonData["links"])["next"]));
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
                return new List<ListEntry>();
            }
        }

        private List<ListEntry> NormalizeKitsuAnimeList()
        {
            // Create temp list
            List<ListEntry> tmplistentries = new List<ListEntry>();

            foreach (Dictionary<String, Object> entry in this.tmplist)
            {
                int titleId = (int)entry["id"];
                Dictionary<string, object> attributes = FindMetaData(titleId);
                String title = (String)((Dictionary<string, object>)attributes["attributes"])["canonicalTitle"];
                String tmpstatus = (String)((Dictionary<string, object>)entry["attributes"])["status"];
                bool reconsuming = (bool)((Dictionary<string, object>)entry["attributes"])["reconsuming"];
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
                int progress = (int)((Dictionary<string, object>)entry["attributes"])["progress"];
                ListEntry newentry = new ListEntry(titleId, title, eStatus, progress);
                newentry.totalSegment = (!(object.ReferenceEquals(null, ((Dictionary<string, object>)attributes["attributes"])["episodeCount"]))) ? (int)((Dictionary<string, object>)attributes["attributes"])["episodeCount"] : 0;
                newentry.mediaFormat = (String)((Dictionary<string, object>)attributes["attributes"])["subtype"];
                newentry.repeating = reconsuming;
                newentry.repeatCount = (int)((Dictionary<string, object>)entry["attributes"])["reconsumeCount"];
                newentry.personalComments = (String)((Dictionary<string, object>)entry["attributes"])["notes"];
                newentry.rating = (!object.ReferenceEquals(null, ((Dictionary<string, object>)entry["attributes"])["ratingTwenty"])) ? ConvertRatingTwentyToRawScore((int)((Dictionary<string, object>)entry["attributes"])["ratingTwenty"]) : 0;
                if (!object.ReferenceEquals(null, ((Dictionary<string, object>)entry["attributes"])["startedAt"]))
                {
                    newentry.startDate = ((string)((Dictionary<string, object>)entry["attributes"])["startedAt"]).Substring(0, 10);
                }
                else
                {
                    newentry.startDate = "0000-00-00";
                }
                if (!object.ReferenceEquals(null, ((Dictionary<string, object>)entry["attributes"])["finishedAt"]))
                {
                    newentry.startDate = ((string)((Dictionary<string, object>)entry["attributes"])["finishedAt"]).Substring(0, 10);
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
                int titleId = (int)entry["id"];
                Dictionary<string, object> attributes = FindMetaData(titleId);
                String title = (String)((Dictionary<string, object>)attributes["attributes"])["canonicalTitle"];
                String tmpstatus = (String)((Dictionary<string, object>)entry["attributes"])["status"];
                bool reconsuming = (bool)((Dictionary<string, object>)entry["attributes"])["reconsuming"];
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
                int progress = (int)((Dictionary<string, object>)entry["attributes"])["progress"];
                int progressVolumes = (int)((Dictionary<string, object>)entry["attributes"])["volumesOwned"];
                ListEntry newentry = new ListEntry(titleId, title, eStatus, progress, progressVolumes);
                newentry.totalSegment = (!(object.ReferenceEquals(null, ((Dictionary<string, object>)attributes["attributes"])["chapterCount"]))) ? (int)((Dictionary<string, object>)attributes["attributes"])["chapterCount"] : 0;
                newentry.totalVolumes = (!(object.ReferenceEquals(null, ((Dictionary<string, object>)attributes["attributes"])["volumeCount"]))) ? (int)((Dictionary<string, object>)attributes["attributes"])["volumeCount"] : 0;
                newentry.mediaFormat = (String)((Dictionary<string, object>)attributes["attributes"])["subtype"];
                newentry.repeating = reconsuming;
                newentry.repeatCount = (int)((Dictionary<string, object>)entry["attributes"])["reconsumeCount"];
                newentry.personalComments = (String)((Dictionary<string, object>)entry["attributes"])["notes"];
                newentry.rating = (!object.ReferenceEquals(null, ((Dictionary<string, object>)entry["attributes"])["ratingTwenty"])) ? ConvertRatingTwentyToRawScore((int)((Dictionary<string, object>)entry["attributes"])["ratingTwenty"]) : 0;
                if (!object.ReferenceEquals(null, ((Dictionary<string, object>)entry["attributes"])["startedAt"]))
                {
                    newentry.startDate = ((string)((Dictionary<string, object>)entry["attributes"])["startedAt"]).Substring(0, 10);
                }
                else
                {
                    newentry.startDate = "0000-00-00";
                }
                if (!object.ReferenceEquals(null, ((Dictionary<string, object>)entry["attributes"])["finishedAt"]))
                {
                    newentry.startDate = ((string)((Dictionary<string, object>)entry["attributes"])["finishedAt"]).Substring(0, 10);
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
            RestRequest request = new RestRequest("https://graphql.anilist.co", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddBody("{ \"query\" : \"query ($name: String) {\n  User (name: $name) {\n    id\n    name\n    mediaListOptions {\n      scoreFormat\n    }\n }\n}\", \"variables\" : { \"name\" :" + username + "} }");

            IRestResponse response = restclient.Execute(request);
            if (response.StatusCode.GetHashCode() == 200)
            {
                Dictionary<string, object> jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                Dictionary<string, object> data = (Dictionary<string, object>)jsonData["data"];
                int userid = (int)((Dictionary<string, object>)data["User"])["id"];
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
            RestRequest request = new RestRequest("https://kitsu.io/api/edge/users?filter[slug]="+ username, Method.POST);
   
            IRestResponse response = restclient.Execute(request);
            if (response.StatusCode.GetHashCode() == 200)
            {
                Dictionary<string, object> jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                List<Dictionary<string, object>> data = (List<Dictionary<string, object>>)jsonData["data"];
                int userid = (int)data[0]["id"];
                return userid;
            }
            else
            {
                return -1;
            }
        }

        private Dictionary<string,object> FindMetaData(int titleid)
        {
            // This methods finds the metadata that is associated with a title id
            Dictionary<string, object> searchpattern = new Dictionary<string, object>();
            searchpattern["id"] = titleid;
            return attributes.FirstOrDefault(x => searchpattern.All(x.Contains));
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
            return (int)(advrating * 100);
        }
    }
}
