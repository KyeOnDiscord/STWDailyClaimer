namespace STWDailyClaimer;

internal sealed class Modal
{

    internal sealed class QueryProfileRoot
    {
        public int profileRevision { get; set; }
        public string profileId { get; set; }
        public int profileChangesBaseRevision { get; set; }
        public List<ProfileChange> profileChanges { get; set; }
        public int profileCommandRevision { get; set; }
        public DateTime serverTime { get; set; }
        public int responseVersion { get; set; }
    }

    internal sealed class ProfileChange
    {
        public Profile profile { get; set; }
    }

    internal sealed class Profile
    {
        public string _id { get; set; }
        public DateTime created { get; set; }
        public DateTime updated { get; set; }
        public int rvn { get; set; }
        public int wipeNumber { get; set; }
        public string accountId { get; set; }
        public string profileId { get; set; }
        public string version { get; set; }
        public Dictionary<string, Item> items { get; set; }
        public Stats stats { get; set; }
        public int commandRevision { get; set; }
    }

    internal sealed class Stats
    {
        public Attributes attributes { get; set; }
    }
    internal sealed class Item
    {
        public string templateId { get; set; }
        public Attributes attributes { get; set; }
        public int? quantity { get; set; }
    }

    public sealed class PastSeason
    {
        public PastSeason() { }
        public int seasonNumber { get; set; }
        public int numWins { get; set; }
        //public int numHighBracket { get; set; }
        //public int numLowBracket { get; set; }
        //public int seasonXp { get; set; }
        public int seasonLevel { get; set; }
        //public int bookXp { get; set; }
        public int bookLevel { get; set; }
        public bool purchasedVIP { get; set; }
        //    public int numRoyalRoyales { get; set; }
    }


    public class Season
    {
        public int numWins { get; set; }
        //public int numHighBracket { get; set; }
        //public int numLowBracket { get; set; }
    }


    public class QVariant
    {
        public string channel { get; set; }
        public string active { get; set; }
        public string[] owned { get; set; }
    }


    internal class Attributes
    {
        //athena
        public List<PastSeason> past_seasons { get; set; }
        public int season_match_boost { get; set; }
        public List<string> loadouts { get; set; }
        public int style_points { get; set; }
        public bool mfa_reward_claimed { get; set; }
        public bool favorite { get; set; } = false;
        public int rested_xp_overflow { get; set; }
        public DateTime last_xp_interaction { get; set; }
        public int rested_xp_golden_path_granted { get; set; }
        public int book_level { get; set; }
        public int season_num { get; set; }
        public int book_xp { get; set; }
        public Season season { get; set; }
        public int battlestars { get; set; }
        public int battlestars_season_total { get; set; }
        public int alien_style_points { get; set; }
        public int lifetime_wins { get; set; }
        public double rested_xp_exchange { get; set; }
        public int level { get; set; }
        public int rested_xp { get; set; }
        public double rested_xp_mult { get; set; }
        public int accountLevel { get; set; }
        public int style_points_season_total { get; set; }
        public int season_levels_purchased { get; set; }
        public int xp { get; set; }
        public DateTime last_match_end_datetime { get; set; }
        public string platform { get; set; }


        //skin attributes
        public QVariant[] variants { get; set; }


        //common_core
        public GiftHistory gift_history { get; set; }

        public class GiftHistory
        {
            public int num_sent { get; set; }
            public int num_received { get; set; }
        }
    }
}
