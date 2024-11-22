using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using System;
using System.IO;
using System.Numerics;

namespace ProgressPeeper
{
    public sealed partial class Plugin
    {
        private class ConfigurationWindow : Window, IDisposable
        {
            private const string Pad = "    ";
            private readonly Configuration configuration;

            public ConfigurationWindow(Plugin plugin) : base("ProgressPeeper###Configuration")
            {
                Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize;
                configuration = plugin.Configuration;
            }

            public void Dispose() { }

            public override void Draw()
            {
                var enabled = configuration.Enabled;
                var tomestoneAuthorizationToken = configuration.TomestoneAuthorizationToken;

                if (ImGui.Checkbox($"{Pad}Automatic Peeping{Pad}", ref enabled))
                {
                    configuration.Enabled = enabled;
                    configuration.Save();
                }
 
                ImGui.SameLine();

                if (ImGui.InputText($"{Pad}Tomestone Token{Pad}", ref tomestoneAuthorizationToken, 100, ImGuiInputTextFlags.Password))
                {
                    configuration.TomestoneAuthorizationToken = tomestoneAuthorizationToken;
                    configuration.Save();
                }

                ImGui.Spacing();

                if (ImGui.CollapsingHeader("Instructions", ImGuiTreeNodeFlags.Selected))
                {
                    ImGui.Spacing();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Bullet();

                    if (ImGui.Button("Go to your Tomestone account settings.")) Util.OpenLink("https://tomestone.gg/profile/account/");

                    ImGui.AlignTextToFramePadding();
                    ImGui.BulletText("Create an API Access Token.");

                    var image = Services.TextureProvider.GetFromFile(Path.Combine(Services.PluginInterface.AssemblyLocation.Directory?.FullName!, "guide-1.png")).GetWrapOrDefault();

                    if (image != null)
                    {
                        ImGui.Spacing();
                        ImGui.Image(image.ImGuiHandle, new Vector2(image.Width, image.Height));
                        ImGui.Spacing();
                    }

                    ImGui.AlignTextToFramePadding();
                    ImGui.BulletText("Paste it into the box above!");
                }
            }

            public void Open()
            {
                if (!IsOpen)
                {
                    Toggle();
                }
                else
                {
                    BringToFront();
                }
            }
        }
    }
}
