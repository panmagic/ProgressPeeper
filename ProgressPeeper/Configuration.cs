using System;
using Dalamud.Configuration;
using ProgressPeeper.Helpers;

namespace ProgressPeeper;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public bool Enabled { get; set; } = true;

    public bool PrintLogo { get; set; } = true;

    public bool PrintExtremeClears { get; set; } = true;

    public bool PrintSavageClears { get; set; } = true;

    public bool PrintUltimateClears { get; set; } = true;

    public string TomestoneAuthorizationToken { get; set; } = string.Empty;

    public bool UseShortCommand { get; set; } = true;

    public int Version { get; set; } = 0;
    public event EventHandler<Configuration>? OnSave;

    public void Save()
    {
        TomestoneClient.AuthorizationToken = TomestoneAuthorizationToken;
        Services.PluginInterface.SavePluginConfig(this);
        OnSave?.Invoke(this, this);
    }
}
