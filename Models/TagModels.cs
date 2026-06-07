using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AnimeTagsEditor.Models
{
    public class Example
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "";
    }

    public class Wiki
    {
        [JsonPropertyName("overview")]
        public string Overview { get; set; } = "";

        [JsonPropertyName("key_traits")]
        public List<string> KeyTraits { get; set; } = new();

        [JsonPropertyName("for_who")]
        public List<string> ForWho { get; set; } = new();

        [JsonPropertyName("examples")]
        public List<Example> Examples { get; set; } = new();

        [JsonPropertyName("related")]
        public List<string> Related { get; set; } = new();

        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; } = new();
    }

    public class Meta
    {
        [JsonPropertyName("popularity_rank")]
        public int? PopularityRank { get; set; }

        [JsonPropertyName("avg_rating")]
        public double? AvgRating { get; set; }

        [JsonPropertyName("beginner_friendly")]
        public bool? BeginnerFriendly { get; set; }
    }

    public class Tag
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = "";

        [JsonPropertyName("name_en")]
        public string NameEn { get; set; } = "";

        [JsonPropertyName("name_ru")]
        public string NameRu { get; set; } = "";

        [JsonPropertyName("short_desc")]
        public string ShortDesc { get; set; } = "";

        [JsonPropertyName("wiki")]
        public Wiki Wiki { get; set; } = new();

        [JsonPropertyName("meta")]
        public Meta? Meta { get; set; }

        // Чтобы в списке показывалось название, а не "AnimeTagsEditor.Models.Tag"
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(NameRu)) return NameRu;
            if (!string.IsNullOrEmpty(NameEn)) return NameEn;
            return Slug;
        }
    }

    public class TagsData
    {
        [JsonPropertyName("main_genres")]
        public List<Tag> MainGenres { get; set; } = new();

        [JsonPropertyName("demographics")]
        public List<Tag> Demographics { get; set; } = new();

        [JsonPropertyName("isekai_fantasy")]
        public List<Tag> IsekaiFantasy { get; set; } = new();

        [JsonPropertyName("sci_fi_tech")]
        public List<Tag> SciFiTech { get; set; } = new();

        [JsonPropertyName("school_life")]
        public List<Tag> SchoolLife { get; set; } = new();

        [JsonPropertyName("military_war")]
        public List<Tag> MilitaryWar { get; set; } = new();

        [JsonPropertyName("music_arts")]
        public List<Tag> MusicArts { get; set; } = new();

        [JsonPropertyName("relationships_romance")]
        public List<Tag> RelationshipsRomance { get; set; } = new();

        [JsonPropertyName("character_archetypes")]
        public List<Tag> CharacterArchetypes { get; set; } = new();

        [JsonPropertyName("plot_structures")]
        public List<Tag> PlotStructures { get; set; } = new();

        [JsonPropertyName("tone_mood")]
        public List<Tag> ToneMood { get; set; } = new();

        [JsonPropertyName("content_warnings")]
        public List<Tag> ContentWarnings { get; set; } = new();

        [JsonPropertyName("niche_aesthetics")]
        public List<Tag> NicheAesthetics { get; set; } = new();

        [JsonPropertyName("production_format")]
        public List<Tag> ProductionFormat { get; set; } = new();

        [JsonPropertyName("historical_periods")]
        public List<Tag> HistoricalPeriods { get; set; } = new();

        [JsonPropertyName("power_systems")]
        public List<Tag> PowerSystems { get; set; } = new();
    }
}