using MelonLoader;

[assembly: MelonInfo(typeof(RepairHammer.Core), "Repair Hammer", "0.1.0", "ATT", null)]
[assembly: MelonGame("Alta", "A Township Tale")]
[assembly: MelonAdditionalDependencies("CustomRecipesAPI")]

namespace RepairHammer;

public sealed class Core : MelonMod
{
    internal static MelonLogger.Instance Logger { get; private set; } = null!;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;
        CustomRecipesAPI.Core.SetUpRecipes += RepairAlloyRecipeRegistration.Register;
        LoggerInstance.Msg("Repair Hammer initialized. The Repair Alloy small hammer repairs hot completed weapons on an anvil by 20% per strike.");
    }
}
