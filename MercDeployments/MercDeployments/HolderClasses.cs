using BattleTech;
using BattleTech.Framework;
using System.Collections.Generic;

namespace MercDeployments {
    public class Settings {
        public float MissionChancePerDay = 0.1f;
    }
    
    public static class Fields {
        public static bool Deployment = false;
        public static Dictionary<string, Contract> DeploymentContracts = new Dictionary<string, Contract>();
        public static Faction DeploymentEmployer = Faction.INVALID_UNSET;
        public static Faction DeploymentTarget = Faction.INVALID_UNSET;
        public static int DeploymentDifficulty = 1;
        public static float DeploymentNegotiatedSalvage = 1;
        public static float DeploymentNegotiatedPayment = 0;
        public static float DeploymentNegotiatedRep = 0;
        public static int DeploymentSallary = 100000;
        public static int DeploymentSalvage = 0;
    }

    public struct PotentialContract {
        // Token: 0x040089A4 RID: 35236
        public ContractOverride contractOverride;

        // Token: 0x040089A5 RID: 35237
        public Faction employer;

        // Token: 0x040089A6 RID: 35238
        public Faction target;

        // Token: 0x040089A7 RID: 35239
        public int difficulty;
    }
}