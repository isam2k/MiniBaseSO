using HarmonyLib;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace MiniBase
{
    public class Mod : KMod.UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            PUtil.InitLibrary(false);
            MiniBaseOptions.Reload();
            new POptions().RegisterOptions(this, typeof(MiniBaseOptions));
            base.OnLoad(harmony);
        }
    }
}
