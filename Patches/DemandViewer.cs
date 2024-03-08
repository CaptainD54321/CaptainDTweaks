using System;
using HarmonyLib;
using BepInEx;
using SailwindModdingHelper;
using UnityEngine;

namespace CaptainDTweaks.Patches;

[HarmonyPatch(typeof(IslandMarket))]
internal static class MarketPatches {
    [HarmonyPatch("UpdateSelfPriceReport")]
    [HarmonyPostfix]
    public static void AddSupplyValues(ref IslandMarket __instance) {
        Plugin.logger.LogInfo($"CaptainDTweaks.DemandViewer: Adding supply values to port report for IslandMarket {__instance}");
        IslandMarket me = __instance;
        if (me == null) {Plugin.logger.LogError("CaptainDTweaks.DemandViewer: Updating price report failed, instance null");return;}
        int portIndex = me.GetPortIndex();
        if (me.knownPrices == null) {Plugin.logger.LogError("CaptainDTweaks.DemandViewer: Updating price report failed, known prices null");return;}
        SupplyPriceReport report = new SupplyPriceReport(me.knownPrices[portIndex]);
        if (me.currentSupply == null) {Plugin.logger.LogError("CaptainDTweaks.DemandViewer: Updating price report failed, current supply null");return;}
        for (int i = 0; i < me.currentSupply.Length; i++)
        {
            
            report.supplyValues[i] = me.currentSupply[i];
        }
        me.knownPrices[portIndex] = report;
    }
}

[HarmonyPatch(typeof(EconomyUI))]
internal static class TradeUIPatches {
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

        public override string ToString() {return $"islandNames: {islandNames}\ndaysAgo: {daysAgo}\nheaders: {headers}\nvertLines: {vertLines}\nshortVertLine: {shortVertLine}";}
    }

    private static UIHolder vanillaUI;
    private static UIHolder modUI;
    private static TextMesh supplyText;


    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    public static void WakeUp(ref GameObject ___ui) {
        Plugin.logger.LogInfo($"CaptainDTweaks.DemandViewer: beginning to modify UI, UI private field value: {___ui}");
        ///Plugin.logger.LogFatal("CAN ANYONE HEAR ME PLEASE LET ME OUT");
        Transform temp = ___ui.transform.Find("good details (right panel)/details UI");
        if (temp == null) {Plugin.logger.LogFatal("CaptainDTweaks.DemandViewer: Could not find UI subsection");return;}
        Plugin.logger.LogInfo($"CaptainDTweaks.DemandViewer: Found UI subsection transform: {temp}");
        GameObject ui = temp.gameObject;
        if(ui == null) { // if its null I've fucked up, skip everything so we don't crash the player
            Plugin.logger.LogError("CaptainDTweaks.DemandViewer: Cannot find good details UI section");
            return;
        }
        Plugin.logger.LogInfo($"CaptainDTweaks.DemandViewer: Found UI subsection gameobject: {ui}");
        vanillaUI = new UIHolder(); // create a UIHolder for the unmodified UI and put all the objects we'll be making modified copies of in there.
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

        Plugin.logger.LogInfo("Unmodified UI objects: \n"+vanillaUI);

        modUI = new UIHolder(); // create another one for the modified UI, and duplicate everything we need to change.
        modUI.islandNames = GameObject.Instantiate(vanillaUI.islandNames,ui.transform).GetComponent<TextMesh>();
        modUI.daysAgo = GameObject.Instantiate(vanillaUI.daysAgo,ui.transform).GetComponent<TextMesh>();
        modUI.headers = GameObject.Instantiate(vanillaUI.headers,ui.transform);
        modUI.vertLines = GameObject.Instantiate(vanillaUI.vertLines,ui.transform);
        modUI.shortVertLine = GameObject.Instantiate(vanillaUI.shortVertLine,ui.transform);

        if(modUI.CheckNull()) Plugin.logger.LogError("CaptainDTweaks.DemandViewer: one or more UI components not duplicated properly!");

        Plugin.logger.LogInfo("Duplicated UI objects: \n"+modUI);

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
        
    }

    private static void SetTransX(Transform transform, float x) { // helper method to save me typing this bullshit six times
        Vector3 temp = transform.localPosition;
        temp.x = x;
        transform.localPosition = temp;
    }

    [HarmonyPatch("OpenUI")]
    [HarmonyPrefix]
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
    public static void DisplaySupply(int goodIndex, ref int[][] ___bookmarkIslands, ref int ___currentBookmark, ref IslandMarket ___currentIsland) {
        // null check
        if(supplyText==null){Plugin.logger.LogError("CaptainDTweaks.DemandViewer: SupplyText not instantiated! Cannot update!"); return;}
        
        ShipItem good = PrefabsDirectory.instance.GetGood(goodIndex);
        Good component = good.GetComponent<Good>();
        supplyText.text = "";
        
        for (int i = 0; i < ___bookmarkIslands[___currentBookmark].Length; i++) { // iterate through the islands on this current page
            int island = ___bookmarkIslands[___currentBookmark][i];
            SupplyPriceReport report = new SupplyPriceReport(___currentIsland.knownPrices[island]); // get the report for that island
            
            // add the value or "-" if null (null only if the last report wasnt a SupplyPriceReport)
            supplyText.text += (report.supplyValues!=null ? Mathf.FloorToInt(report.supplyValues[goodIndex]).ToString():"-") + "\n";
        }

    }

}

public class SupplyPriceReport : PriceReport {
    // derived class from PriceReport that also stores supply values for goods

    public float[] supplyValues;

    public SupplyPriceReport():base() {}
    
    public SupplyPriceReport(PriceReport report):base(report) {}

    public SupplyPriceReport(SupplyPriceReport report):base(report) {
        if (report.supplyValues == null) {
            Plugin.logger.LogError("SupplyPriceReport: report.supplyValues is null");
        }
        supplyValues = new float[report.supplyValues.Length];
        for (int i = 0; i < report.supplyValues.Length; i++)
        {
            supplyValues[i] = report.supplyValues[i];
        }
    }
}