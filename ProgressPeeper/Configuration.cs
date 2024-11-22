using Dalamud.Configuration;
using ProgressPeeper.Helpers;
using System;

namespace ProgressPeeper;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public string TomestoneAuthorizationToken { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public void Save()
    {
        TomestoneClient.AuthorizationToken = TomestoneAuthorizationToken;
        Services.PluginInterface.SavePluginConfig(this);
    }
}
