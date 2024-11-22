using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProgressPeeper.Helpers
{
    internal static class TomestoneClient
    {
        internal class ApiAchievement
        {
            [JsonProperty("id")] public int Id = 0;
        }

        internal class ApiActivity
        {
            [JsonProperty("id")] public int Id = 0;
        }

        internal class ApiCharacter
        {
            [JsonProperty("id")] public int Id = 0;
            [JsonProperty("name")] public string Name = string.Empty;
            [JsonProperty("server")] public string Server = string.Empty;
            [JsonProperty("datacenter")] public string DataCenter = string.Empty;
            [JsonProperty("encounters")] public ApiEncounters Encounters = new() { };

            public string Link()
            {
                if (Id == 0 || Name == "")
                {
                    return "";
                }

                return $"https://tomestone.gg/character/{Id}/{Name}";
            }

            public bool IsValid()
            {
                return Id != 0;
            }
        }

        internal class ApiEncounter
        {
            [JsonProperty("id")] public int Id = 0;
            [JsonProperty("name")] public string Name = string.Empty;
            [JsonProperty("compactName")] public string CompactName = string.Empty;
            [JsonProperty("zoneName")] public string ZoneName = string.Empty;
            [JsonProperty("activity")] public ApiActivity? Activity = null;
            [JsonProperty("achievement")] public ApiAchievement? Achievement = null;

            public string Link(string name, int tomestoneId)
            {
                if (name == "" || tomestoneId == 0 || Activity == null && Achievement == null)
                {
                    return "";
                }

                if (ZoneName.Contains("Savage"))
                {
                    return $"https://tomestone.gg/character/{tomestoneId}/{name}/clears?encounterCategory=savage";
                }

                if (ZoneName.Contains("Ultimate"))
                {
                    return $"https://tomestone.gg/character/{tomestoneId}/{name}/clears?encounterCategory=ultimate";
                }

                return "";
            }
        }

        internal class ApiEncounters
        {
            [JsonProperty("extremes")] public List<ApiEncounter> Extremes = [];
            [JsonProperty("extremesProgressionTarget")] public ApiProgressionTarget? ExtremesProgressionTarget = null;
            [JsonProperty("savage")] public List<ApiEncounter> Savage = [];
            [JsonProperty("savageProgressionTarget")] public ApiProgressionTarget? SavageProgressionTarget = null;
            [JsonProperty("ultimate")] public List<ApiEncounter> Ultimate = [];
            [JsonProperty("ultimateProgressionTarget")] public ApiProgressionTarget? UltimateProgressionTarget = null;
        }

        internal class ApiProgressionTarget
        {
            [JsonProperty("name")] public string Name = string.Empty;
            [JsonProperty("percent")] public string Percent = string.Empty;
            [JsonProperty("link")] public string? Link = null;
            [JsonProperty("encounter")] public ApiProgressionTargetEncounter Encounter = new() { };
            [JsonProperty("updatedAt")] public int UpdatedAt = 0;
        }

        internal class ApiProgressionTargetEncounter
        {
            [JsonProperty("id")] public int Id = 0;
            [JsonProperty("name_en")] public string Name = string.Empty;
        }

        private readonly static HttpClient Client = new() { };

        public static string AuthorizationToken
        {
            set
            {
                Client.DefaultRequestHeaders.Clear();
                Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + value);
            }
        }

        public static bool HasAuthorizationToken() => Client.DefaultRequestHeaders.Contains("Authorization");

        public static async Task<ApiCharacter> FetchCharacterOverview(string name, string world)
        {
            var response = await Client.GetAsync($"https://tomestone.gg/api/character/profile/{world}/{name}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new Exception("Character not found.");
            }
            else if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Failed to fetch character data ({response.StatusCode}).");
            }

            var jsonContent = await response.Content.ReadAsStringAsync();

            var apiCharacter = JsonConvert.DeserializeObject<ApiCharacter>(jsonContent);
            return apiCharacter == null || !apiCharacter.IsValid() ? throw new Exception("Failed to parse character data.") : apiCharacter;
        }
    }
}
