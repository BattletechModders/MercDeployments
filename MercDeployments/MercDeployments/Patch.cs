using BattleTech;
using BattleTech.Save;
using BattleTech.Save.SaveGameStructure;
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
                if (contract.Override.travelOnly && !Fields.AlreadyRaised.ContainsKey(contract.Name)) {
                    Settings settings = Helper.LoadSettings();
                    contract.SetInitialReward(Mathf.RoundToInt(contract.InitialContractValue * settings.DeploymentSalaryMultiplier));
                    System.Random rand = new System.Random();
                    int numberOfMonth = rand.Next(1, settings.MaxMonth+1);
                    Fields.AlreadyRaised.Add(contract.Name, numberOfMonth);
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

                    int numberOfMonth = Fields.AlreadyRaised[contract.Name];
                    ReflectionHelper.InvokePrivateMethode(__instance, "setFieldText", new object[] { contractName, contract.Override.contractName + " (" + numberOfMonth + " Months)" });
                    ReflectionHelper.InvokePrivateMethode(__instance, "setFieldText", new object[] { contractMaxPay, contractMaxPay.text + " (per month)" });
                    ReflectionHelper.InvokePrivateMethode(__instance, "setFieldText", new object[] { contractMaxSalvage, contractMaxSalvage.text + " (per mission)" });
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
    [HarmonyPatch(typeof(StarSystem), "ResetContracts")]
    public static class StarSystem_ResetContracts_Patch {
        static void Postfix() {
            Fields.AlreadyRaised.Clear();
        }
    }

    [HarmonyPatch(typeof(GameInstanceSave))]
    [HarmonyPatch(new Type[] { typeof(GameInstance), typeof(SaveReason) })]
    public static class GameInstanceSave_Constructor_Patch {
        static void Postfix(GameInstanceSave __instance, GameInstance gameInstance, SaveReason saveReason) {
            Helper.SaveState(__instance.InstanceGUID, __instance.SaveTime);
            if (Fields.Deployment) {
                gameInstance.Simulation.CurSystem.SystemContracts.Clear();
                gameInstance.Simulation.CurSystem.SystemContracts.AddRange(Fields.DeploymentContracts.Values);
            }
        }
    }

    [HarmonyPatch(typeof(GameInstance), "Load")]
    public static class GameInstance_Load_Patch {
        static void Prefix(GameInstanceSave save) {
            Helper.LoadState(save.InstanceGUID, save.SaveTime);
        }
    }
    [HarmonyPatch(typeof(SimGameState), "Rehydrate")]
    public static class SimGameState_Rehydrate_Patch {
        static void Postfix(SimGameState __instance) {
            if (Fields.Deployment) {
                foreach (Contract contract in __instance.CurSystem.SystemContracts) {
                    Fields.DeploymentContracts.Add(contract.Name, contract);
                }
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
            Fields.DeploymentRemainingDays = __instance.Constants.Finances.QuarterLength * Fields.DeploymentLenght;
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
            Fields.DeploymentSalary = Mathf.RoundToInt(contract.InitialContractValue * contract.PercentageContractValue);
            Fields.DeploymentSalvage = contract.Override.salvagePotential;
            Fields.DeploymentLenght = Fields.AlreadyRaised[contract.Name];
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
                    __result -= Fields.DeploymentSalary;
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }


    [HarmonyPatch(typeof(SGCaptainsQuartersStatusScreen), "RefreshData")]
    public static class SGCaptainsQuartersStatusScreen_RefreshData_Patch {

        [HarmonyAfter(new string[] { "de.morphyum.MechMaintenanceByCost" })]
        static void Postfix(ref SimGameState __instance) {
            try {
                if (Fields.Deployment) {
                    ReflectionHelper.InvokePrivateMethode(__instance, "AddListLineItem", new object[] { ReflectionHelper.GetPrivateField(__instance, "SectionOneExpensesList"), "Deployment Salary", SimGameState.GetCBillString(0 - Fields.DeploymentSalary) });
                    TextMeshProUGUI SectionOneExpensesField = (TextMeshProUGUI)ReflectionHelper.GetPrivateField(__instance, "SectionOneExpensesField");
                    int newTotal = int.Parse(SectionOneExpensesField.text.Replace("¢", "").Replace(",", ""));
                    ReflectionHelper.InvokePrivateMethode(__instance, "SetField", new object[] { SectionOneExpensesField, SimGameState.GetCBillString(newTotal - Fields.DeploymentSalary) }, new Type[] { typeof(TextMeshProUGUI), typeof(string) });
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
                Fields.DeploymentRemainingDays--;
                if (Fields.DeploymentRemainingDays <= 0) {
                    Fields.Deployment = false;
                    SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(__instance);
                    interruptQueue.QueueGenericPopup("Deployment Over", "Thanks for your services.");
                }
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