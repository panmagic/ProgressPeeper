using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace ProgressPeeper.Helpers
{
    internal static class FancyChat
    {
        public delegate void RefSeStringAction(ref SeStringBuilder item);

        public class DynamicChatLinkHandler : IDisposable
        {
            private readonly List<uint> opCodes = [];

            public DalamudLinkPayload GetDynamicLinkPayload(string destination)
            {
                if (opCodes.Count > 100)
                {
                    Services.PluginInterface.RemoveChatLinkHandler(opCodes[0]);
                    opCodes.RemoveAt(0);
                }

                var hashed = MD5.HashData(Encoding.UTF8.GetBytes(destination));
                var opCode = BitConverter.ToUInt32(hashed, 0);
                opCodes.Add(opCode);

                Services.PluginInterface.RemoveChatLinkHandler(opCode);

                return Services.PluginInterface.AddChatLinkHandler(opCode, (uint OpCode, SeString clickedString) =>
                {
                    Process.Start(new ProcessStartInfo { FileName = destination, UseShellExecute = true });
                });
            }

            public void Dispose()
            {
                foreach (var opCode in opCodes)
                {
                    Services.PluginInterface.RemoveChatLinkHandler(opCode);
                }
            }
        }

        public static DynamicChatLinkHandler DynamicChatLinkHandlerInstance= new() { };

        public static void PrintLogo(ref SeStringBuilder builder)
        {
            builder.AddText(" ");
            builder.AddItalicsOn();
            builder.AddUiGlow(56);
            builder.AddUiForeground("Pr", 2);
            builder.AddUiForeground("og", 3);
            builder.AddUiForeground("re", 4);
            builder.AddUiForeground("ss", 5);
            builder.AddUiForeground(" ", 1);
            builder.AddUiForeground("Pe", 2);
            builder.AddUiForeground("ep", 3);
            builder.AddUiForeground("er", 4);
            builder.AddUiGlowOff();
            builder.AddItalicsOff();
            builder.AddText(" ");

            // builder.AddUiForeground("_", 37);
            // builder.AddUiForeground("\\", 37);
            // builder.AddUiForeground("|", 57);
            // builder.AddUiForeground("/", 35);
            // builder.AddUiForeground("_", 35);
            // builder.AddUiForeground(" ", 1);
        }

        public static void WithLink(ref SeStringBuilder builder, string url, RefSeStringAction func)
        {
            if (url == "")
            {
                func(ref builder);
                return;
            }

            builder.Add(DynamicChatLinkHandlerInstance.GetDynamicLinkPayload(url));
            func(ref builder);
            builder.Add(RawPayload.LinkTerminator);
        }

        public static void WithEncounterStyle(ref SeStringBuilder builder, ushort defaultColor, string encounterName, RefSeStringAction func)
        {
            ushort glow = encounterName switch
            {
                "FRU" => 1,
                _ => 0,
            };

            ushort color = encounterName switch
            {
                "UCOB" => 25,
                "UWU" => 34,
                "TEA" => 74,
                "DSR" => 28,
                "TOP" => 56,
                "FRU" => 7,
                _ => defaultColor,
            };

            if (glow != 0)
            {
                builder.AddUiGlow(glow);
            }

            builder.AddUiForeground(color);
            func(ref builder);
            builder.AddUiForegroundOff();

            if (glow != 0)
            {
                builder.AddUiGlowOff();
            }
        }
    }
}
