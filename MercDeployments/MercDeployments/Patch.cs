using BattleTech;
using BattleTech.Save;
using BattleTech.Save.Test;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MercDeployments {
    [HarmonyPatch(typeof(SGContractsListItem), "Init")]
    public static class SGContractsListItem_Init_Patch {
        static void Prefix(SGContractsListItem __instance, Contract contract) {
            try {
                if (contract.Override.travelOnly) {
                    Settings settings = Helper.LoadSettings();
                    contract.SetInitialReward(Mathf.RoundToInt(contract.InitialContractValue * settings.DeploymentSalaryMultiplier));
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }

        static void Postfix(SGContractsListItem __instance, Contract contract) {
            try {
                if (contract.Override.travelOnly) {
                    TextMeshProUGUI contractName = (TextMeshProUGUI)ReflectionHelper.GetPrivateField(__instance, "contractName");
                    TextMeshProUGUI contractMaxPay = (TextMeshProUGUI)ReflectionHelper.GetPrivateField(__instance, "contractMaxPay");
                    TextMeshProUGUI contractMaxSalvage = (TextMeshProUGUI)ReflectionHelper.GetPrivateField(__instance, "contractMaxSalvage");

                    ReflectionHelper.InvokePrivateMethode(__instance, "setFieldText", new object[] { contractName, contract.Override.contractName + " (Deployment)" });
                    ReflectionHelper.InvokePrivateMethode(__instance, "setFieldText", new object[] { contractMaxPay, contractMaxPay.text + " (per month)" });
                    ReflectionHelper.InvokePrivateMethode(__instance, "setFieldText", new object[] { contractMaxSalvage, contractMaxSalvage.text + " (per mission)" });
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "Dehydrate")]
    public class SimGameState_Dehydrate_Patch {
        public static void Prefix(SimGameState __instance, SimGameSave save, ref SerializableReferenceContainer references) {
            SaveFields fields = new SaveFields(Fields.Deployment, Fields.DeploymentContracts,
                        Fields.DeploymentEmployer, Fields.DeploymentTarget, Fields.DeploymentDifficulty,
                        Fields.DeploymentNegotiatedSalvage, Fields.DeploymentNegotiatedPayment, Fields.DeploymentSallary, Fields.DeploymentSalvage);
            references.AddItem("MercDeployment", fields);
        }
    }

    [HarmonyPatch(typeof(SimGameState), "Rehydrate")]
    public class SimGameState_Rehydrate_Patch {
        public static void Postfix(SimGameState __instance, GameInstanceSave gameInstanceSave,
           ref List<Contract> ___globalContracts) {

            SimGameSave save = gameInstanceSave.SimGameSave;
            if (save.GlobalReferences.HasItem("MercDeployment")) {
                SaveFields fields = save.GlobalReferences.GetItem<SaveFields>("MercDeployment");
                Fields.Deployment = fields.Deployment;
                Fields.DeploymentContracts = fields.DeploymentContracts;
                Fields.DeploymentEmployer = fields.DeploymentEmployer;
                Fields.DeploymentTarget = fields.DeploymentTarget;
                Fields.DeploymentDifficulty = fields.DeploymentDifficulty;
                Fields.DeploymentNegotiatedSalvage = fields.DeploymentNegotiatedSalvage;
                Fields.DeploymentNegotiatedPayment = fields.DeploymentNegotiatedPayment;
                Fields.DeploymentSallary = fields.DeploymentSalary;
                Fields.DeploymentSalvage = fields.DeploymentSalvage;
            }
        }
    }

    [HarmonyPatch(typeof(AAR_SalvageScreen), "OnCompleted")]
    public static class AAR_SalvageScreen_OnCompleted_Patch {
        static void Postfix(AAR_SalvageScreen __instance) {
            try {
                Contract con = (Contract)ReflectionHelper.GetPrivateField(__instance, "contract");
                Fields.DeploymentContracts.Remove(con.Name);
            }
            catch (Exception e) {
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
            Fields.DeploymentSallary = Mathf.RoundToInt(contract.InitialContractValue * contract.PercentageContractValue);
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

    [HarmonyPatch(typeof(SimGameState), "GetExpenditures")]
    public static class SimGameState_GetExpenditures_Patch {

        static void Postfix(ref SimGameState __instance, ref int __result) {
            try {
                if (Fields.Deployment) {
                    __result -= Fields.DeploymentSallary;
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
    public static class SGCaptainsQuartersStatusScreen_RefreshData_Patch {

        static void Postfix(ref SimGameState __instance) {
            try {
                if (Fields.Deployment) {
                    ReflectionHelper.InvokePrivateMethode(__instance, "AddListLineItem", new object[] { ReflectionHelper.GetPrivateField(__instance, "SectionOneExpensesList"), "Deployment Salary", SimGameState.GetCBillString(0-Fields.DeploymentSallary) });
                    TextMeshProUGUI SectionOneExpensesField = (TextMeshProUGUI)ReflectionHelper.GetPrivateField(__instance, "SectionOneExpensesField");
                    int newTotal = int.Parse(SectionOneExpensesField.text.Replace("¢",""));
                    ReflectionHelper.InvokePrivateMethode(__instance, "SetField", new object[] { SectionOneExpensesField, SimGameState.GetCBillString(newTotal-Fields.DeploymentSallary) }, new Type[] { typeof(TextMeshProUGUI), typeof(string) });
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
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