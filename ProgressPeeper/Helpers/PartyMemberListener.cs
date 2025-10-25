using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;

namespace ProgressPeeper.Helpers;

internal unsafe class PartyMemberListener : IDisposable
{
    internal class PartyMember : IEquatable<PartyMember>
    {
        public string Name = string.Empty;
        public string World = string.Empty;
        public bool IsLeader = false;

        public bool Equals(PartyMember? other) => other != null && Name == other.Name && World == other.World;
    }

    public event EventHandler<PartyMember>? OnJoin;
    public event EventHandler<List<PartyMember>>? OnChange;
    public event EventHandler<PartyMember>? OnLeave;

    private readonly TimeSpan tickInterval = TimeSpan.FromMilliseconds(200); // How often to check for party changes (5 times per second).

    private DateTime? leaveLock = null;
    private List<PartyMember> partyMembers = [new PartyMember { }]; // An initial empty value so that the first check will always be dirty.
    private TimeSpan updateDeltaCounter = TimeSpan.Zero;

    public PartyMemberListener()
    {
        Services.Framework.Update += OnFrameworkTick;
    }

    public void Dispose()
    {
        Services.Framework.Update -= OnFrameworkTick;
    }

    private void OnFrameworkTick(IFramework framework)
    {
        if ((updateDeltaCounter += framework.UpdateDelta) < tickInterval)
        {
            return;
        }
        else
        {
            updateDeltaCounter = TimeSpan.Zero;
        }

        if (!Services.ClientState.IsLoggedIn)
        {
            return;
        }

        var currentMemberList = new List<PartyMember>();

        var dirtyInInstance = false;
        var dirtyJoin = false;
        var dirtyLeave = false;

        if (InfoProxyCrossRealm.IsCrossRealmParty())
        {
            for (uint i = 0; i < InfoProxyCrossRealm.GetPartyMemberCount(); i++)
            {
                var groupMember = InfoProxyCrossRealm.GetGroupMember(i);

                var pm = new PartyMember
                {
                    Name = groupMember->NameString,
                    World = Services.DataManager.GetExcelSheet<World>()?.GetRow((uint)groupMember->HomeWorld).Name.ToString() ?? "???",
                    IsLeader = groupMember->IsPartyLeader,
                };

                if (!partyMembers.Contains(pm))
                {
                    dirtyJoin = true;
                    OnJoin?.Invoke(this, pm);
                }

                currentMemberList.Add(pm);
            }
        }
        else
        {
            foreach (var item in Services.PartyList)
            {
                var pm = new PartyMember
                {
                    Name = item.Name.ToString(),
                    World = Services.DataManager.GetExcelSheet<World>()?.GetRow(item.World.RowId).Name.ToString() ?? "???",
                    IsLeader = false,
                };

                if (!partyMembers.Contains(pm))
                {
                    dirtyInInstance = dirtyJoin = true;
                    OnJoin?.Invoke(this, pm);
                }

                currentMemberList.Add(pm);
            }
        }

        var missingMemberList = new List<PartyMember>();

        foreach (var item in partyMembers)
        {
            if (!currentMemberList.Contains(item) && !item.Equals(new PartyMember { }))
            {
                missingMemberList.Add(item);
            }
        }

        if (missingMemberList.Count > 0)
        {
            var processMissing = false;

            if (leaveLock != null && leaveLock < DateTime.Now)
            {
                processMissing = true;
                leaveLock = null;
            }
            else if (leaveLock == null)
            {
                if (!dirtyJoin)
                {
                    if (dirtyInInstance)
                    {
                        // Wait a few minutes before processing missing members while in instance.
                        // This will prevent the leave + rejoin of everyone that happens when you leave an instance but stay in the party.
                        leaveLock = DateTime.Now + TimeSpan.FromMinutes(1);
                    }
                    else if (!dirtyInInstance)
                    {
                        // Wait a few seconds before processing missing members outside of an instance.
                        // This will prevent the leave + rejoinof everyone that happens when you join an instance.
                        leaveLock = DateTime.Now + TimeSpan.FromSeconds(5);
                    }
                }
                else
                {
                    processMissing = true;
                    leaveLock = null;
                }
            }

            if (processMissing)
            {
                dirtyLeave = true;
                missingMemberList.ForEach(item => OnLeave?.Invoke(this, item));
            }
        }

        partyMembers = [.. currentMemberList];

        if (!dirtyJoin && !dirtyLeave)
        {
            return;
        }

        OnChange?.Invoke(this, partyMembers);
    }

    public List<PartyMember> GetCurrentMembers()
    {
        return partyMembers;
    }
}
