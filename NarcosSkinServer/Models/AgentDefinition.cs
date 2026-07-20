using CounterStrikeSharp.API.Modules.Utils;

namespace NarcosSkinServer.Models;

public class AgentDefinition
{
    public string Name { get; set; } = "";

    public string ModelPath { get; set; } = "";

    public CsTeam Team { get; set; }
}
