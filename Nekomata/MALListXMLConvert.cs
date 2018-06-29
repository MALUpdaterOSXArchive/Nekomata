using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;

namespace Nekomata
{
    class MALListXMLConvert
    {
        private List<Dictionary<String, Object>> tmplist;
        private List<Dictionary<String, Object>> attributes;
        RestClient restclient;
        private int currentuserid;
        private EntryType currenttype;

        public MALListXMLConvert()
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

        public List<ListEntry> RetrieveKitsuList(EntryType type, String Username)
        {
            this.currentuserid = this.GetKitsuUserID(Username);
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
                newentry.totalSegment = (int)((Dictionary<string, object>)entry["volumes"])["volumes"];
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
         * Helpers
         */
        private int GetAniListUserID(String username)
        {
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
    }
}
