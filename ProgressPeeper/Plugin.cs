using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ProgressPeeper.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProgressPeeper;

public sealed partial class Plugin : IDalamudPlugin
{
    private const string Command = "/ppeep";

    private readonly ConfigurationWindow configWindow;
    private readonly PartyMemberListener partyMemberListener;
    private readonly WindowSystem windowSystem = new("ProgressPeeper");

    public Configuration Configuration { get; init; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();
        Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        windowSystem.AddWindow(configWindow = new ConfigurationWindow(this));

        pluginInterface.UiBuilder.Draw += windowSystem.Draw;
        pluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;

        TomestoneClient.AuthorizationToken = Configuration.TomestoneAuthorizationToken;

        Services.CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = $"""
            Re-print status status for your current party.
            {Command} enable/disable → Toggle the automatic plugin functionality on/off.
            {Command} cfg → Open configuration window.
            {Command} <Character> @ <World> → Print status for the designated character.
            """
        });

        (partyMemberListener = new() { }).OnJoin += (sender, args) =>
        {
            if (TomestoneClient.HasAuthorizationToken() && Configuration.Enabled)
            {
                doProgressPeep(args.Name, args.World);
            }
        };
    }

    public void Dispose()
    {
        windowSystem.RemoveAllWindows();
        configWindow.Dispose();
        FancyChat.DynamicChatLinkHandlerInstance.Dispose();
        Services.CommandManager.RemoveHandler(Command);
    }

    private static void PrintChatError(string msg)
    {
        var builder = new SeStringBuilder();
        FancyChat.PrintLogo(ref builder);
        builder.AddUiForeground(msg, 73);
        Services.ChatGui.Print(builder.Build());
    }

    private void OnCommand(string command, string args)
    {
        if (args == "")
        {
            if (!TomestoneClient.HasAuthorizationToken())
            {
                PrintChatError($"No Tomestone token configured. Use '{Command} cfg' to open the configuration window.");
            }
            else
            {
                if (partyMemberListener.GetCurrentMembers() is var members && members.Count > 0)
                {
                    foreach (var member in partyMemberListener.GetCurrentMembers())
                    {
                        doProgressPeep(member.Name, member.World);
                    }
                }
                else
                {
                    var builder = new SeStringBuilder();
                    FancyChat.PrintLogo(ref builder);
                    builder.AddUiForeground("No party members.", 20);
                    Services.ChatGui.Print(builder.Build());
                }
            }
        }
        else if (args == "cfg")
        {
            configWindow.Open();
        }
        else if (args == "enable")
        {
            Configuration.Enabled = true;
            Configuration.Save();

            var builder = new SeStringBuilder();
            FancyChat.PrintLogo(ref builder);
            builder.AddUiForeground("Automatic peeping enabled.", 20);
            Services.ChatGui.Print(builder.Build());
        }
        else if (args == "disable")
        {
            Configuration.Enabled = false;
            Configuration.Save();

            var builder = new SeStringBuilder();
            FancyChat.PrintLogo(ref builder);
            builder.AddUiForeground("Automatic peeping disabled.", 20);
            Services.ChatGui.Print(builder.Build());
        }
        else
        {
            if (args.Split('@') is var parts && parts.Length == 2)
            {
                if (!TomestoneClient.HasAuthorizationToken())
                {
                    PrintChatError($"No Tomestone token configured. Use '{Command} cfg' to open the configuration window.");
                }
                else
                {
                    doProgressPeep(parts[0].Trim(), parts[1].Trim());
                }
            }
            else
            {
                var builder = new SeStringBuilder();
                FancyChat.PrintLogo(ref builder);
                builder.AddUiForeground($"Unknown option(s): {args}", 73);
                Services.ChatGui.Print(builder.Build());
            }
        }
    }

    public void ToggleConfigUI() => configWindow.Toggle();

    private static void doProgressPeep(string name, string world)
    {
        Task.Run(async () =>
        {
            TomestoneClient.ApiCharacter apiCharacter = new() { };

            try
            {
                var builder = new SeStringBuilder();
                var hasData = false;

                FancyChat.PrintLogo(ref builder);

                apiCharacter = await TomestoneClient.FetchCharacterOverview(name, world);

                FancyChat.WithLink(ref builder, apiCharacter.Link(), (ref SeStringBuilder builder) =>
                {
                    builder.AddUiForeground($"{name} ", 24);
                    builder.AddIcon(BitmapFontIcon.CrossWorld);
                    builder.AddUiForeground(world, 24);
                });

                List<TomestoneClient.ApiProgressionTarget> progressionTargets = [
                    apiCharacter.Encounters.ExtremesProgressionTarget,
                    apiCharacter.Encounters.SavageProgressionTarget,
                    apiCharacter.Encounters.UltimateProgressionTarget,
                ];

                progressionTargets.RemoveAll(item => item == null);

                foreach (var progressionTarget in progressionTargets)
                {
                    hasData = true;

                    builder.AddUiGlow(9);
                    builder.AddUiForeground(" Progress ", 10);
                    builder.AddUiGlowOff();
                    builder.AddUiForeground(" ", 10);

                    FancyChat.WithLink(ref builder, $"https://tomestone.gg{progressionTarget.Link}", (ref SeStringBuilder builder) =>
                    {
                        FancyChat.WithEncounterStyle(ref builder, 2, progressionTarget.Name, (ref SeStringBuilder builder) =>
                        {
                            if (progressionTarget.Name == "FRU")
                            {
                                builder.AddText(" ");
                            }

                            var text = $"{progressionTarget.Name} {progressionTarget.Percent}";
                            text += text.Contains('%') ? "" : "%";
                            builder.AddText(text);
                        });
                    });
                }

                List<TomestoneClient.ApiEncounter> clearedEncounters = [
                    ..apiCharacter.Encounters.Savage ?? [], // Does anyone really care about savage?
                    ..apiCharacter.Encounters.Ultimate ?? [],
                ];

                clearedEncounters.RemoveAll(item => item.Activity == null && item.Achievement == null);

                if (clearedEncounters.Count == 0)
                {
                    clearedEncounters.AddRange(apiCharacter.Encounters.Extremes);
                }

                clearedEncounters.RemoveAll(item => item.Activity == null && item.Achievement == null);

                foreach (var clearedEncounter in clearedEncounters)
                {
                    hasData = true;

                    builder.AddText(" ");

                    FancyChat.WithLink(ref builder, clearedEncounter.Link(apiCharacter.Name, apiCharacter.Id), (ref SeStringBuilder builder) =>
                    {
                        builder.AddUiGlow(45);
                        if (clearedEncounter.CompactName == "FRU")
                        {
                            builder.AddUiForeground("", 42);
                        }
                        else
                        {
                            builder.AddUiForeground("", 42);
                        }
                        builder.AddUiGlowOff();

                        FancyChat.WithEncounterStyle(ref builder, 2, clearedEncounter.CompactName, (ref SeStringBuilder builder) =>
                        {
                            builder.AddText(clearedEncounter.CompactName);
                        });
                    });
                }

                if (!hasData)
                {
                    builder.AddUiForeground(" Nothing to show.", 21);
                }

                Services.ChatGui.Print(builder.Build());
            }
            catch (Exception e)
            {
                var builder = new SeStringBuilder();

                FancyChat.PrintLogo(ref builder);

                FancyChat.WithLink(ref builder, apiCharacter.Link(), (ref SeStringBuilder builder) =>
                {
                    builder.AddUiForeground($"{name} ", 24);
                    builder.AddIcon(BitmapFontIcon.CrossWorld);
                    builder.AddUiForeground(world, 24);
                });

                builder.AddUiForeground(" Unable to fetch data from Tomestone" + e switch
                {
                    null => ".",
                    _ => e.Message.Length > 100 ? ": !!! Error string too long. !!!" : $": {e.Message}",
                }, 73);

                Services.ChatGui.Print(builder.Build());
            }
        });
    }
}
