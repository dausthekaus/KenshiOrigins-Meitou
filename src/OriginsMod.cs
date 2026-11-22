using Meitou.Modding;
using Meitou.Simulation;

namespace KenshiOrigins;

/// <summary>The entry point: loads definitions, then registers the per-world system and editor/UI hooks.</summary>
public sealed class OriginsMod : IMod
{
    internal const string Id = "kenshiorigins";

    public void Initialize(ModContext context)
    {
        var catalog = OriginCatalog.Load(context.Folder, context.Name, context.Data, context.Log);
        context.AddSystem("origins", _ => new OriginSystem(catalog, context.RngPurpose, context.Log, context.Population.Items), SystemAnchor.End);
        OriginUi.Install(context.Harmony, catalog, context.LogError);
    }
}
