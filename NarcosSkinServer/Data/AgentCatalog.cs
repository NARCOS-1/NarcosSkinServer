using CounterStrikeSharp.API.Modules.Utils;
using NarcosSkinServer.Models;

namespace NarcosSkinServer.Data;

// Base faction player models, not the ~180 individually-skinned agent items from the
// economy (those need per-item vmdl paths pulled from the game files with a tool like
// Source2Viewer - not something safe to guess at). This is a starting roster of the
// stock CT/T faction bodies; add more the same way once you've confirmed a path.
public static class AgentCatalog
{
    public static readonly List<AgentDefinition> Agents =
    [
        new() { Name = "SAS", ModelPath = "agents/models/ctm_sas/ctm_sas.vmdl", Team = CsTeam.CounterTerrorist },
        new() { Name = "FBI", ModelPath = "agents/models/ctm_fbi/ctm_fbi.vmdl", Team = CsTeam.CounterTerrorist },
        new() { Name = "SWAT", ModelPath = "agents/models/ctm_swat/ctm_swat.vmdl", Team = CsTeam.CounterTerrorist },
        new() { Name = "CT Heavy", ModelPath = "agents/models/ctm_heavy/ctm_heavy.vmdl", Team = CsTeam.CounterTerrorist },

        new() { Name = "Phoenix", ModelPath = "agents/models/tm_phoenix/tm_phoenix.vmdl", Team = CsTeam.Terrorist },
        new() { Name = "Leet Krew", ModelPath = "agents/models/tm_leet/tm_leet.vmdl", Team = CsTeam.Terrorist },
        new() { Name = "Balkan", ModelPath = "agents/models/tm_balkan/tm_balkan.vmdl", Team = CsTeam.Terrorist },
        new() { Name = "T Heavy", ModelPath = "agents/models/tm_phoenix_heavy/tm_phoenix_heavy.vmdl", Team = CsTeam.Terrorist }
    ];
}
