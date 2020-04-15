using System;
using System.Linq;
using System.Collections.Generic;
using static System.StringComparer;

using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

using CellarAvailable.Framework;


namespace CellarAvailable {
    public class ModEntry : Mod {
        private bool showCommunityUpgrade_;

        public override void Entry(IModHelper helper) {
            this.Helper.Events.GameLoop.DayStarted += OnDayStarted;
            this.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            // Hook into MenuChanged event to intercept dialogues.
            this.Helper.Events.Display.MenuChanged += OnMenuChanged;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e) {
            // Read persisted config.
            ModConfig config = this.Helper.ReadConfig<ModConfig>();

            // Create a config entry for this save game if necessary.
            string saveGameName = $"{Game1.GetSaveGameName()}_{Game1.uniqueIDForThisGame}";
            if (!config.SaveGame.ContainsKey(saveGameName)) {
                config.SaveGame.Add(saveGameName, new ConfigEntry());
                this.Helper.WriteConfig(config);
            }

            this.showCommunityUpgrade_ = config.SaveGame[saveGameName].ShowCommunityUpgrade;
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e) {
            FarmHouse farmHouse = Utility.getHomeOfFarmer(Game1.player);
            CreateCellarEntrance(farmHouse);
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e) {
            // Nothing to do.
            if (!showCommunityUpgrade_) {
                return;
            }

            // Unfinished requirements.
            bool ccIsComplete = Game1.MasterPlayer.mailReceived.Contains("ccIsComplete") ||
                                Game1.MasterPlayer.hasCompletedCommunityCenter();
            bool jojaMember = Game1.MasterPlayer.mailReceived.Contains("JojaMember");
            // Community upgrade in progress or already completed.
            bool communityUpgradeInProgress = (Game1.getLocationFromName("Town") as Town).daysUntilCommunityUpgrade.Value > 0;
            bool pamHouseUpgrade = Game1.MasterPlayer.mailReceived.Contains("pamHouseUpgrade");

            if ((!ccIsComplete && !jojaMember) || communityUpgradeInProgress || pamHouseUpgrade) {
                return;
            }

            // Intercept carpenter's menu.
            if (e.NewMenu is DialogueBox dialogue) {
                string text = this.Helper.Reflection.GetField<List<string>>(dialogue, "dialogues").GetValue().FirstOrDefault();
                string menuText = Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu");
                if (text == menuText) {
                    this.Monitor.Log("Intercepting carpenter's menu", LogLevel.Debug);
                    List<Response> responses = this.Helper.Reflection.GetField<List<Response>>(dialogue, "responses").GetValue();
                    Response upgrade          = responses.FirstOrDefault(r => r.responseKey == "Upgrade");
                    Response communityUpgrade = responses.FirstOrDefault(r => r.responseKey == "CommunityUpgrade");
                    if (upgrade == null || communityUpgrade != null) {
                        return;
                    }

                    // Replace "Upgrade" by "CommunityUpgrade".
                    upgrade.responseKey = "CommunityUpgrade";
                    upgrade.responseText = Game1.content.LoadString("Strings\\Locations:ScienceHouse_CarpenterMenu_CommunityUpgrade");
                }
            }
        }

        private void CreateCellarEntrance(FarmHouse farmHouse) {
            if (farmHouse.upgradeLevel >= 3) {
                // The large farm house already has a cellar, nothing to do.

                return;
            }

            // First check the cellar map for the required warp points.
            // If that fails we get an exception and can't continue.
            Tuple<Warp, Warp> warps = GetCellarToFarmHouseWarps(farmHouse);

            if (farmHouse.upgradeLevel == 0)
            {
                // The small farm house has a narrower cellar entrance.
                this.Monitor.Log("Create narrow cellar entrance.");

                // Remove tiles for cellar entrance.
                farmHouse.removeTile(7, 10, "Front");
                farmHouse.removeTile(7, 11, "Back");
                farmHouse.removeTile(7, 11, "Buildings");

                // Remove tiles for walls.
                farmHouse.removeTile(6, 11, "Buildings");
                farmHouse.removeTile(8, 11, "Buildings");

                // Rebuild wall.
                farmHouse.setMapTileIndex(6, 10, 162, "Front");
                farmHouse.setMapTileIndex(8, 10, 163, "Front");
                farmHouse.setMapTileIndex(6, 11, 96, "Buildings");
                farmHouse.setMapTileIndex(7, 11, 165, "Front");
                farmHouse.setMapTileIndex(8, 11, 130, "Buildings");

                // Add stairs.
                farmHouse.setMapTileIndex(7, 11, 1043, "Back");
                farmHouse.setTileProperty(7, 11, "Back", "NoFurniture", "t");
                farmHouse.setTileProperty(7, 11, "Back", "NPCBarrier", "t");

                // Warp points to cellar.
                this.Monitor.Log("Create warp to cellar.");
                farmHouse.cellarWarps = new List<Warp> {
                    new Warp(7, 12, farmHouse.GetCellarName(), 3, 2, false)
                };
                farmHouse.updateCellarWarps();

                // Warp points from cellar.
                warps.Item1.TargetX = 7;
                warps.Item1.TargetY = 11;
                this.Monitor.Log($"Adjusted warp in cellar: {warps.Item1.TargetName}, ({warps.Item1.TargetX}, {warps.Item1.TargetY})");

                warps.Item2.TargetX = 7;
                warps.Item2.TargetY = 11;
                this.Monitor.Log($"Adjusted warp in cellar: {warps.Item2.TargetName}, ({warps.Item2.TargetX}, {warps.Item2.TargetY})");
            }
            else if (farmHouse.upgradeLevel == 1)
            {
                // Cellar entrance has normal width but can't be as 'deep' as in the big house.
                this.Monitor.Log("Create cellar entrance.");

                // Remove tiles for cellar entrance.
                farmHouse.removeTile(4, 10, "Front");
                farmHouse.removeTile(5, 10, "Front");
                farmHouse.removeTile(4, 11, "Back");
                farmHouse.removeTile(5, 11, "Back");
                farmHouse.removeTile(4, 11, "Buildings");
                farmHouse.removeTile(5, 11, "Buildings");

                // Rebuild wall.
                farmHouse.setMapTileIndex(3, 10, 162, "Front");
                farmHouse.setMapTileIndex(6, 10, 163, "Front");
                farmHouse.setMapTileIndex(3, 11, 96, "Buildings");
                farmHouse.setMapTileIndex(4, 11, 165, "Front");
                farmHouse.setMapTileIndex(5, 11, 165, "Front");
                farmHouse.setMapTileIndex(6, 11, 130, "Buildings");

                // Add stairs.
                farmHouse.setMapTileIndex(4, 11, 1043, "Back");
                farmHouse.setMapTileIndex(5, 11, 1043, "Back");
                farmHouse.setTileProperty(4, 11, "Back", "NoFurniture", "t");
                farmHouse.setTileProperty(5, 11, "Back", "NoFurniture", "t");
                farmHouse.setTileProperty(4, 11, "Back", "NPCBarrier", "t");
                farmHouse.setTileProperty(5, 11, "Back", "NPCBarrier", "t");

                // Warp points to cellar.
                this.Monitor.Log("Create warps to cellar.");
                farmHouse.cellarWarps = new List<Warp> {
                    new Warp(4, 12, farmHouse.GetCellarName(), 3, 2, false),
                    new Warp(5, 12, farmHouse.GetCellarName(), 4, 2, false)
                };
                farmHouse.updateCellarWarps();

                // Warp points from cellar.
                warps.Item1.TargetX = 4;
                warps.Item1.TargetY = 11;
                this.Monitor.Log($"Adjusted warp in cellar: {warps.Item1.TargetName}, ({warps.Item1.TargetX}, {warps.Item1.TargetY})");

                warps.Item2.TargetX = 5;
                warps.Item2.TargetY = 11;
                this.Monitor.Log($"Adjusted warp in cellar: {warps.Item2.TargetName}, ({warps.Item2.TargetX}, {warps.Item2.TargetY})");
            }
            else if (farmHouse.upgradeLevel == 2) {
                this.Monitor.Log("Upgrade farm house to make cellar available.");

                // The easiest case: Set the upgrade level to three.
                farmHouse.upgradeLevel = 3;
                farmHouse.updateFarmLayout();

                // Warp points from cellar.
                warps.Item1.TargetX = 4;
                warps.Item1.TargetY = 24;
                this.Monitor.Log($"Adjusted warp in cellar: {warps.Item1.TargetName}, ({warps.Item1.TargetX}, {warps.Item1.TargetY})");

                warps.Item2.TargetX = 5;
                warps.Item2.TargetY = 24;
                this.Monitor.Log($"Adjusted warp in cellar: {warps.Item2.TargetName}, ({warps.Item2.TargetX}, {warps.Item2.TargetY})");
            }

            if (!Game1.player.craftingRecipes.ContainsKey("Cask")) {
                Game1.player.craftingRecipes.Add("Cask", 0);
            }
        }

        private Tuple<Warp, Warp> GetCellarToFarmHouseWarps(FarmHouse farmHouse)
        {
            GameLocation cellar = Game1.getLocationFromName(farmHouse.GetCellarName());

            // Get warp points. If these aren't available an exception is thrown.
            Point p1 = new Point(3, 1);
            Point p2 = new Point(4, 1);
            try {
                // ATTENTION: There are multiple variants of the string "FarmHouse"
                // in the code and the map that differ by case.
                Warp warp1 = cellar.warps.First(warp => {
                                return OrdinalIgnoreCase.Equals(warp.TargetName, "FarmHouse")
                                    && warp.X == p1.X
                                    && warp.Y == p1.Y;
                             });

                Warp warp2 = cellar.warps.First(warp => {
                                return OrdinalIgnoreCase.Equals(warp.TargetName, "FarmHouse")
                                    && warp.X == p2.X
                                    && warp.Y == p2.Y;
                             });

                return Tuple.Create(warp1, warp2);
            }
            catch {
                throw new Exception($"The cellar map doesn't have the required warp points at {p1} and {p2}.");
            }
        }
    }
}
