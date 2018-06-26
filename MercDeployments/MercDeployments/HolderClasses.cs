using BattleTech;
using BattleTech.Framework;
using System.Collections.Generic;
using System;

namespace MercDeployments {
    public class Settings {
        public float MissionChancePerDay = 0.1f;
        public float DeploymentSalaryMultiplier = 5f;
        public int MaxMonth = 3;
        public int DeploymentBreakRepCost = -30;
        public int DeploymentBreakMRBRepCost = -50;
    }
    
    public static class Fields {
            
        public static bool Deployment = false;
        public static Faction DeploymentEmployer = Faction.INVALID_UNSET;
        public static Faction DeploymentTarget = Faction.INVALID_UNSET;
        public static int DeploymentDifficulty = 1;
        public static float DeploymentNegotiatedSalvage = 1;
        public static float DeploymentNegotiatedPayment = 0;
        public static int DeploymentSalary = 100000;
        public static int DeploymentSalvage = 0;
        public static int DeploymentLenght = 0;
        public static int DeploymentRemainingDays = 0;

        public static Dictionary<string, Contract> DeploymentContracts = new Dictionary<string, Contract>();

        public static Dictionary<string, int> AlreadyRaised = new Dictionary<string, int>();
        public static bool InvertCBills = false;
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