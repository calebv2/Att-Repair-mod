using MelonLoader;

[assembly: MelonInfo(typeof(RepairHammer.Core), "Crystal Repair Hammer", "1.0", "ATT", null)]
[assembly: MelonGame("Alta", "A Township Tale")]

namespace RepairHammer;

public sealed class Core : MelonMod
{
    internal static MelonLogger.Instance Logger { get; private set; } = null!;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        LoggerInstance.Msg("Repair Hammer initialized. The Repair Alloy small hammer repairs hot completed weapons on an anvil by 20% per strike.");
    }

    public override void OnLateInitializeMelon()
    {
        RepairAlloyRecipeRegistration.Register();
    }
}
