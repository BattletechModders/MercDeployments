using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;
using BattleTech.Save;
using BattleTech.Save.SaveGameStructure;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MercDeployments {

    [HarmonyPatch(typeof(GameInstanceSave))]
    [HarmonyPatch(new Type[] { typeof(GameInstance), typeof(SaveReason) })]
    public static class GameInstanceSave_Constructor_Patch {
        static void Postfix(GameInstanceSave __instance) {
            Helper.SaveState(__instance.InstanceGUID, __instance.SaveTime);
        }
    }

    [HarmonyPatch(typeof(GameInstance), "Load")]
    public static class GameInstance_Load_Patch {
        static void Prefix(GameInstanceSave save) {
            Helper.LoadState(save.InstanceGUID, save.SaveTime);
        }
    }

    [HarmonyPatch(typeof(AAR_SalvageScreen), "OnCompleted")]
    public static class AAR_SalvageScreen_OnCompleted_Patch {
        static void Postfix(AAR_SalvageScreen __instance) {
            try {
                Contract con = (Contract)ReflectionHelper.GetPrivateField(__instance, "contract");
                Fields.DeploymentContracts.Remove(con.Name);
            } catch(Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "OnBreadcrumbArrival")]
    public static class SimGameState_OnBreadcrumbArrival_Patch {
        static void Postfix(SimGameState __instance) {
            Fields.Deployment = true;
        }
    }

    [HarmonyPatch(typeof(SGTimePlayPause), "ToggleTime")]
    public static class SGTimePlayPause_ToggleTime_Patch {
        static bool Prefix(SGTimePlayPause __instance) {
            if (Fields.Deployment && Fields.DeploymentContracts.Count > 0) {
                return false;
            }
            else {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "PrepareBreadcrumb")]
    public static class SimGameState_PrepareBreadcrumb_Patch {
        static void Postfix(SimGameState __instance, ref Contract contract) {
            Fields.DeploymentContracts.Add(contract.Name, contract);
            Fields.DeploymentDifficulty = contract.Difficulty;
            Fields.DeploymentEmployer = contract.Override.employerTeam.faction;
            Fields.DeploymentTarget = contract.Override.targetTeam.faction;
            Fields.DeploymentNegotiatedPayment = contract.PercentageContractValue;
            Fields.DeploymentNegotiatedSalvage = contract.PercentageContractSalvage;
            Fields.DeploymentSallary = Mathf.RoundToInt( contract.InitialContractValue * contract.PercentageContractValue);
            Fields.DeploymentSalvage = contract.Override.salvagePotential;
            contract.SetInitialReward(0);
        }
    }
    

    [HarmonyPatch(typeof(SGRoomController_CmdCenter), "GetAllContracts")]
    public static class SGRoomController_CmdCenter_GetAllContracts_Patch {
        static bool Prefix(SGRoomController_CmdCenter __instance, ref List<Contract> __result) {
            if (Fields.Deployment) {
                List<Contract> list = Fields.DeploymentContracts.Values.ToList();
                __result = list;
                return false;
            }
            else {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "OnDayPassed")]
    public static class SimGameState_OnDayPassed_Patch {
        static void Postfix(SimGameState __instance) {
            if (Fields.Deployment) {
                Settings settings = Helper.LoadSettings();
                System.Random rand = new System.Random();
                if (rand.NextDouble() < settings.MissionChancePerDay) {
                    __instance.PauseTimer();
                    SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(__instance);
                    Contract newcon = Helper.GetNewContract(__instance, Fields.DeploymentDifficulty, Fields.DeploymentEmployer, Fields.DeploymentTarget);
                    newcon.SetInitialReward(0);
                    newcon.Override.salvagePotential = Fields.DeploymentSalvage;
                    newcon.SetNegotiatedValues(Fields.DeploymentNegotiatedPayment, Fields.DeploymentNegotiatedSalvage);
                    newcon.Override.disableNegotations = true;
                    Fields.DeploymentContracts.Add(newcon.Name, newcon);
                    interruptQueue.QueueGenericPopup("New Mission", "Our Employer has a new mission for us.");
                }
            }
        }



    }
    
}