/* EntryClass.cs
 * This class specifies the ListEntry object, which is a entry on an Anime or Manga List
 * 
 * Copyright (c) 2018 MAL Updater OS X Group, a division of Moy IT Solutions
 * Licensed under MIT License
 */

using System;

namespace Nekomata
{
    enum EntryType
    {
        Anime,
        Manga
    }
    enum EntryStatus
    {
        current,
        completed,
        dropped,
        paused,
        planning
    }
    class ListEntry
    {
        public int titleId;
        public EntryType type;
        public String title;
        public EntryStatus entryStatus;
        public int progress;
        public int progressVolumes;
        public bool repeating;
        public int repeatCount;
        public String startDate;
        public String endDate;
        public String personalComments;
        public int totalSegment;
        public int totalVolumes;
        public String mediaFormat;
        public int rating;

        public ListEntry (int etitleid, String etitle, EntryStatus estatus, int eprogress)
        {
            // This constructor creates an anime entry
            this.titleId = etitleid;
            this.type = EntryType.Anime;
            this.title = etitle;
            this.entryStatus = estatus;
            this.progress = eprogress;
            this.progressVolumes = 0;
            this.repeating = false;
            this.repeatCount = 0;
            this.startDate = "";
            this.endDate = "";
            this.personalComments = "";
            this.rating = 0;
        }
        public ListEntry(int etitleid, String etitle, EntryStatus estatus, int eprogress, int eprogressvolumes)
        {
            // This constructor creates an manga entry
            this.titleId = etitleid;
            this.type = EntryType.Manga;
            this.title = etitle;
            this.entryStatus = estatus;
            this.progress = eprogress;
            this.progressVolumes = eprogressvolumes;
            this.repeating = false;
            this.repeatCount = 0;
            this.startDate = "";
            this.endDate = "";
            this.personalComments = "";
            this.rating = 0;
        }

        public String getEntryStatusStringMAL()
        {
            // Converts Enum EntryStatus to MAL String Status
            if (this.type == EntryType.Anime)
            {
                switch (this.entryStatus)
                {
                    case EntryStatus.current:
                        return "Watching";
                    case EntryStatus.completed:
                        return "Completed";
                    case EntryStatus.dropped:
                        return "Dropped";
                    case EntryStatus.planning:
                        return "Plan to Watch";
                    case EntryStatus.paused:
                        return "On Hold";
                    default:
                        return "";
                }
            }
            else if (this.type == EntryType.Manga)
            {
                switch (this.entryStatus)
                {
                    case EntryStatus.current:
                        return "Reading";
                    case EntryStatus.completed:
                        return "Completed";
                    case EntryStatus.dropped:
                        return "Dropped";
                    case EntryStatus.planning:
                        return "Plan to Read";
                    case EntryStatus.paused:
                        return "On Hold";
                    default:
                        return "";
                }
            }
            return "";
        }

        public void setRatingTwentytoStandardRating(int ratingTwenty)
        {
            // Converts Rating Twenty to a raw score
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
            // Convert to raw score
            this.rating = (int)(advrating * 100);
        }
    }
}
