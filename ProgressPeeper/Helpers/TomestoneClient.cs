using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ProgressPeeper.Helpers;

internal static class TomestoneClient
{
    private static readonly HttpClient Client = new();

    public static string AuthorizationToken
    {
        set
        {
            Client.DefaultRequestHeaders.Clear();

            if (value != string.Empty)
                Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + value);
        }
    }

    public static bool HasAuthorizationToken()
    {
        return Client.DefaultRequestHeaders.Contains("Authorization");
    }

    public static async Task<ApiCharacter> FetchCharacterOverview(string name, string world)
    {
        var response = await Client.GetAsync($"https://tomestone.gg/api/character/profile/{world}/{name}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new Exception("Character not found.");

        if (response.StatusCode != HttpStatusCode.OK)
            throw new Exception($"Failed to fetch character data ({response.StatusCode}).");

        var jsonContent = await response.Content.ReadAsStringAsync();

        var apiCharacter = JsonConvert.DeserializeObject<ApiCharacter>(jsonContent);

        return apiCharacter == null || !apiCharacter.IsValid() ? throw new Exception("Failed to parse character data.") : apiCharacter;
    }

    internal class ApiAchievement
    {
        [JsonProperty("id")]
        public int Id;
    }

    internal class ApiActivity
    {
        [JsonProperty("id")]
        public int Id;
    }

    internal class ApiCharacter
    {
        [JsonProperty("datacenter")]
        public string DataCenter = string.Empty;

        [JsonProperty("encounters")]
        public ApiEncounters Encounters = new();

        [JsonProperty("id")]
        public int Id;

        [JsonProperty("name")]
        public string Name = string.Empty;

        [JsonProperty("server")]
        public string Server = string.Empty;

        public string Link()
        {
            if (Id == 0 || Name == "")
                return "";

            return $"https://tomestone.gg/character/{Id}/{Name}";
        }

        public bool IsValid()
        {
            return Id != 0;
        }
    }

    internal class ApiEncounter
    {
        [JsonProperty("achievement")]
        public ApiAchievement? Achievement;

        [JsonProperty("activity")]
        public ApiActivity? Activity;

        [JsonProperty("compactName")]
        public string CompactName = string.Empty;

        [JsonProperty("id")]
        public int Id;

        [JsonProperty("name")]
        public string Name = string.Empty;

        [JsonProperty("zoneName")]
        public string ZoneName = string.Empty;

        public string Link(string name, int tomestoneId)
        {
            if (name == "" || tomestoneId == 0 || (Activity == null && Achievement == null))
                return "";

            if (ZoneName.Contains("Savage"))
                return $"https://tomestone.gg/character/{tomestoneId}/{name}/clears?encounterCategory=savage";

            if (ZoneName.Contains("Ultimate"))
                return $"https://tomestone.gg/character/{tomestoneId}/{name}/clears?encounterCategory=ultimate";

            return "";
        }
    }

    internal class ApiEncounters
    {
        [JsonProperty("extremes")]
        public List<ApiEncounter> Extremes = [];

        [JsonProperty("extremesProgressionTarget")]
        public ApiProgressionTarget? ExtremesProgressionTarget;

        [JsonProperty("savage")]
        public List<ApiEncounter> Savage = [];

        [JsonProperty("savageProgressionTarget")]
        public ApiProgressionTarget? SavageProgressionTarget;

        [JsonProperty("ultimate")]
        public List<ApiEncounter> Ultimate = [];

        [JsonProperty("ultimateProgressionTarget")]
        public ApiProgressionTarget? UltimateProgressionTarget;
    }

    internal class ApiProgressionTarget
    {
        [JsonProperty("encounter")]
        public ApiProgressionTargetEncounter Encounter = new();

        [JsonProperty("link")]
        public string? Link;

        [JsonProperty("name")]
        public string Name = string.Empty;

        [JsonProperty("percent")]
        public string Percent = string.Empty;

        [JsonProperty("updatedAt")]
        public int UpdatedAt;
    }

    internal class ApiProgressionTargetEncounter
    {
        [JsonProperty("id")]
        public int Id;

        [JsonProperty("name_en")]
        public string Name = string.Empty;
    }
}
