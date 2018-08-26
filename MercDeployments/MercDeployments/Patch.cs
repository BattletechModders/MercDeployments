using BattleTech;
using BattleTech.Framework;
using BattleTech.Save;
using BattleTech.Save.SaveGameStructure;
using BattleTech.UI;
using DG.Tweening;
using Harmony;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MercDeployments {

    [HarmonyPatch(typeof(SGContractsWidget), "OnNegotiateClicked")]
    public static class SGContractsWidget_OnNegotiateClicked_Patch {

        static bool Prefix(SGContractsWidget __instance) {
            try {
                SimGameState Sim = (SimGameState)AccessTools.Property(typeof(SGContractsWidget), "Sim").GetValue(__instance, null);
                if (__instance.SelectedContract.Override.travelOnly && !__instance.SelectedContract.IsPriorityContract && Sim.ActiveMechs.Count < 8) {
                    string message = "Commander, a deployment is a longer term arrangement with an employer, that may require missions to be done without time between them for repairs. I strongly encourage you to only deploy on this arrangement if we are capable of fielding multiple lances with little or no time for repairs, just in case.";
                    PauseNotification.Show("Deployment", message,
                        Sim.GetCrewPortrait(SimGameCrew.Crew_Darius), string.Empty, true, delegate {
                            __instance.NegotiateContract(__instance.SelectedContract, null);
                        }, "Do it anyways", null, "Cancel");
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

    [HarmonyPatch(typeof(AAR_ContractObjectivesWidget), "FillInObjectives")]
    public static class AAR_ContractObjectivesWidget_FillInObjectives {

        static bool Prefix(AAR_ContractObjectivesWidget __instance) {
            try {
                if (Fields.Deployment) {
                    Settings settings = Helper.LoadSettings();
                    Contract theContract = (Contract)AccessTools.Field(typeof(AAR_ContractObjectivesWidget), "theContract").GetValue(__instance);
                    foreach (MissionObjectiveResult missionObjectiveResult in theContract.MissionObjectiveResultList) {
                        if (missionObjectiveResult.isPrimary) {
                            foreach (SimGameEventResult result in missionObjectiveResult.simGameEventResultList) {
                                result.Stats = null;
                            }
                            ReflectionHelper.InvokePrivateMethode(__instance, "AddObjective", new object[] { missionObjectiveResult });
                        }
                        else if (missionObjectiveResult.status == ObjectiveStatus.Succeeded) {
                            int Bonus = Mathf.RoundToInt(settings.BonusPercentage * Fields.DeploymentSalary);
                            string missionObjectiveResultString = "Bonus For Secondary Objective: " + SimGameState.GetCBillString(Bonus);
                            MissionObjectiveResult missionObjectiveResult2 = new MissionObjectiveResult(missionObjectiveResultString, "7facf07a-626d-4a3b-a1ec-b29a35ff1ac0", false, true, ObjectiveStatus.Succeeded, false);
                            ReflectionHelper.InvokePrivateMethode(__instance, "AddObjective", new object[] { missionObjectiveResult2 });
                        }
                        else {
                            ReflectionHelper.InvokePrivateMethode(__instance, "AddObjective", new object[] { missionObjectiveResult });
                        }
                    }
                    return false;
                }
                return true;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract {
        static void Postfix(Contract __instance) {
            try {
                if (Fields.Deployment) {
                    Settings settings = Helper.LoadSettings();
                    int bonusPayment = 0;
                    foreach (MissionObjectiveResult missionObjectiveResult in __instance.MissionObjectiveResultList) {
                        if (!missionObjectiveResult.isPrimary && missionObjectiveResult.status == ObjectiveStatus.Succeeded) {
                            bonusPayment += Mathf.RoundToInt(settings.BonusPercentage * Fields.DeploymentSalary);
                        }
                    }
                    int newMoneyResults = Mathf.FloorToInt(__instance.MoneyResults + bonusPayment);
                    ReflectionHelper.InvokePrivateMethode(__instance, "set_MoneyResults", new object[] { newMoneyResults });
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "_OnFirstPlayInit")]
    public static class SimGameState_FirstPlayInit_Patch {
        static void Postfix(SimGameState __instance) {
            Fields.Deployment = false;
        }
    }

    [HarmonyPatch(typeof(SGContractsListItem), "Init")]
    public static class SGContractsListItem_Init_Patch {
        static void Prefix(SGContractsListItem __instance, Contract contract) {
            try {
                if (contract.Override.travelOnly && !Fields.AlreadyRaised.ContainsKey(contract.Name) && !contract.IsPriorityContract) {
                    Settings settings = Helper.LoadSettings();
                    contract.SetInitialReward(Mathf.RoundToInt(contract.InitialContractValue * settings.DeploymentSalaryMultiplier));
                    contract.Override.difficultyUIModifier = 2;
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
                if (contract.Override.travelOnly && !contract.IsPriorityContract) {
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
            try {
                Fields.AlreadyRaised.Clear();
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(TaskTimelineWidget), "RemoveEntry")]
    public static class TaskTimelineWidget_RemoveEntry_Patch {
        static bool Prefix(WorkOrderEntry entry) {
            try {
                if (Fields.Deployment && entry.ID.Equals("Deployment End")) {
                    return false;
                }
                return true;
            }
            catch (Exception e) {
                Logger.LogError(e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(TaskTimelineWidget), "RegenerateEntries")]
    public static class TaskTimelineWidget_RegenerateEntries_Patch {
        static void Postfix(TaskTimelineWidget __instance) {
            try {
                if (Fields.Deployment) {
                    if (Fields.TimeLineEntry == null) {
                        Fields.TimeLineEntry = new WorkOrderEntry_Notification(WorkOrderType.NotificationGeneric, "Deployment End", "Deployment End");
                        Fields.TimeLineEntry.SetCost(Fields.DeploymentRemainingDays);
                    }
                    __instance.AddEntry(Fields.TimeLineEntry, false);
                    __instance.RefreshEntries();
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(GameInstanceSave), MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(GameInstance), typeof(SaveReason) })]
    public static class GameInstanceSave_Constructor_Patch {
        static void Postfix(GameInstanceSave __instance, GameInstance gameInstance, SaveReason saveReason) {
            try {
                Helper.SaveState(__instance.InstanceGUID, __instance.SaveTime);
                if (Fields.Deployment) {
                    gameInstance.Simulation.CurSystem.SystemContracts.Clear();
                    gameInstance.Simulation.CurSystem.SystemContracts.AddRange(Fields.DeploymentContracts.Values);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }

        }
    }

    [HarmonyPatch(typeof(GameInstance), "Load")]
    public static class GameInstance_Load_Patch {
        static void Prefix(GameInstanceSave save) {
            try {
                Helper.LoadState(save.InstanceGUID, save.SaveTime);
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "Rehydrate")]
    public static class SimGameState_Rehydrate_Patch {
        static void Postfix(SimGameState __instance, GameInstanceSave gameInstanceSave) {
            try {
                if (Fields.Deployment) {
                    Fields.DeploymentContracts = new Dictionary<string, Contract>();
                    foreach (Contract contract in __instance.CurSystem.SystemContracts) {
                        contract.Override.salvagePotential = Fields.DeploymentSalvage;
                        contract.Override.disableNegotations = true;
                        SimGameEventResult simGameEventResult = new SimGameEventResult();
                        SimGameResultAction simGameResultAction = new SimGameResultAction();
                        int num2 = 11;
                        simGameResultAction.Type = SimGameResultAction.ActionType.System_StartNonProceduralContract;
                        simGameResultAction.value = contract.mapName;
                        simGameResultAction.additionalValues = new string[num2];
                        simGameResultAction.additionalValues[0] = __instance.CurSystem.ID;
                        simGameResultAction.additionalValues[1] = contract.mapPath;
                        simGameResultAction.additionalValues[2] = contract.encounterObjectGuid;
                        simGameResultAction.additionalValues[3] = contract.Override.ID;
                        simGameResultAction.additionalValues[4] = (!contract.Override.useTravelCostPenalty).ToString();
                        simGameResultAction.additionalValues[5] = Fields.DeploymentEmployer.ToString();
                        simGameResultAction.additionalValues[6] = Fields.DeploymentTarget.ToString();
                        simGameResultAction.additionalValues[7] = contract.Difficulty.ToString();
                        simGameResultAction.additionalValues[8] = "true";
                        simGameResultAction.additionalValues[9] = Fields.DeploymentEmployer.ToString();
                        simGameResultAction.additionalValues[10] = contract.Override.travelSeed.ToString();
                        simGameEventResult.Actions = new SimGameResultAction[1];
                        simGameEventResult.Actions[0] = simGameResultAction;
                        contract.Override.OnContractSuccessResults.Add(simGameEventResult);
                        if (!gameInstanceSave.HasCombatData) {
                            AccessTools.Field(typeof(SimGameState), "activeBreadcrumb").SetValue(__instance, contract);
                        }
                        Fields.DeploymentContracts.Add(contract.Name, contract);
                    }
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SGContractsWidget), "OnContractAccepted")]
    [HarmonyPatch(new Type[] { typeof(bool) })]
    public static class SGContractsWidget_OnContractAccepted_Patch {
        static bool Prefix(SGContractsWidget __instance) {
            try {
                if (Fields.Deployment) {
                    HBSSliderInput NegPaymentSlider = (HBSSliderInput)AccessTools.Field(typeof(SGContractsWidget), "NegPaymentSlider").GetValue(__instance);
                    HBSSliderInput NegSalvageSlider = (HBSSliderInput)AccessTools.Field(typeof(SGContractsWidget), "NegSalvageSlider").GetValue(__instance);
                    float cbill = NegPaymentSlider.Value / NegPaymentSlider.ValueMax;
                    float salvage = NegSalvageSlider.Value / NegSalvageSlider.ValueMax;
                    __instance.SelectedContract.SetNegotiatedValues(cbill, salvage);
                    Action<bool> contractAccepted = (Action<bool>)AccessTools.Field(typeof(SGContractsWidget), "contractAccepted").GetValue(__instance);
                    contractAccepted(false);
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

    [HarmonyPatch(typeof(AAR_SalvageScreen), "OnCompleted")]
    public static class AAR_SalvageScreen_OnCompleted_Patch {
        static void Postfix(AAR_SalvageScreen __instance) {
            try {
                if (Fields.Deployment) {
                    Contract con = (Contract)ReflectionHelper.GetPrivateField(__instance, "contract");
                    Fields.DeploymentContracts.Remove(con.Name);
                    Fields.MissionsDoneCurrentMonth++;
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "OnBreadcrumbArrival")]
    public static class SimGameState_OnBreadcrumbArrival_Patch {
        static void Postfix(SimGameState __instance) {
            try {
                if (!__instance.ActiveTravelContract.IsPriorityContract) {
                    Fields.Deployment = true;
                    Fields.DeploymentRemainingDays = __instance.Constants.Finances.QuarterLength * Fields.DeploymentLenght;
                    Fields.TimeLineEntry = new WorkOrderEntry_Notification(WorkOrderType.NotificationGeneric, "Deployment End", "Deployment End");
                    Fields.TimeLineEntry.SetCost(Fields.DeploymentRemainingDays);
                    __instance.RoomManager.AddWorkQueueEntry(Fields.TimeLineEntry);
                    __instance.RoomManager.SortTimeline();
                    __instance.RoomManager.RefreshTimeline();
                    Fields.DeploymentContracts = new Dictionary<string, Contract>();
                    Fields.DeploymentContracts.Add(__instance.ActiveTravelContract.Name, __instance.ActiveTravelContract);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SGTimePlayPause), "ToggleTime")]
    public static class SGTimePlayPause_ToggleTime_Patch {
        static bool Prefix(SGTimePlayPause __instance) {
            try {
                if (Fields.Deployment && Fields.DeploymentContracts.Count > 0) {
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

    [HarmonyPatch(typeof(SGTimePlayPause), "ReceiveButtonPress")]
    public static class SGTimePlayPause_ReceiveButtonPress_Patch {
        static void Prefix(SGTimePlayPause __instance, string button) {
            try {
                if (button != null) {
                    if (Fields.Deployment && button == "LaunchContract") {
                        Fields.SkipPreparePostfix = true;
                    }
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "ForceTakeContract")]
    public static class SimGameState_ForceTakeContract_Patch {
        static void Prefix(SimGameState __instance, Contract c) {
            try {
                if (Fields.Deployment) {
                    c.SetInitialReward(0);
                    c.Override.salvagePotential = Fields.DeploymentSalvage;
                    c.SetNegotiatedValues(Fields.DeploymentNegotiatedPayment, Fields.DeploymentNegotiatedSalvage);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }


    [HarmonyPatch(typeof(SimGameState), "PrepareBreadcrumb")]
    public static class SimGameState_PrepareBreadcrumb_Patch {
        static void Postfix(SimGameState __instance, ref Contract contract) {
            try {
                if (!Fields.SkipPreparePostfix) {
                    Fields.DeploymentDifficulty = contract.Difficulty;
                    Fields.DeploymentEmployer = contract.Override.employerTeam.faction;
                    Fields.DeploymentTarget = contract.Override.targetTeam.faction;
                    Fields.DeploymentNegotiatedPayment = contract.PercentageContractValue;
                    Fields.DeploymentNegotiatedSalvage = contract.PercentageContractSalvage;
                    Fields.DeploymentSalary = Mathf.RoundToInt(__instance.GetScaledCBillValue(contract.InitialContractValue, contract.InitialContractValue * contract.PercentageContractValue));
                    Fields.DeploymentSalvage = contract.Override.salvagePotential;
                    Fields.DeploymentLenght = Fields.AlreadyRaised[contract.Name];
                    contract.Override.disableNegotations = true;
                    contract.SetInitialReward(0);
                }
                Fields.SkipPreparePostfix = false;
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
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
                        uiManager.ResetFader(UIManagerRootType.PopupRoot);
                        simState.Starmap.Screen.AllowInput(true);
                    };
                    string primaryButtonText = "Break Contract";
                    string message = "WARNING: This action will break your current deployment contract. Your reputation with the employer and the MRB will be negatively affected.";
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
                    uiManager.SetFaderColor(uiManager.UILookAndColorConstants.PopupBackfill, UIManagerFader.FadePosition.FadeInBack, UIManagerRootType.PopupRoot, true);
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
            try {
                if (Fields.Deployment) {
                    List<Contract> list = Fields.DeploymentContracts.Values.ToList();
                    __result = list;
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

                    UnderlineColor.SetUIColor(UIColor.Green);
                    ReportBGColor.SetUIColor(UIColor.Green);
                    SpendingValueColor.SetUIColor(UIColor.Green);
                    FinancialTextColor.SetUIColor(UIColor.Green);

                    BankrupcyIncomingOverlay.gameObject.SetActive(false);

                }
                TextMeshProUGUI CurrSpendingValueText = (TextMeshProUGUI)AccessTools.Field(typeof(SGFinancialForecastWidget), "CurrSpendingValueText").GetValue(__instance);
                CurrSpendingValueText.text = string.Format("{0} / mo", SimGameState.GetCBillString(0 - expenditures));
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
                if (Fields.Deployment && ((__instance.DayRemainingInQuarter <= Fields.DeploymentRemainingDays) || Fields.PaymentCall)) {
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
                SimGameState simState = (SimGameState)AccessTools.Field(typeof(SGCaptainsQuartersStatusScreen), "simState").GetValue(__instance);
                if (Fields.Deployment && (simState.DayRemainingInQuarter <= Fields.DeploymentRemainingDays)) {
                    ReflectionHelper.InvokePrivateMethode(__instance, "AddListLineItem", new object[] { ReflectionHelper.GetPrivateField(__instance, "SectionOneExpensesList"), "Deployment Salary", SimGameState.GetCBillString(0 - Fields.DeploymentSalary) });
                    TextMeshProUGUI SectionOneExpensesField = (TextMeshProUGUI)ReflectionHelper.GetPrivateField(__instance, "SectionOneExpensesField");
                    int newTotal = int.Parse(SectionOneExpensesField.text.Replace("¢", "").Replace(",", ""));
                    ReflectionHelper.InvokePrivateMethode(__instance, "SetField", new object[] { SectionOneExpensesField, SimGameState.GetCBillString(newTotal - Fields.DeploymentSalary) }, new Type[] { typeof(TextMeshProUGUI), typeof(string) });
                }
                Fields.InvertCBills = false;
                SGFinancialForecastWidget FinanceWidget = (SGFinancialForecastWidget)AccessTools.Field(typeof(SGCaptainsQuartersStatusScreen), "FinanceWidget").GetValue(__instance);
                FinanceWidget.RefreshData();
                TextMeshProUGUI EndOfQuarterFunds = (TextMeshProUGUI)AccessTools.Field(typeof(SGCaptainsQuartersStatusScreen), "EndOfQuarterFunds").GetValue(__instance);
                TextMeshProUGUI CurrentFunds = (TextMeshProUGUI)AccessTools.Field(typeof(SGCaptainsQuartersStatusScreen), "CurrentFunds").GetValue(__instance);

                if (simState.GetExpenditures(false) <= 0) {
                    TextMeshProUGUI QuarterOperatingExpenses = (TextMeshProUGUI)AccessTools.Field(typeof(SGCaptainsQuartersStatusScreen), "QuarterOperatingExpenses").GetValue(__instance);
                    UIColorRefTracker BR = QuarterOperatingExpenses.transform.parent.GetComponentsInChildren<UIColorRefTracker>().FirstOrDefault(x => x.name.Equals("BR"));
                    BR.colorRef.UIColor = UIColor.Green;
                    UIColorRefTracker BL = QuarterOperatingExpenses.transform.parent.GetComponentsInChildren<UIColorRefTracker>().FirstOrDefault(x => x.name.Equals("BL"));
                    BL.colorRef.UIColor = UIColor.Green;
                    UIColorRefTracker TL = QuarterOperatingExpenses.transform.parent.GetComponentsInChildren<UIColorRefTracker>().FirstOrDefault(x => x.name.Equals("TL"));
                    TL.colorRef.UIColor = UIColor.Green;
                    UIColorRefTracker TR = QuarterOperatingExpenses.transform.parent.GetComponentsInChildren<UIColorRefTracker>().FirstOrDefault(x => x.name.Equals("TR"));
                    TR.colorRef.UIColor = UIColor.Green;
                    UIColorRefTracker txt_opExpensesLabel = QuarterOperatingExpenses.transform.parent.GetComponentsInChildren<UIColorRefTracker>().FirstOrDefault(x => x.name.Equals("txt_opExpensesLabel"));
                    txt_opExpensesLabel.colorRef.UIColor = UIColor.Green;
                    UIColorRefTracker txt_opExpensesAmmount = QuarterOperatingExpenses.transform.parent.GetComponentsInChildren<UIColorRefTracker>().FirstOrDefault(x => x.name.Equals("txt_opExpensesAmmount"));
                    txt_opExpensesAmmount.colorRef.UIColor = UIColor.Green;
                }

                ReflectionHelper.InvokePrivateMethode(__instance, "SetField", new object[] { EndOfQuarterFunds, SimGameState.GetCBillString(simState.Funds + simState.GetExpenditures(false)) }, new Type[] { typeof(TextMeshProUGUI), typeof(string) });
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
            try {
                if (Fields.InvertCBills) {
                    __result = string.Format("{0}{1:n0}", '¢', 0 - value);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "OnDayPassed")]
    public static class SimGameState_OnDayPassed_Patch {
        static void Prefix(SimGameState __instance, int timeLapse) {
            try {
                int num = (timeLapse <= 0) ? 1 : timeLapse;
                if ((__instance.DayRemainingInQuarter - num <= 0)) {
                    Fields.PaymentCall = true;
                    Fields.MissionsDoneCurrentMonth = 0;
                }
                if (Fields.Deployment) {
                    Fields.DeploymentRemainingDays -= num;
                }
                if (Fields.TimeLineEntry != null) {
                    Fields.TimeLineEntry.PayCost(num);
                    TaskManagementElement taskManagementElement4 = null;
                    TaskTimelineWidget timelineWidget = (TaskTimelineWidget)AccessTools.Field(typeof(SGRoomManager), "timelineWidget").GetValue(__instance.RoomManager);
                    Dictionary<WorkOrderEntry, TaskManagementElement> ActiveItems = (Dictionary<WorkOrderEntry, TaskManagementElement>)AccessTools.Field(typeof(TaskTimelineWidget), "ActiveItems").GetValue(timelineWidget);
                    if (ActiveItems.TryGetValue(Fields.TimeLineEntry, out taskManagementElement4)) {
                        taskManagementElement4.UpdateItem(0);
                    }
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }

        static void Postfix(SimGameState __instance) {
            try {
                Fields.PaymentCall = false;
                if (Fields.Deployment) {
                    if (Fields.DeploymentRemainingDays <= 0) {
                        __instance.StopPlayMode();
                        Fields.Deployment = false;
                        SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(__instance);
                        interruptQueue.QueueGenericPopup("Deployment Over", "Thanks for your services.");
                        Fields.DeploymentContracts = new Dictionary<string, Contract>();
                        __instance.CurSystem.SystemContracts.Clear();
                        __instance.RoomManager.RefreshTimeline();
                        AccessTools.Field(typeof(SimGameState), "activeBreadcrumb").SetValue(__instance, null);

                    }
                    else {
                        Settings settings = Helper.LoadSettings();
                        System.Random rand = new System.Random();
                        int ChanceDivider = Mathf.Max(1, 2 ^ ((Fields.MissionsDoneCurrentMonth + 1) - Mathf.RoundToInt((__instance.Constants.Finances.QuarterLength * settings.MissionChancePerDay))));
                        if (rand.NextDouble() < settings.MissionChancePerDay / ChanceDivider) {
                            __instance.PauseTimer();
                            __instance.StopPlayMode();

                            SimGameInterruptManager interruptQueue = (SimGameInterruptManager)AccessTools.Field(typeof(SimGameState), "interruptQueue").GetValue(__instance);
                            Contract newcon = Helper.GetNewContract(__instance, Fields.DeploymentDifficulty, Fields.DeploymentEmployer, Fields.DeploymentTarget);
                            newcon.SetInitialReward(0);
                            newcon.Override.salvagePotential = Fields.DeploymentSalvage;
                            newcon.SetNegotiatedValues(Fields.DeploymentNegotiatedPayment, Fields.DeploymentNegotiatedSalvage);
                            newcon.Override.disableNegotations = true;
                            SimGameEventResult simGameEventResult = new SimGameEventResult();
                            SimGameResultAction simGameResultAction = new SimGameResultAction();
                            int num2 = 11;
                            simGameResultAction.Type = SimGameResultAction.ActionType.System_StartNonProceduralContract;
                            simGameResultAction.value = newcon.mapName;
                            simGameResultAction.additionalValues = new string[num2];
                            simGameResultAction.additionalValues[0] = __instance.CurSystem.ID;
                            simGameResultAction.additionalValues[1] = newcon.mapPath;
                            simGameResultAction.additionalValues[2] = newcon.encounterObjectGuid;
                            simGameResultAction.additionalValues[3] = newcon.Override.ID;
                            simGameResultAction.additionalValues[4] = (!newcon.Override.useTravelCostPenalty).ToString();
                            simGameResultAction.additionalValues[5] = Fields.DeploymentEmployer.ToString();
                            simGameResultAction.additionalValues[6] = Fields.DeploymentTarget.ToString();
                            simGameResultAction.additionalValues[7] = newcon.Difficulty.ToString();
                            simGameResultAction.additionalValues[8] = "true";
                            simGameResultAction.additionalValues[9] = Fields.DeploymentEmployer.ToString();
                            simGameResultAction.additionalValues[10] = newcon.Override.travelSeed.ToString();
                            simGameEventResult.Actions = new SimGameResultAction[1];
                            simGameEventResult.Actions[0] = simGameResultAction;
                            newcon.Override.OnContractSuccessResults.Add(simGameEventResult);
                            AccessTools.Field(typeof(SimGameState), "activeBreadcrumb").SetValue(__instance, newcon);
                            Fields.DeploymentContracts.Add(newcon.Name, newcon);
                            Action primaryAction = delegate () {
                                __instance.QueueCompleteBreadcrumbProcess(true);
                            };
                            interruptQueue.QueueTravelPauseNotification("New Mission", "Our Employer has a new mission for us.", __instance.GetCrewPortrait(SimGameCrew.Crew_Darius),
                            string.Empty, new Action(primaryAction), "Proceed", new Action(__instance.OnBreadcrumbWait), "Not Yet");
                        }
                    }
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
}