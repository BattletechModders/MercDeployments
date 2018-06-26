using BattleTech;
using BattleTech.Save;
using BattleTech.Save.SaveGameStructure;
using BattleTech.UI;
using DG.Tweening;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MercDeployments {
    [HarmonyPatch(typeof(SGContractsListItem), "Init")]
    public static class SGContractsListItem_Init_Patch {
        static void Prefix(SGContractsListItem __instance, Contract contract) {
            try {
                if (contract.Override.travelOnly && !Fields.AlreadyRaised.ContainsKey(contract.Name)) {
                    Settings settings = Helper.LoadSettings();
                    contract.SetInitialReward(Mathf.RoundToInt(contract.InitialContractValue * settings.DeploymentSalaryMultiplier));
                    System.Random rand = new System.Random();
                    int numberOfMonth = rand.Next(1, settings.MaxMonth + 1);
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
                Fields.DeploymentContracts = new Dictionary<string, Contract>();
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
    [HarmonyPatch(typeof(SGNavigationScreen), "OnTravelCourseAccepted")]
    public static class SGNavigationScreen_OnTravelCourseAccepted_Patch {
        static bool Prefix(SGNavigationScreen __instance) {
            try {
                if (Fields.Deployment) {
                    UIManager uiManager = (UIManager)AccessTools.Field(typeof(SGNavigationScreen), "uiManager").GetValue(__instance);
                    SimGameState simState = (SimGameState)AccessTools.Field(typeof(SGNavigationScreen), "simState").GetValue(__instance);
                    Action cleanup = delegate () {
                        uiManager.ResetFader(UIManagerFader_Controller.FadeUIContainer.PopupRoot);
                        simState.Starmap.Screen.AllowInput(true);
                    };
                    string primaryButtonText = "Break Contract";
                    string message = "WARNING: This action will break your current deplyoment contract. Your reputation with the employer and the MRB will be negatively affected.";
                    PauseNotification.Show("Navigation Change", message, simState.GetCrewPortrait(SimGameCrew.Crew_Sumire), string.Empty, true, delegate {
                        cleanup();
                        Fields.Deployment = false;
                        if (simState.DoesFactionGainReputation(Fields.DeploymentEmployer)) {
                            Settings settings = Helper.LoadSettings();
                            ReflectionHelper.InvokePrivateMethode(simState, "SetReputation", new object[] { Fields.DeploymentEmployer, settings.DeploymentBreakRepCost, StatCollection.StatOperation.Int_Add, null });
                            ReflectionHelper.InvokePrivateMethode(simState, "SetReputation", new object[] { Faction.MercenaryReviewBoard, settings.DeploymentBreakMRBRepCost, StatCollection.StatOperation.Int_Add, null });
                            AccessTools.Field(typeof(SimGameState), "activeBreadcrumb").SetValue(simState, null);
                        }
                        simState.Starmap.SetActivePath();
                        simState.SetSimRoomState(DropshipLocation.SHIP);
                    }, primaryButtonText, cleanup, "Cancel");
                    simState.Starmap.Screen.AllowInput(false);
                    uiManager.SetFaderColor(uiManager.UILookAndColorConstants.PopupBackfill, UIManagerFader.FadePosition.FadeInBack, UIManagerFader_Controller.FadeUIContainer.PopupRoot, true);
                    return false;
                }
                else {
                    return true;
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
                return true;
            }
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

    [HarmonyPatch(typeof(SGFinancialForecastWidget), "RefreshData")]
    public static class SGFinancialForecastWidget_RefreshData_Patch {
        static void Postfix(SGFinancialForecastWidget __instance) {
            try {
                SimGameState simState = (SimGameState)AccessTools.Field(typeof(SGFinancialForecastWidget), "simState").GetValue(__instance);             
                int expenditures = simState.GetExpenditures(false);
                if (expenditures < 0) {
                    
                    List<Image> PipFills = (List<Image>)AccessTools.Field(typeof(SGFinancialForecastWidget), "PipFills").GetValue(__instance);
                    List<UIColorRefTracker> PipColors = (List<UIColorRefTracker>)AccessTools.Field(typeof(SGFinancialForecastWidget), "PipColors").GetValue(__instance);
                    List<DOTweenAnimation> PipsToFlash = (List<DOTweenAnimation>)AccessTools.Field(typeof(SGFinancialForecastWidget), "PipsToFlash").GetValue(__instance);
                    for (int i = 0; i < PipFills.Count; i++) {
                        PipFills[i].gameObject.SetActive(true);
                        PipColors[i].SetUIColor(UIColor.Green);
                    }
                    PipsToFlash.ForEach(delegate (DOTweenAnimation tween) {
                        tween.DOPause();
                    });
                    UIColorRefTracker UnderlineColor = (UIColorRefTracker)AccessTools.Field(typeof(SGFinancialForecastWidget), "UnderlineColor").GetValue(__instance);
                    UIColorRefTracker ReportBGColor = (UIColorRefTracker)AccessTools.Field(typeof(SGFinancialForecastWidget), "ReportBGColor").GetValue(__instance);
                    UIColorRefTracker SpendingValueColor = (UIColorRefTracker)AccessTools.Field(typeof(SGFinancialForecastWidget), "SpendingValueColor").GetValue(__instance);
                    UIColorRefTracker FinancialTextColor = (UIColorRefTracker)AccessTools.Field(typeof(SGFinancialForecastWidget), "FinancialTextColor").GetValue(__instance);
                    Image BankrupcyIncomingOverlay = (Image)AccessTools.Field(typeof(SGFinancialForecastWidget), "BankrupcyIncomingOverlay").GetValue(__instance);
                    TextMeshProUGUI CurrSpendingValueText = (TextMeshProUGUI)AccessTools.Field(typeof(SGFinancialForecastWidget), "CurrSpendingValueText").GetValue(__instance);
                    UnderlineColor.SetUIColor(UIColor.Green);
                    ReportBGColor.SetUIColor(UIColor.Green);
                    SpendingValueColor.SetUIColor(UIColor.Green);
                    FinancialTextColor.SetUIColor(UIColor.Green);
                    CurrSpendingValueText.text = string.Format("{0} / mo (income)", SimGameState.GetCBillString(0-expenditures));
                    BankrupcyIncomingOverlay.gameObject.SetActive(false);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
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
        static void Prefix(SGCaptainsQuartersStatusScreen __instance) {
            try {
                Fields.InvertCBills = true;
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }

        [HarmonyAfter(new string[] { "de.morphyum.MechMaintenanceByCost" })]
        static void Postfix(SGCaptainsQuartersStatusScreen __instance) {
            try {
                if (Fields.Deployment) {
                    ReflectionHelper.InvokePrivateMethode(__instance, "AddListLineItem", new object[] { ReflectionHelper.GetPrivateField(__instance, "SectionOneExpensesList"), "Deployment Salary", SimGameState.GetCBillString(0 - Fields.DeploymentSalary) });
                    TextMeshProUGUI SectionOneExpensesField = (TextMeshProUGUI)ReflectionHelper.GetPrivateField(__instance, "SectionOneExpensesField");
                    int newTotal = int.Parse(SectionOneExpensesField.text.Replace("¢", "").Replace(",", ""));
                    ReflectionHelper.InvokePrivateMethode(__instance, "SetField", new object[] { SectionOneExpensesField, SimGameState.GetCBillString(newTotal - Fields.DeploymentSalary) }, new Type[] { typeof(TextMeshProUGUI), typeof(string) });
                }
                Fields.InvertCBills = false;

                TextMeshProUGUI EndOfQuarterFunds = (TextMeshProUGUI)AccessTools.Field(typeof(SGCaptainsQuartersStatusScreen), "EndOfQuarterFunds").GetValue(__instance);
                TextMeshProUGUI QuarterOperatingExpenses = (TextMeshProUGUI)AccessTools.Field(typeof(SGCaptainsQuartersStatusScreen), "QuarterOperatingExpenses").GetValue(__instance);
                TextMeshProUGUI CurrentFunds = (TextMeshProUGUI)AccessTools.Field(typeof(SGCaptainsQuartersStatusScreen), "CurrentFunds").GetValue(__instance);
                SimGameState simState = (SimGameState)AccessTools.Field(typeof(SGCaptainsQuartersStatusScreen), "simState").GetValue(__instance);
                ReflectionHelper.InvokePrivateMethode(__instance, "SetField", new object[] { EndOfQuarterFunds, SimGameState.GetCBillString(simState.Funds + simState.GetExpenditures(false)) }, new Type[] { typeof(TextMeshProUGUI), typeof(string) });
                ReflectionHelper.InvokePrivateMethode(__instance, "SetField", new object[] { QuarterOperatingExpenses, SimGameState.GetCBillString(simState.GetExpenditures(false)) }, new Type[] { typeof(TextMeshProUGUI), typeof(string) });
                ReflectionHelper.InvokePrivateMethode(__instance, "SetField", new object[] { CurrentFunds, SimGameState.GetCBillString(simState.Funds) }, new Type[] { typeof(TextMeshProUGUI), typeof(string) });
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
    [HarmonyPatch(typeof(SimGameState), "GetCBillString")]
    public static class SimGameState_GetCBillString_Patch {
        static void Postfix(ref string __result, int value) {
            if (Fields.InvertCBills) {
                __result = string.Format("{0}{1:n0}", '¢', 0 - value);
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