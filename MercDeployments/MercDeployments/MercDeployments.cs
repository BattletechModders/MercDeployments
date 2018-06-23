using Harmony;
using System.Reflection;

namespace MercDeployments
{
    public class MercDeployments
    {
        internal static string ModDirectory;
        public static void Init(string directory, string settingsJSON) {
            var harmony = HarmonyInstance.Create("de.morphyum.MercDeployments");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ModDirectory = directory;
        }
    }
}
