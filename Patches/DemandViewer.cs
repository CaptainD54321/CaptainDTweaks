using System;
using HarmonyLib;
using BepInEx;
using SailwindModdingHelper;
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Net.Http.Headers;

namespace CaptainDTweaks.Patches;

[HarmonyPatch(typeof(IslandMarket))]
internal static class MarketPatches {
    [HarmonyPatch("UpdateSelfPriceReport")]
    [HarmonyPostfix]
    // Whenever an island generates its PriceReport, make it a SupplyPriceReport and adjust for whatever RL's supplyPurchaseLimit is.
    public static void AddSupplyValues(ref IslandMarket __instance) {
        IslandMarket me = __instance;
        int portIndex = me.GetPortIndex();
        SupplyPriceReport report = new SupplyPriceReport(me.knownPrices[portIndex]);
        report.supplyValues = new float[me.currentSupply.Length];
        for (int i = 0; i < me.currentSupply.Length; i++)
        {
            report.supplyValues[i] = me.currentSupply[i] - me.supplyPurchaseLimit + 1f;
        }
        me.knownPrices[portIndex] = report;
    }

    [HarmonyPatch("ReceivePriceReports")]
    [HarmonyPrefix]
    public static bool ReceivePriceReports(PriceReport[] reports, ref PriceReport[] ___knownPrices, ref IslandMarket __instance) 
    {
        // Plugin.logger.LogInfo($"CaptainDTweaks.DemandViewer: receiving price reports at island {__instance.GetPortName()}");
        for (int i = 0; i < reports.Length; i++)
        {
            // Plugin.logger.LogInfo($"CaptainDTweaks.DemandViewer: receiving price report for port {i}");
            if (reports[i] != null && reports[i].approved && (___knownPrices[i] == null || ___knownPrices[i].day <= reports[i].day))
            {
                // Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer");
                if (reports[i] is SupplyPriceReport) {
                    ___knownPrices[i] = new SupplyPriceReport((SupplyPriceReport)reports[i]);
                }
                else ___knownPrices[i] = new PriceReport(reports[i]);
                // ___knownPrices[i] = new SupplyPriceReport(reports[i] as SupplyPriceReport ?? reports[i]);
            }
        }
        return false;
    }
}

[HarmonyPatch(typeof(TraderBoat))]
internal static class TraderBoatPatches {
    [HarmonyPatch("UpdateCarriedPriceReports")]
    [HarmonyPrefix]
    public static bool UpdateReportsFix(ref PriceReport[] ___carriedPriceReports, ref IslandMarket ___currentIslandMarket) {
        for (int i = 0; i < ___carriedPriceReports.Length; i++)
        {
            if (___currentIslandMarket.knownPrices[i] != null && ___currentIslandMarket.knownPrices[i].approved && (___carriedPriceReports[i] == null || ___currentIslandMarket.knownPrices[i].day >= ___carriedPriceReports[i].day))
            {
                if (___currentIslandMarket.knownPrices[i] is SupplyPriceReport) {
                    ___carriedPriceReports[i] = new SupplyPriceReport((SupplyPriceReport)___currentIslandMarket.knownPrices[i]);
                }
                else ___carriedPriceReports[i] = new PriceReport(___currentIslandMarket.knownPrices[i]);
                // ___carriedPriceReports[i] = new SupplyPriceReport(___currentIslandMarket.knownPrices[i] as SupplyPriceReport ?? ___currentIslandMarket.knownPrices[i]);
            }
        }
        return false;
    }
}

[HarmonyPatch(typeof(EconomyUI))]
internal static class TradeUIPatches {
    // Helper class to hold all the different gameobjects we need to toggle on and off
    internal class UIHolder {
        public TextMesh islandNames;
        public TextMesh daysAgo;
        public GameObject headers;
        public GameObject vertLines;
        public GameObject shortVertLine;

        public void SetActive(bool active) {
            islandNames.gameObject.SetActive(active);
            daysAgo.gameObject.SetActive(active);
            headers.SetActive(active);
            vertLines.SetActive(active);
            shortVertLine.SetActive(active);
        }

        public bool CheckNull() {return islandNames==null||daysAgo==null||headers==null||vertLines==null||shortVertLine==null;}

        //public override string ToString() {return $"islandNames: {islandNames}\ndaysAgo: {daysAgo}\nheaders: {headers}\nvertLines: {vertLines}\nshortVertLine: {shortVertLine}";}
    }

    private static UIHolder vanillaUI;
    private static UIHolder modUI;
    private static TextMesh supplyText;


    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    // On startup, find all the requisite pieces of the UI and tweak as necessary
    public static void WakeUp(ref GameObject ___ui) {
        Plugin.logger.LogInfo($"CaptainDTweaks.DemandViewer: Beginning to modify trade UI.");
        ///Plugin.logger.LogFatal("CAN ANYONE HEAR ME PLEASE LET ME OUT");
        Transform temp = ___ui.transform.Find("good details (right panel)/details UI");
        if (temp == null) {Plugin.logger.LogError("CaptainDTweaks.DemandViewer: Could not find UI subsection");return;}
        //Plugin.logger.LogInfo($"CaptainDTweaks.DemandViewer: Found UI subsection transform: {temp}");
        GameObject ui = temp.gameObject;
        if(ui == null) { // if its null I've fucked up, skip everything so we don't crash the player
            Plugin.logger.LogError("CaptainDTweaks.DemandViewer: Cannot find good details UI section");
            return;
        }
        //Plugin.logger.LogInfo($"CaptainDTweaks.DemandViewer: Found UI subsection gameobject: {ui}");
        // create a UIHolder for the unmodified UI and put all the objects we'll be making modified copies of in there.
        vanillaUI = new UIHolder(); 
        try {
            vanillaUI.islandNames = ui.transform.Find("static text (islands)").GetComponent<TextMesh>();
            vanillaUI.daysAgo = ui.transform.Find("text (days ago)").GetComponent<TextMesh>();
            vanillaUI.headers = ui.transform.GetChild(8).gameObject;            //Find("static text (buy/sell)").gameObject;
            vanillaUI.vertLines = ui.transform.GetChild(13).gameObject;         //Find("static text (buy/sell) (1)").gameObject;
            vanillaUI.shortVertLine = ui.transform.GetChild(15).gameObject;     //Find("static text (buy/sell) (3)").gameObject;
        } catch {
            Plugin.logger.LogError("CaptainDTweaks.DemandViewer: Failed to find one or more text components!");
            return;
        }

        if(vanillaUI.CheckNull()) Plugin.logger.LogError("CaptainDTweaks.DemandViewer: cannot find one or more UI components!");

        //Plugin.logger.LogInfo("Unmodified UI objects: \n"+vanillaUI);
        // create another UIHolder for the modified UI, and duplicate everything we need to change.
        modUI = new UIHolder(); 
        modUI.islandNames = GameObject.Instantiate(vanillaUI.islandNames,ui.transform).GetComponent<TextMesh>();
        modUI.daysAgo = GameObject.Instantiate(vanillaUI.daysAgo,ui.transform).GetComponent<TextMesh>();
        modUI.headers = GameObject.Instantiate(vanillaUI.headers,ui.transform);
        modUI.vertLines = GameObject.Instantiate(vanillaUI.vertLines,ui.transform);
        modUI.shortVertLine = GameObject.Instantiate(vanillaUI.shortVertLine,ui.transform);

        //if(modUI.CheckNull()) Plugin.logger.LogError("CaptainDTweaks.DemandViewer: one or more UI components not duplicated properly!");

        //Plugin.logger.LogInfo("Duplicated UI objects: \n"+modUI);

        supplyText = GameObject.Instantiate(modUI.daysAgo,ui.transform); // also create the supply text as a duplicate of the daysago text
        
        // Move the copied gameobjects and change the static texts

        SetTransX(modUI.islandNames.transform,0.37f);
        SetTransX(modUI.daysAgo.transform,0.25f);
        SetTransX(supplyText.transform,0.07f);
        SetTransX(modUI.headers.transform,0.45f);
        modUI.headers.GetComponent<TextMesh>().text = "days ago  supply        buy         sell       profit";
        SetTransX(modUI.vertLines.transform,0.39f);
        modUI.vertLines.GetComponent<TextMesh>().text = "       |        |           |            |            |";
        SetTransX(modUI.shortVertLine.transform,0.35f);
        
        Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Sucessfully set up UI!");
    }

    // helper method to save me typing this bullshit six times
    private static void SetTransX(Transform transform, float x) { 
        Vector3 temp = transform.localPosition;
        temp.x = x;
        transform.localPosition = temp;
    }

    [HarmonyPatch("OpenUI")]
    [HarmonyPrefix]
    // Whenever the player opens the trade UI, check if the mod is enabled and open either the modified or unmodified UI
    public static void OpenRightUI(ref TextMesh ___textIslandNames, ref TextMesh ___textDaysAgo) {
        // null check
        if(vanillaUI == null||modUI==null) {Plugin.logger.LogError("CaptainDTweaks.DemandViewer.OpenUI(): UI elements not instantiated correctly! Aborting!"); return;}
        // set the elements we want active, and the other ones inactive
        modUI.SetActive(Plugin.supplyDemand.Value);
        supplyText.gameObject.SetActive(Plugin.supplyDemand.Value);
        vanillaUI.SetActive(!Plugin.supplyDemand.Value);
        // set the private fields to the correct object (yes I'm a code golfer I love ternary expressions)
        ___textIslandNames = (Plugin.supplyDemand.Value?modUI:vanillaUI).islandNames;
        ___textDaysAgo = (Plugin.supplyDemand.Value?modUI:vanillaUI).daysAgo;
    }
    [HarmonyPatch("ShowGoodPage")]
    [HarmonyPostfix]
    // Update the supply values on the UI
    public static void DisplaySupply(int goodIndex, ref int[][] ___bookmarkIslands, ref int ___currentBookmark, ref IslandMarket ___currentIsland) {
        // null check
        if(supplyText==null){Plugin.logger.LogError("CaptainDTweaks.DemandViewer: SupplyText not instantiated! Cannot update!"); return;}
        
        ShipItem good = PrefabsDirectory.instance.GetGood(goodIndex);
        Good component = good.GetComponent<Good>();
        supplyText.text = "";
        
        for (int i = 0; i < ___bookmarkIslands[___currentBookmark].Length; i++) { // iterate through the islands on this current page
            int island = ___bookmarkIslands[___currentBookmark][i];
            // get the report for that island
            SupplyPriceReport report = ___currentIsland.knownPrices[island] as SupplyPriceReport; 

            //Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: " + report.supplyValues);

            // add the value or "-" if null (null only if the last report wasnt a SupplyPriceReport)
            supplyText.text += (report != null && report.supplyValues!=null ? Mathf.FloorToInt(report.supplyValues[goodIndex]).ToString():"-") + "\n";
        }
        
    }

}

[HarmonyPatch(typeof(SaveLoadManager))]
public class SaveTweaks {

    // After RL does his loading, load all saved pricereports and overwrite RL's ones
    [HarmonyPatch("LoadGame")]
    [HarmonyPostfix]
    public static void Load(ref TraderBoat[] ___traderBoats) {
        if (!ModSave.Load(Plugin.instance.Info, out CaptainDSaveContainer saveContainer)) {
            Plugin.logger.LogWarning("CaptainDTweaks.DemandViewer: Save file loading failed. File is either corrupt or does not exist. If this is the first time loading this save with this mod, this is normal.");
            return;
        }
        
        //Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Loading player known reports.");
        for (int i = 0; i < GameState.playerKnownPrices.Length; i++) {
            if (saveContainer.playerReports[i] == null) continue;
            GameState.playerKnownPrices[i] = saveContainer.playerReports[i];
        }
        //GameState.playerKnownPrices = saveContainer.playerReports;

        //Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Loading port known reports.");
        foreach (Port port in Port.ports) {
            if (port) {
                IslandMarket market = port.GetComponent<IslandMarket>();
                if (market) {
                    for (int i = 0; i < market.knownPrices.Length; i++) {
                        if (saveContainer.marketKnownReports[port.portIndex][i] == null) continue;
                        market.knownPrices[i] = saveContainer.marketKnownReports[port.portIndex][i];
                    }
                }
            }
        }

        //Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Loading trader boat reports.");
        //Plugin.logger.LogInfo($"CaptainDTweaks.DemandViewer: Number of trader boats: {___traderBoats.Length}.");
        for (int i = 0; i < ___traderBoats.Length; i++) {
            //Plugin.logger.LogInfo($"CaptainDTweaks.DemandViewer: Loading reports for trader boat #{i}.");
            for (int j = 0; j < saveContainer.traderBoatReports[i].Length; j++) {
                if (saveContainer.traderBoatReports[i][j] == null) continue;
                ___traderBoats[i].carriedPriceReports[j] = saveContainer.traderBoatReports[i][j];
            }
        }
        //Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Finished loading.");
    }

    // Prefix RL's save coroutine to save all SupplyPriceReports myself, 
    // and then convert them all to PriceReports before they reach RL's save code so it doesn't have a stroke
    [HarmonyPatch("DoSaveGame")]
    [HarmonyPostfix]
    public static IEnumerator SaveGame(IEnumerator original) {
        // Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Save code started.");
        original.MoveNext();
        CaptainDSaveContainer mySave = new CaptainDSaveContainer(); // initialize container
        TraderBoat[] traderBoats = SaveLoadManager.instance.GetPrivateField<TraderBoat[]>("traderBoats");
        //___busy = true;
        // Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Cleaning up saved data.");

        Port[] ports = Port.ports;
        // initialize fields
        // Plugin.logger.LogInfo($"CaptainDTweaks.DemandViewer: Null tests: GameState.playerKnownPrices: {GameState.playerKnownPrices == null}, Port.ports: {Port.ports == null}, traderBoats: {traderBoats == null}");
        if (GameState.playerKnownPrices != null) {
            mySave.playerReports = new SupplyPriceReport[GameState.playerKnownPrices.Length];
        }
        mySave.marketKnownReports = new SupplyPriceReport[ports.Length][];
        mySave.traderBoatReports = new SupplyPriceReport[traderBoats.Length][];

        if (GameState.playerKnownPrices != null) {
            // Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Fixing player reports.");
            // store the player's known prices as SupplyPriceReports and then convert them to PriceReports for RL's save code
            for (int i = 0; i < GameState.playerKnownPrices.Length; i++) {
                if(GameState.playerKnownPrices[i] == null) continue;
                mySave.playerReports[i] = GameState.playerKnownPrices[i] as SupplyPriceReport; // ?? new SupplyPriceReport(GameState.playerKnownPrices[i]);
                GameState.playerKnownPrices[i] = new PriceReport(GameState.playerKnownPrices[i]);
            }
        }

        // Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Fixing port known reports");
        //store the known prices for each report as SupplyPriceReports, and then convert them to PriceReports for RL
        foreach (Port port in ports) {
            if ((bool)port)
            {
                IslandMarket market = port.GetComponent<IslandMarket>();
                if ((bool)market)
                {
                    SupplyPriceReport[] reports = new SupplyPriceReport[market.knownPrices.Length];
                    for (int i = 0; i < market.knownPrices.Length; i++) {
                        if(market.knownPrices[i] == null) continue;
                        reports[i] = market.knownPrices[i] as SupplyPriceReport; // ?? new SupplyPriceReport(market.knownPrices[i]);
                        market.knownPrices[i] = new PriceReport(market.knownPrices[i]);
                    }
                    mySave.marketKnownReports[port.portIndex] = reports;
                }
            }
        }

        // Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Fixing Traderboat reports");
        // store each TraderBoat's report as a SupplyPriceReport, and then convert it for RL
        for (int i = 0; i < traderBoats.Length; i++) {
            PriceReport[] reports = traderBoats[i].carriedPriceReports;
            mySave.traderBoatReports[i] = new SupplyPriceReport[reports.Length];
            for (int j = 0; j < reports.Length; j++) {
                if(reports[j] == null) continue;
                mySave.traderBoatReports[i][j] = reports[j] as SupplyPriceReport; // ?? new SupplyPriceReport(reports[j]);
                reports[j] = new PriceReport(reports[j]);
            }
            traderBoats[i].carriedPriceReports = reports;
        }
        //__state = mySave;
        // Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Finished cleaning up.");
        // Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Saving mod data.");
        ModSave.Save(Plugin.instance.Info,mySave);
        // Plugin.logger.LogInfo("CaptainDTweaks.DemandViewer: Running RL save code.");
        yield return original;        
    }
}

[Serializable]
public class CaptainDSaveContainer {
    public SupplyPriceReport[] playerReports;
    public SupplyPriceReport[][] marketKnownReports;
    public SupplyPriceReport[][] traderBoatReports; 

}

[Serializable]
public class SupplyPriceReport : PriceReport {
    // derived class from PriceReport that also stores supply values for goods

    public float[] supplyValues;

    public SupplyPriceReport():base() {}
    
    public SupplyPriceReport(PriceReport report):base(report) {
        if (report is SupplyPriceReport) {
            supplyValues = ((SupplyPriceReport)report).supplyValues?.Clone() as float[];
        }
    }

    public SupplyPriceReport(SupplyPriceReport report):base(report) {
        if (report.supplyValues == null) {
            Plugin.logger.LogError("SupplyPriceReport: report.supplyValues is null");
        }
        supplyValues = report.supplyValues.Clone() as float[];
    }
}
