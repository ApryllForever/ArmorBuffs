using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore.Events;
using SpaceCore.Interface;
using SpaceShared;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.GameData.Characters;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Objects;
using xTile.Dimensions;
using Object = StardewValley.Object;

namespace ArmorBuffs
{
    internal class ModEntry : Mod
    {

        static bool isWearingIridiumArmour = false;

        public static ModEntry instance;

        public const float steamZoom = 4f;
        public const float steamYMotionPerMillisecond = 0.1f;
        private Texture2D steamAnimation;
        private Vector2 steamPosition;
        private float steamYOffset;
        public static bool BikiniFlirt = false;
        public static ModConfig Config;
        internal static IModHelper? Helpinator { get; set; }

        //public static int Width = instance.Helper.Reflection.GetField<int>(nameof(Item), "getDescriptionWidth").GetValue();

        private static Microsoft.Xna.Framework.Rectangle chaosSource = new Microsoft.Xna.Framework.Rectangle(640, 0, 64, 64);
        public static Vector2 chaosPos;

        public override void Entry(IModHelper helper)
        {

            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            Helpinator = helper;

            Config = Helpinator.ReadConfig<ModConfig>();

            var harmony = new Harmony(ModManifest.UniqueID);

            harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Item), nameof(Item.onEquip)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Item_onEquipPostfix))
            );
            harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Item), nameof(Item.AddEquipmentEffects)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Item_AddEquipmentEffectsPostfix))
            );
            harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Item), nameof(Item.drawTooltip)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Item_drawTooltipPostfix))
            );
            harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Boots), nameof(Boots.drawTooltip)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Boots_drawTooltipPostfix))
            );
            harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Item), nameof(Item.getExtraSpaceNeededForTooltipSpecialIcons)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Item_getExtraSpaceNeededForTooltipSpecialIconsPostfix))
            );

           // harmony.Patch(
         //  original: AccessTools.DeclaredMethod(typeof(Item), nameof(Item.drawTooltip)),
          // postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Item_drawTooltipPostfix))
          // );
           // harmony.Patch(
           // original: AccessTools.DeclaredMethod(typeof(Item), nameof(Item.getExtraSpaceNeededForTooltipSpecialIcons)),
           // postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Hat_getExtraSpaceNeededForTooltipSpecialIconsPostfix))
           // );

            harmony.Patch(
         original: AccessTools.DeclaredMethod(typeof(Boots), nameof(Boots.getExtraSpaceNeededForTooltipSpecialIcons)),
         postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Boots_getExtraSpaceNeededForTooltipSpecialIconsPostfix))
         );

            harmony.Patch(
           original: AccessTools.DeclaredMethod(typeof(BreakableContainer), nameof(BreakableContainer.releaseContents)),
           postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.BreakableContainer_releaseContentsPostfix))
           );

            harmony.Patch(
          original: AccessTools.DeclaredMethod(typeof(Object), nameof(Object.cutWeed)),
          postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Object_cutWeedPostfix))
          );


            /*
            harmony.Patch(
      original: AccessTools.DeclaredMethod(typeof(Item), nameof(Item.drawInMenu)),
      postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.Item_drawInMenuPostfix))
     );
            */


            harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.drawAboveAlwaysFrontLayer)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.GameLocation_drawAboveAlwaysFrontLayer_Postfix))
            );

            harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(NPC), nameof(NPC.grantConversationFriendship)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.NPC_grantConversationFriendshipPostfix))
            //postfix: new HarmonyMethod(typeof(ModEntry), nameof(ModEntry.NPC_checkActionPostfix))
            );


        }

        private void OnGameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Sets whether or not people flirt with you if you are wearing certain clothing.",
                getValue: () => Config.Flirt,
                setValue: value => Config.Flirt = value
            );
            configMenu.AddBoolOption(
               mod: ModManifest,
               name: () => "Sets only people who match your gender to flirt with you.",
               getValue: () => Config.SameSexFlirtOnly,
               setValue: value => Config.SameSexFlirtOnly = value
           );
        }

        private static void Object_cutWeedPostfix(Farmer who, Object __instance)
        {
            Random r = Utility.CreateRandom(__instance.TileLocation.X, (double)__instance.TileLocation.Y * 10000.0, Game1.stats.DaysPlayed, 1);
            int x = (int)__instance.TileLocation.X;
            int y = (int)__instance.TileLocation.Y;

            Item item = null;
            switch (r.Next(13))
            {
                case 0:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_UnrepentantCuirass");
                    break;
                case 1:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_CherryPetalCuirass");
                    break;
                case 2:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_IndigoSirenShirt");
                    break;
                case 3:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_MermaidActionVest"); //MermaidActionCorset 
                    break;
                case 4:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_InfidelCuirass"); //+ r.Next(1362, 1370));
                    break;
                case 5:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_ClamshellBra");
                    break;
                case 6:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_MermaidActionCorset");
                    break;
                case 7:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_ClamshellBra");
                    break;
                case 8:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_MermaidActionCorset");
                    break;
                case 9:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_IndigoSirenShirt");
                    break;
                case 10:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_MermaidActionVest"); //MermaidActionCorset 
                    break;
                case 11:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_ClamshellBra");
                    break;
                case 12:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_IndigoSirenShirt");
                    break;

            }
            if (item == null || item.Name.Contains("Error"))
            {
                item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_ClamshellBra");
            }

            if (Game1.random.NextDouble() <= 0.029)
            {
                Game1.createItemDebris(item, __instance.TileLocation, 1);

            }

            if (Game1.player.shirtItem.Value != null)
            {
                if (Game1.player.shirtItem.Value.ItemId.Equals("1034")) //Link Shirt
                {
                    if (Game1.random.NextDouble() <= 0.05)
                    {
                        if (Game1.random.NextDouble() <= 0.4)
                        {
                            Game1.playSound("cowboy_powerup");
                        }
                        Game1.createMultipleObjectDebris("(O)GoldCoin", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation);
                    }
                    if (Game1.random.NextDouble() <= 0.03)
                    {
                        if (Game1.random.NextDouble() <= 0.4)
                        {
                            Game1.playSound("cowboy_powerup");
                        }
                        Game1.createMultipleObjectDebris("(O)287", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation);
                    }
                    if (Game1.player.health <= 25)
                    {
                        if (Game1.random.NextDouble() <= 0.2)
                        {
                            if (Game1.random.NextDouble() <= 0.4)
                            {
                                Game1.playSound("cowboy_powerup");
                            }
                            Game1.createMultipleObjectDebris("(O)930", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Hearts
                        }
                    }
                }
                if (Game1.player.shirtItem.Value.ItemId.Equals("1242")) //Abigail Shirt!
                {
                    if (Game1.random.NextDouble() <= 0.05)
                    {
                        if (Game1.random.NextDouble() <= 0.4)
                        {
                            Game1.playSound("cowboy_powerup");
                        }
                        Game1.createMultipleObjectDebris("(O)GoldCoin", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation);
                    }
                    if (Game1.random.NextDouble() <= 0.03)
                    {
                        if (Game1.random.NextDouble() <= 0.4)
                        {
                            Game1.playSound("cowboy_powerup");
                        }
                        Game1.createMultipleObjectDebris("(O)287", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation);
                    }
                    if (Game1.player.health <= 25)
                    {
                        if (Game1.random.NextDouble() <= 0.2)
                        {
                            if (Game1.random.NextDouble() <= 0.4)
                            {
                                Game1.playSound("cowboy_powerup");
                            }
                            Game1.createMultipleObjectDebris("(O)930", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Hearts
                        }
                    }
                }
            }
        }



        private static void Item_onEquipPostfix(Farmer who, Item __instance)
        {
            if (__instance.QualifiedItemId.Equals("1151"))
            {
                //isWearingIridiumArmour = true;
            }
            if (__instance.QualifiedItemId.Equals("1151"))
            {
                //isWearingIridiumArmour = true;
            }
        }

        private static void BreakableContainer_releaseContentsPostfix(Farmer who, BreakableContainer __instance)
        {
            Random r = Utility.CreateRandom(__instance.TileLocation.X, (double)__instance.TileLocation.Y * 10000.0, Game1.stats.DaysPlayed, 1);
            int x = (int)__instance.TileLocation.X;
            int y = (int)__instance.TileLocation.Y;

            Item item = null;
            switch (r.Next(13))
            {
                case 0:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_UnrepentantCuirass");
                    break;
                case 1:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_CherryPetalCuirass");
                    break;
                case 2:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_IndigoSirenShirt");
                    break;
                case 3: 
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_MermaidActionVest"); //MermaidActionCorset 
                    break;
                case 4:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_InfidelCuirass"); //+ r.Next(1362, 1370));
                    break;
                case 5:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_ClamshellBra");
                    break;
                case 6:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_MermaidActionCorset");
                    break;
                case 7:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_ClamshellBra");
                    break;
                case 8:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_MermaidActionCorset");
                    break;
                case 9:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_IndigoSirenShirt");
                    break;
                case 10:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_MermaidActionVest"); //MermaidActionCorset 
                    break;
                case 11:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_ClamshellBra");
                    break;
                case 12:
                    item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_IndigoSirenShirt");
                    break;

            }
            if (item == null || item.Name.Contains("Error"))
            {
                item = ItemRegistry.Create("(S)ApryllForever.ArmorBuffs_ClamshellBra");
            }

            if (Game1.random.NextDouble() <= 0.039)
            {
                Game1.createItemDebris(item, __instance.TileLocation, 1);
            }


                if (Game1.player.shirtItem.Value != null)
            {

                if (Game1.player.shirtItem.Value.ItemId.Equals("1034")) //Link Shirt
                {
                     if (Game1.random.NextDouble() <= 0.05)
                    {
                    Game1.createMultipleObjectDebris("(O)GoldCoin", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation);
                    }
                    if (Game1.random.NextDouble() <= 0.03)
                    {
                        Game1.createMultipleObjectDebris("(O)287", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation);
                    }
                    if (Game1.random.NextDouble() <= 0.03)
                    {
                        Game1.createMultipleObjectDebris("(O)930", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Hearts
                    }
                    if(Game1.player.health <= 25)
                    {
                        if (Game1.random.NextDouble() <= 0.2)
                        {
                            Game1.createMultipleObjectDebris("(O)930", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Hearts
                        }
                    }
                }

                if (Game1.player.shirtItem.Value.ItemId.Equals("1242")) //Long Vest - Abigail Shirt!
                {
                    if (Game1.random.NextDouble() <= 0.05)
                    {
                        Game1.createMultipleObjectDebris("(O)GoldCoin", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation);
                    }
                    if (Game1.random.NextDouble() <= 0.03)
                    {
                        Game1.createMultipleObjectDebris("(O)287", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation);
                    }
                    if (Game1.random.NextDouble() <= 0.03)
                    {
                        Game1.createMultipleObjectDebris("(O)930", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Hearts
                    }
                    if (Game1.player.health <= 25)
                    {
                        if (Game1.random.NextDouble() <= 0.2)
                        {
                            Game1.createMultipleObjectDebris("(O)930", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Hearts
                        }
                    }
                }


                if (Game1.player.shirtItem.Value.ItemId.Equals("ApryllForever.ArmorBuffs_UnrepentantCuirass"))
                {
                    if (Game1.random.NextDouble() <= 0.08)
                    {
                        Game1.createMultipleObjectDebris("(O)773", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //LifeElixir
                    }

                    if (Game1.random.NextDouble() <= 0.05)
                    {
                        Game1.createMultipleObjectDebris("(O)879", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Monster Perfume
                    }
                    if (Game1.random.NextDouble() <= 0.05)
                    {
                        Game1.createMultipleObjectDebris("(O)243", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Lollypop
                    }
                    if (Game1.player.health <= 37)
                    {
                        {
                            Game1.createMultipleObjectDebris("(O)930", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Hearts
                        }
                    }
                }
                if (Game1.player.shirtItem.Value.ItemId.Equals("ApryllForever.ArmorBuffs_CherryPetalCuirass"))
                {

                    if (Game1.random.NextDouble() <= 0.08)
                    {
                        Game1.createMultipleObjectDebris("(O)638", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Cherry
                    }

                    if (Game1.random.NextDouble() <= 0.04)
                    {
                        Game1.createMultipleObjectDebris("(O)628", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Cherry Sapling
                    }
                    if (Game1.random.NextDouble() <= 0.05)
                    {
                        Game1.createMultipleObjectDebris("(O)288", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //bomb
                    }
                    if (Game1.player.health <= 37)
                    {
                        {
                            Item loot = ItemRegistry.Create("930");

                            Game1.createMultipleObjectDebris("(O)930", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Hearts
                            Game1.createItemDebris(loot, __instance.TileLocation,2,Game1.player.currentLocation);
                        }
                    }

                }
                if (Game1.player.shirtItem.Value.ItemId.Equals("ApryllForever.ArmorBuffs_InfidelCuirass"))
                {

                    if (Game1.random.NextDouble() <= 0.08)
                    {
                        Game1.createMultipleObjectDebris("(O)637", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Pommegranet
                    }

                    if (Game1.random.NextDouble() <= 0.0073)
                    {
                        Game1.createMultipleObjectDebris("(O)797", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Pearl
                    }
                    if (Game1.random.NextDouble() <= 0.05)
                    {
                        Game1.createMultipleObjectDebris("(O)288", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //bomb
                    }
                    if (Game1.player.health <= 37)
                    {
                        {
                            Game1.createMultipleObjectDebris("(O)930", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Hearts
                        }
                    }

                }


                if (Game1.player.shirtItem.Value.ItemId.Equals("1034")) //Kelp Shirt
            {
                if (Game1.random.NextDouble() <= 0.09)
                {
                    Game1.createMultipleObjectDebris("(O)139", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Salmon
                }
            }

            if (Game1.player.shirtItem.Value.ItemId.Equals("1046")) //Unnamed Purple Shirt
            {
                if (Game1.random.NextDouble() <= 0.05)
                {
                    Game1.createMultipleObjectDebris("(O)272", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Eggplant
                }
            }


            if (Game1.player.shirtItem.Value.ItemId.Equals("1028")) //Heart Shirt
            {
                if (Game1.random.NextDouble() <= 0.2)
                {
                    Game1.createMultipleObjectDebris("(O)930", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation); //Hearts

                       // ItemRegistry.Create("(O)930");
                }
            }

            if (Game1.player.shirtItem.Value.ItemId.Equals("1042")) //Lime Green Tunic
            {
                if (Game1.random.NextDouble() <= 0.05)
                {
                    Game1.createMultipleObjectDebris("(O)GoldCoin", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation);
                }
            }

           
                switch (Game1.player.shirtItem.Value.QualifiedItemId)
                {
                    case "(S)1049": //3 Unnamed Pink Shirts
                    case "(S)1050":
                    case "(S)1051":
                        if (Game1.random.NextDouble() <= 0.05)
                        {
                            Game1.createMultipleObjectDebris("(O)400", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation);
                        }
                        break;
                    case "(S)1048":
                        if (Game1.random.NextDouble() <= 0.05)
                        {
                            Game1.createMultipleObjectDebris("(O)400", x, y, r.Next(1, 3), who.UniqueMultiplayerID, Game1.currentLocation);
                        }
                        break;
                    
                }
            }
        }


        public static int getDescieWidthClothing()
        {
            Clothing clothing = new Clothing();

            int minimum_size;
            minimum_size = 272;
            if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.fr)
            {
                minimum_size = 384;
            }
            if (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.tr)
            {
                minimum_size = 336;
            }
            return Math.Max(minimum_size, (int)Game1.dialogueFont.MeasureString((clothing.DisplayName == null) ? "" : clothing.DisplayName).X);
        }


        private static void Boots_drawTooltipPostfix(SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText, Item __instance)
        {
            ParsedItemData itemData;
            itemData = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId);
            string DisplayNameFuckIt = itemData.DisplayName;
            string descriptionFuckIt = itemData.Description;

            if (__instance.QualifiedItemId.Equals("(B)806")) //Leprechaun Shoes
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Luck, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(B)854")) //Mermaid Shoes
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+4 Attack, +1 Luck, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)855")) //Dragon Shoes
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack, +2 Luck, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)853")) //CinderClown Shoes
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Attack, +2 Luck, +2 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }


            if (__instance.QualifiedItemId.Equals("(B)504")) //Sneakers
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)505")) //Rubber Boots
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+0.5 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)506")) //Leather Boots
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+0.5 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)507")) //Work Boots
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+0.5 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)509")) //Tundra Boots
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+0.5 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)510))")) //Therml Boots
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+0.5 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(B)508")) //Combat Boots
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)511")) //Dark Boots
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+0.5 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)512")) //Firewalker Boots
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+0.5 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)514")) //Space Boots
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+0.5 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)513")) //Genie Boots
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Speed, +1 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)515")) //CowHuman Boots
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Farming, +1 Foraging", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)804")) //Emily Boots
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Speed, +2 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(B)878")) //Crystal Shoes
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Attack, +2 Speed, +2 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
        }


        private static void Item_drawTooltipPostfix(SpriteBatch spriteBatch, ref int x, ref int y, SpriteFont font, float alpha, StringBuilder overrideText, Item __instance)
        {
            ParsedItemData itemData;
            itemData = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId);
            string DisplayNameFuckIt = itemData.DisplayName;
            string descriptionFuckIt = itemData.Description;

            //Vanilla Armours!!!
            //
            if (__instance.QualifiedItemId.Equals("(S)1151")) //Iridium Breastplate
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+19 Defense, -0.5 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
                // Utility.drawWithShadow(spriteBatch, Game1.mouseCursors, new Vector2(x + 16 + 4, y + 16 + 4), new Rectangle(110, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, flipped: false, 1f);
            }
            if (__instance.QualifiedItemId.Equals("(S)1150")) //Gold Breastplate
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+13 Defense, -0.5 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1149")) //Copper Breastplate
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+7 Defense, -0.5 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1148")) // Steel Breastplate
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+11 Defense, -0.5 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            //Specialty
            //
            //

            if (__instance.QualifiedItemId.Equals("(S)1265")) //Bridal Top
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Luck, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(S)1171")) //Oasis Gown - Fortuna favors those who wear Her wear!
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Luck, +1 Speed, +1 Crit Chance", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1297")) //Island Bikini - This is a loving mockery of all the million games who feature women in bikinis going to battle while men wear full plate...
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack, +1 Crit Chance, +1 Luck, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1242")) // Blue Long Vest - The Abigail Shirt!
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack, +1 Crit Chance, +1 Luck, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1258")) // Spring Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Foraging, +2 Farming, +1 Luck, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(S)1239")) // Captain Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Attack, +1 Crit Chance, +2 Luck, +2 Fishing", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1240")) // Officer Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Attack, +2 Crit Chance, +2 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1241")) // Ranger Shirt
            {

                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Attack, +2 Crit Chance, +2 Luck, +2 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
              
            }

            //Fishing
            //
            if (__instance.QualifiedItemId.Equals("(S)1157")) //Fishing Vest
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Fishing", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(S)1158")) //Fishing Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Fishing", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;

            }
            if (__instance.QualifiedItemId.Equals("(S)1168")) //Kelp Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Fishing", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;

            }
            if (__instance.QualifiedItemId.Equals("(S)1193")) //Ocean Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Fishing", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;

            }

            //Attack
            //
            if (__instance.QualifiedItemId.Equals("(S)1165")) //White Gi
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1166")) //Orange Gi
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1169")) //Studded Vest 
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

                //Speed
            if (__instance.QualifiedItemId.Equals("(S)1164")) //Tracksuit Jack!!!! 
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            //Farming
           //
            if (__instance.QualifiedItemId.Equals("(S)1195")) //Bandana Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1203")) //Bandana Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1206")) //Bandana Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1262")) //Dark Bandana Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            //Foraging
            //
            if (__instance.QualifiedItemId.Equals("(S)1257")) //Morel Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Foraging", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1274")) //Camo Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Foraging", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1187")) //Leafy Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Foraging", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

                //Overalls............
            if (__instance.QualifiedItemId.Equals("(S)1000")) //Basic Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1007")) //Green Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(S)1266")) //Brown Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(S)1268")) //White Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(S)1067")) //Daffodil Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(S)1068")) //Unnamed Green Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1074")) //Unnamed red Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1088")) //Unnamed CORN Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1292")) //Ginger Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1079")) //Unnamed blue Overalls 
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1099")) //Unnamed purple Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1103")) //Unnamed black Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1216")) //Plain Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1217")) //Sleveless Overalls
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            //Mining
            //
            if (__instance.QualifiedItemId.Equals("(S)1285")) //Midnight Dog Jacket
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Fishing", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1290")) //Mineral Dog Jacket
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Mining", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1155")) //Bomber Jacket
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Mining", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)1208")) //Excavator Shirt
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Mining", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }


            //Mermaid Clothes!!!!!!!!!!!!!!

            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_UnrepentantCuirass"))
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack, +1 Speed, +4 Defense", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_CherryPetalCuirass"))
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack, +1 Speed, +4 Defense", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_InfidelCuirass"))
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack, +1 Speed, +4 Defense", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_ClamshellBra")) //Mermaid Shell Top
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack, +1 Crit Chance, +1 Luck, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_IndigoSirenShirt"))
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Speed, +3 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_MermaidActionVest"))
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_MermaidActionCorset"))
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }


            //Hats!!!

            if (__instance.QualifiedItemId.Equals("(H)4")) //Straw Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)5")) //Cop Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)7")) //Plum Chapeau
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Foraging, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)19")) //Fedora
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Attack, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)21")) //LuckyBow
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)26")) //Tiara
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)27")) //Hard Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Mining", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)28")) //Souwester
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Fishing", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)30")) //Watermelon Band
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming, +1 Speed", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)33")) //CowGal
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Farming, +1 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)34")) //CowPoke
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Farming, +1 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)38")) //CowRed
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming, +1 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)37")) //CowBlue
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Farming, +1 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)47")) //Fashion Hat - Zorra Hat!
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Luck, +3 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)50")) //Knight 
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+4 Defense, +2 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)51")) //Squire
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+1 Attack, +2 Defense", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)55")) //Fishing Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Fishing", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)62")) //Pirate Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)66")) //Garbage Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+4 Attack, +2 Speed, +2 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y; //-8 Charisma
            }
            if (__instance.QualifiedItemId.Equals("(H)69")) //Bridal Veil
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+2 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)70")) //Witch Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+4 Attack, +2 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)73")) //Magic Cowboy Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Farming, +2 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)75")) //Golden Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

            if (__instance.QualifiedItemId.Equals("(H)76")) //Deluxe Pirate Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack, +1 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)81")) //Deluxe CowPerson Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Farming, +1 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)83")) //Deluxe CowPerson Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Farming, +1 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)85")) //Swashbuckler Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)90")) //Forager Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Foraging", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)91")) //Tiger Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)93")) //Warrior Hat
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)AbigailsBow")) //Abigail's Bow
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)TricornHat")) //Tricorn
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)BlueBow")) //
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)DarkVelvetBow")) //
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)WhiteBow")) //
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Attack", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)LeprechuanHat")) //
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Luck", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }
            if (__instance.QualifiedItemId.Equals("(H)JunimoHat")) //
            {
                Utility.drawTextWithShadow(spriteBatch, Game1.parseText("+3 Foraging", Game1.smallFont, getDescieWidthClothing()), font, new Vector2(x + 16, y + 32 + 4), Game1.textColor);
                y += (int)font.MeasureString(Game1.parseText(descriptionFuckIt, Game1.smallFont, getDescieWidthClothing())).Y;
            }

        }


        private static void Item_getExtraSpaceNeededForTooltipSpecialIconsPostfix(SpriteFont font, int minWidth, int horizontalBuffer, int startingHeight, StringBuilder descriptionText, string boldTitleText, int moneyAmountToDisplayAtBottom, Item __instance, ref Point __result)
        {

            Point dimensions;
            dimensions = new Point(0, startingHeight);
            int extra_rows_needed;
            extra_rows_needed = 0;

            if (__instance.QualifiedItemId.Equals("(S)1151"))
            {
                extra_rows_needed++;
                extra_rows_needed++;
                
            }
            if (__instance.QualifiedItemId.Equals("(S)1150"))
            {
                extra_rows_needed++;
                extra_rows_needed++;
            }
            if (__instance.QualifiedItemId.Equals("(S)1149"))
            {
                extra_rows_needed++;
            }
            if (__instance.QualifiedItemId.Equals("(S)1148"))
            {
                extra_rows_needed++;
                extra_rows_needed++;
            }


            {
                switch (__instance.QualifiedItemId)
                {
                    //Specialty
                    //
                    case "(S)1171": //Oasis Gown
                    case "(S)1242": //Abigail Shirt!
                    case "(S)1258": //Oasis Gown
                    case "(S)1297": //Tropical Bikini
                    case "(S)1239": //Captain Shirt
                    case "(S)1240": //Cop Shirt 
                    case "(S)1241": //Ranger Shirt


                        extra_rows_needed++;
                        extra_rows_needed++;
                        break;

                    case "(S)1265": //Bridal Top
                        extra_rows_needed++;
                        break;

                        //fishing
                    case "(S)1158":
                    case "(S)1159":
                    case "(S)1168":
                    case "(S)1193":
                        extra_rows_needed++;
                        break;
                        //Attack
                    case "(S)1165":
                    case "(S)1166":
                    case "(S)1169":
                        //Speed
                    case "(S)1164":
                        extra_rows_needed++;
                        break;
                    //Farming
                    case "(S)1195":
                    case "(S)1203":
                    case "(S)1206":
                    case "(S)1262":
                    //Foraging
                    case "(S)1257":
                    case "(S)1274":
                    case "(S)1187":
                    //Overalls..... .... ... ...
                    case "(S)1000":
                    case "(S)1007":
                    case "(S)1067":
                    case "(S)1068":
                    case "(S)1074":
                    case "(S)1079":
                    case "(S)1088":
                    case "(S)1099":
                    case "(S)1103":
                    case "(S)1216":
                    case "(S)1217":
                    case "(S)1266":
                    case "(S)1268":
                    case "(S)1292":
                    //Mining...
                    case "(S)1285":
                    case "(S)1290":
                    case "(S)1208":
                    case "(S)1155":
                        extra_rows_needed++;

                        break;


                    //Mermaid Clothes!!!
                    case "(S)ApryllForever.ArmorBuffs_UnrepentantCuirass":
                    case "(S)ApryllForever.ArmorBuffs_CherryPetalCuirass":
                    case "(S)ApryllForever.ArmorBuffs_InfidelCuirass":
                    case "(S)ApryllForever.ArmorBuffs_ClamshellBra":
                    case "(S)ApryllForever.ArmorBuffs_IndigoSirenShirt":
                    case "(S)ApryllForever.ArmorBuffs_MermaidActionVest":
                    case "(S)ApryllForever.ArmorBuffs_MermaidActionCorset":

                        extra_rows_needed++;
                        extra_rows_needed++;
                        break;
                }
                if (__instance.QualifiedItemId.Equals("(H)5"))  //Straw Hat
                {
                    extra_rows_needed++;
                }

                if (__instance.QualifiedItemId.Equals("(H)30")) //Watermelon Band
                {
                    extra_rows_needed++;
                }
                switch (__instance.QualifiedItemId)
                {
                    case "(H)4":
                    case "(H)21":                  
                    case "(H)27":
                    case "(H)28":                   
                    case "(H)55":
                    case "(H)62":
                    case "(H)69":                   
                    case "(H)75":                 
                    case "(H)85":
                    case "(H)90":
                    case "(H)91":
                    case "(H)93":
                    case "(H)AbigailsBow":
                    case "(H)TricornHat":
                    case "(H)BlueBow":
                    case "(H)DarkVelvetBow":
                    case "(H)WhiteBow":
                    case "(H)LeprechuanHat":
                    case "(H)JunimoHat":
                        extra_rows_needed++;
                        break;
                    case "(H)7":
                    case "(H)19":
                    case "(H)26":
                    case "(H)30":
                    case "(H)33":
                    case "(H)34":
                    case "(H)37":
                    case "(H)38":
                    case "(H)47":
                    case "(H)50":
                    case "(H)51":
                    case "(H)66":
                    case "(H)70":
                    case "(H)73":
                    case "(H)76":
                    case "(H)81":
                    case "(H)83":
                        extra_rows_needed++;
                        extra_rows_needed++;
                        break;

                }
            }

            dimensions.X = (int)Math.Max(minWidth, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", 9999)).X + (float)horizontalBuffer);
            dimensions.Y += extra_rows_needed * Math.Max((int)font.MeasureString("TT").Y, 48);
            __result = dimensions;


        }

        private static void Boots_getExtraSpaceNeededForTooltipSpecialIconsPostfix(SpriteFont font, int minWidth, int horizontalBuffer, int startingHeight, StringBuilder descriptionText, string boldTitleText, int moneyAmountToDisplayAtBottom, Boots __instance, ref Point __result)
        {

            Point dimensions;
            dimensions = new Point(0, startingHeight);
            int extra_rows_needed;
            extra_rows_needed = 0;
         
            if (__instance.QualifiedItemId.Equals("(B)854") || __instance.QualifiedItemId.Equals("(B)853") ||__instance.QualifiedItemId.Equals("(B)855") || __instance.QualifiedItemId.Equals("(B)804") || __instance.QualifiedItemId.Equals("(B)878"))  //Mermaid Shoes
            {
                extra_rows_needed++;
                extra_rows_needed++;

                if(Game1.player.stats.Get("Book_PriceCatalogue") != 0) //Book_PriceCatalogue
                {
                    extra_rows_needed++;
                    extra_rows_needed++;
                }

                dimensions.X = (int)Math.Max(minWidth, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", 9999)).X + (float)horizontalBuffer);
                dimensions.Y += extra_rows_needed * Math.Max((int)font.MeasureString("TT").Y, 48);
                __result = dimensions;
            }

            if (__instance.QualifiedItemId.Equals("(B)806") || __instance.QualifiedItemId.Equals("(B)504") || __instance.QualifiedItemId.Equals("(B)505") || __instance.QualifiedItemId.Equals("(B)506") || __instance.QualifiedItemId.Equals("(B)507") || __instance.QualifiedItemId.Equals("(B)508") || __instance.QualifiedItemId.Equals("(B)509") || __instance.QualifiedItemId.Equals("(B)510") || __instance.QualifiedItemId.Equals("(B)511")|| __instance.QualifiedItemId.Equals("(B)512")|| __instance.QualifiedItemId.Equals("(B)513")|| __instance.QualifiedItemId.Equals("(B)514")|| __instance.QualifiedItemId.Equals("(B)515")) //Leprechaun Shoes
            {
                extra_rows_needed++;

                if (Game1.player.stats.Get("Book_PriceCatalogue") != 0) //Book_PriceCatalogue
                {
                    extra_rows_needed++;
                }

                dimensions.X = (int)Math.Max(minWidth, font.MeasureString(Game1.content.LoadString("Strings\\UI:ItemHover_DefenseBonus", 9999)).X + (float)horizontalBuffer);
                dimensions.Y += extra_rows_needed * Math.Max((int)font.MeasureString("TT").Y, 48);
                __result = dimensions;
            }
        }




        /* private static void Item_drawInMenuPostfix(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow, Item __instance)
         {

             __instance.AdjustMenuDrawForRecipes(ref transparency, ref scaleSize);
             ParsedItemData itemData;
             itemData = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId);
             spriteBatch.Draw(itemData.GetTexture(), location + new Vector2(32f, 32f) * scaleSize, itemData.GetSourceRect(), color * transparency, 0f, new Vector2(8f, 8f) * scaleSize, scaleSize * 4f, SpriteEffects.None, layerDepth);
             __instance.DrawMenuIcons(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color);

         }*/

        private static void Item_AddEquipmentEffectsPostfix(BuffEffects effects, Item __instance)
        {
            //Shoes
            //3333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333
            //
            if (__instance.QualifiedItemId.Equals("(B)854")) //Mermaid Shoes
            {
                effects.Speed.Value += 4f;
                effects.LuckLevel.Value += 1f;
                effects.AttackMultiplier.Value += 2f;
            }

            if (__instance.QualifiedItemId.Equals("(B)806")) //Leprechaun Shoes
            {
                effects.Speed.Value += 1f;
                effects.LuckLevel.Value += 1f;
            }

            if (__instance.QualifiedItemId.Equals("(B)855")) //Dragon Shoes
            {
                effects.Speed.Value += 3f;
                effects.LuckLevel.Value += 2f;
                effects.AttackMultiplier.Value += 2f;
            }
            if (__instance.QualifiedItemId.Equals("(B)853")) //CinderClown Shoes
            {
                effects.Speed.Value += 2f;
                effects.LuckLevel.Value += 2f;
                effects.AttackMultiplier.Value += 2f;
            }
            if (__instance.QualifiedItemId.Equals("(B)504")) //Sneakers
            {
                effects.Speed.Value += 2f;
            }
            if (__instance.QualifiedItemId.Equals("(B)505")) //Rubber Boots
            {
                effects.Speed.Value += 0.5f;
            }
            if (__instance.QualifiedItemId.Equals("(B)506")) //Leather Boots
            {
                effects.Speed.Value += 0.5f;
            }
            if (__instance.QualifiedItemId.Equals("(B)507")) //Work Boots
            {
                effects.Speed.Value += 0.5f;
            }
            if (__instance.QualifiedItemId.Equals("(B)509")) //Tundra Boots
            {
                effects.Speed.Value += 0.5f;
            }
            if (__instance.QualifiedItemId.Equals("(B)510))")) //Therml Boots
            {
                effects.Speed.Value += 0.5f;
            }

            if (__instance.QualifiedItemId.Equals("(B)508")) //Combat Boots
            {
                effects.Speed.Value += 1f;
            }
            if (__instance.QualifiedItemId.Equals("(B)511")) //Dark Boots
            {
                effects.Speed.Value += 0.5f;
            }
            if (__instance.QualifiedItemId.Equals("(B)512")) //Firewalker Boots
            {
                effects.Speed.Value += 0.5f;
            }
            if (__instance.QualifiedItemId.Equals("(B)514")) //Space Boots
            {
                effects.Speed.Value += 0.5f;
            }
            if (__instance.QualifiedItemId.Equals("(B)513")) //Genie Boots
            {
                effects.Speed.Value += 1f;
                effects.LuckLevel.Value += 1f;
            }
            if (__instance.QualifiedItemId.Equals("(B)515")) //CowHuman Boots
            {
                effects.FarmingLevel.Value += 1f;
                effects.ForagingLevel.Value += 1f; ;
            }
            if (__instance.QualifiedItemId.Equals("(B)804")) //Emily Boots
            {
                effects.Speed.Value += 1f;
                effects.LuckLevel.Value += 2f;
            }
            if (__instance.QualifiedItemId.Equals("(B)878")) //Crystal Shoes
            {
                effects.Speed.Value += 2f;
                effects.LuckLevel.Value += 2f;
                effects.AttackMultiplier.Value += 2f;
            }



            //Tops
            //#################################################################################################

            // Armours
            //
            if (__instance.QualifiedItemId.Equals("(S)1148")) //Steel Brest Plate
            {
                effects.Defense.Value += 11;
                effects.Speed.Value -= .5f;
            }
            if (__instance.QualifiedItemId.Equals("(S)1149")) //Copper Breast
            {
                effects.Defense.Value += 7;
                effects.Speed.Value -= .5f;
            }
            if (__instance.QualifiedItemId.Equals("(S)1150")) //Gold Breast Plate
            {
                effects.Defense.Value += 13;
                effects.Speed.Value -= .5f;
            }
            if (__instance.QualifiedItemId.Equals("(S)1151")) //Iridium Breast Plate
            {
                effects.Defense.Value += 19;
                effects.Speed.Value -= .5f;
            }

            //Fishing
            if (__instance.QualifiedItemId.Equals("(S)1157")) //Fishing Vest
            {
                effects.FishingLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(S)1158")) //Fishing Shirt
            {
                effects.FishingLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1168")) //Kelp Shirt
            {
                effects.FishingLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(S)1193")) //Ocean Shirt
            {
                effects.FishingLevel.Value += 1;
            }


            //Attack
            //
            if (__instance.QualifiedItemId.Equals("(S)1165")) //White Gi
            {
                effects.AttackMultiplier.Value += 0.3f;
            }
            if (__instance.QualifiedItemId.Equals("(S)1166")) //Orange Gi
            {
                effects.AttackMultiplier.Value += 0.3f;
            }
            if (__instance.QualifiedItemId.Equals("(S)1169")) //Studded Vest 
            {
                effects.AttackMultiplier.Value += 0.3f;
            }

            //Speed
            //
            if (__instance.QualifiedItemId.Equals("(S)1164")) //Track Jacket
            {
                effects.Speed.Value += 1f;
            }
     
            //Specialty
            //
            if (__instance.QualifiedItemId.Equals("(S)1171")) //Oasis Gown - Fortuna favors those who wear Her wear!
            {
                //effects.AttackMultiplier.Value += 0.1f;
                effects.LuckLevel.Value += 3;
                effects.CriticalChanceMultiplier.Value += 0.1f;
                effects.Speed.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1265")) //Bridal Top
            {
                effects.Speed.Value += 1f;
                effects.LuckLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1297")) //Island Bikini - This is a loving mockery of all the million games who feature women in bikinis going to battle while men wear full plate...
            {
                effects.Speed.Value += 1f;
                effects.AttackMultiplier.Value += 0.3f;
                effects.CriticalChanceMultiplier.Value += 0.1f;
                effects.LuckLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1242")) // Blue Long Vest - The Abigail Shirt!
            {
                effects.Speed.Value += 1f;
                effects.AttackMultiplier.Value += 0.3f;
                effects.CriticalChanceMultiplier.Value += 0.1f;
                effects.LuckLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1258")) // Spring Shirt
            {
                effects.Speed.Value += 1f;
                effects.ForagingLevel.Value += 2;
                effects.LuckLevel.Value += 1;
                effects.FarmingLevel.Value += 2;
            }


            if (__instance.QualifiedItemId.Equals("(S)1239")) // Captain Shirt
            {
                effects.AttackMultiplier.Value += 0.1f;
                effects.CriticalChanceMultiplier.Value += 0.1f;
                effects.LuckLevel.Value += 2;
                effects.FishingLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(S)1240")) // Officer Shirt
            {
                effects.AttackMultiplier.Value += 0.2f;
                effects.CriticalChanceMultiplier.Value += 0.2f;
                effects.LuckLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(S)1241")) // Ranger Shirt
            {
                effects.AttackMultiplier.Value += 0.2f;
                effects.CriticalChanceMultiplier.Value += 0.2f;
                effects.LuckLevel.Value += 2;
                effects.ForagingLevel.Value += 2;
            }


            //Farming
            if (__instance.QualifiedItemId.Equals("(S)1187")) //Leafy Shirt
            {
                effects.ForagingLevel.Value += 2;
            }
           
            if (__instance.QualifiedItemId.Equals("(S)1195")) //Bandana Shirt
            {
                effects.FarmingLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1203")) //Bandana Shirt
            {
                effects.FarmingLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1206")) //Bandana Shirt
            {
                effects.FarmingLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1262")) //Dark Bandana Shirt
            {
                effects.FarmingLevel.Value += 1;
            }


            //Foraging
            //
            if (__instance.QualifiedItemId.Equals("(S)1257")) //Morel Shirt
            {
                effects.ForagingLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1274")) //Camo Shirt
            {
                effects.ForagingLevel.Value += 1;
            }





            //Overalls (there are many)

            if (__instance.QualifiedItemId.Equals("(S)1000")) //Basic Overalls
            {
                effects.FarmingLevel.Value += 1;
            }


            if (__instance.QualifiedItemId.Equals("(S)1007")) //Green Overalls
            {
                effects.FarmingLevel.Value += 2;
            }

            if (__instance.QualifiedItemId.Equals("(S)1266")) //Brown Overalls
            {
                effects.FarmingLevel.Value += 2;
            }

            if (__instance.QualifiedItemId.Equals("(S)1268")) //White Overalls
            {
                effects.FarmingLevel.Value += 2;
            }

            if (__instance.QualifiedItemId.Equals("(S)1067")) //Daffodil Overalls
            {
                effects.FarmingLevel.Value += 2;
            }

            if (__instance.QualifiedItemId.Equals("(S)1068")) //Unnamed Green Overalls
            {
                effects.FarmingLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(S)1074")) //Unnamed red Overalls
            {
                effects.FarmingLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(S)1088")) //Unnamed CORN Overalls
            {
                effects.FarmingLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(S)1292")) //Ginger Overalls
            {
                effects.FarmingLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(S)1079")) //Unnamed blue Overalls 
            {
                effects.FarmingLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(S)1099")) //Unnamed purple Overalls
            {
                effects.FarmingLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(S)1103")) //Unnamed black Overalls
            {
                effects.FarmingLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(S)1216")) //Plain Overalls
            {
                effects.FarmingLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1217")) //Sleveless Overalls
            {
                effects.FarmingLevel.Value += 2;
            }



            //Mining
            //
            if (__instance.QualifiedItemId.Equals("(S)1285")) //Midnight Dog Jacket
            {
                effects.FishingLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1290")) //Mineral Dog Jacket
            {
                effects.MiningLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1155")) //Bomber Jacket
            {
                effects.MiningLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)1208")) //Excavator Shirt
            {
                effects.MiningLevel.Value += 1;
            }          
            if (__instance.QualifiedItemId.Equals("(S)1219")) //Excavator Shirt
            {
                effects.MiningLevel.Value += 2;
                effects.FarmingLevel.Value -= 2;
                effects.ForagingLevel.Value -= 2;
                effects.Defense.Value += 2;
            }
           //if (__instance.QualifiedItemId.Equals("(H)79")) //Excavator Shirt 
            //{
             //   effects.FarmingLevel.Value -= 4;
             //   effects.ForagingLevel.Value -= 4;
             //   effects.Defense.Value -= 4;
             //   effects.AttackMultiplier.Value -= 0.4f;
             //   effects.LuckLevel.Value -= 3;
           // }


            // And now.... For my splash in the pond!
            //
            //
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_UnrepentantCuirass"))      {
                effects.Speed.Value += 1f;
                effects.AttackMultiplier.Value += 0.3f;
                effects.Defense.Value += 4f;
            }
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_CherryPetalCuirass"))
            {
                effects.Speed.Value += 1f;
                effects.AttackMultiplier.Value += 0.3f;
                effects.Defense.Value += 4f;
            }
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_InfidelCuirass"))
            {
                effects.Speed.Value += 1f;
                effects.AttackMultiplier.Value += 0.3f;
                effects.Defense.Value += 4f;
            }
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_ClamshellBra")) //Mermaid Shell Top
            {
                effects.Speed.Value += 1f;
                effects.AttackMultiplier.Value += 0.3f;
                effects.CriticalChanceMultiplier.Value += 0.1f;
                effects.LuckLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_IndigoSirenShirt"))
            {
                effects.Speed.Value += 1f;
                effects.LuckLevel.Value += 3f;
            }
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_MermaidActionVest"))
            {
                effects.Speed.Value += 1f;
                effects.AttackMultiplier.Value += 0.3f;
            }
            if (__instance.QualifiedItemId.Equals("(S)ApryllForever.ArmorBuffs_MermaidActionCorset"))
            {
                effects.Speed.Value += 1f;
                effects.AttackMultiplier.Value += 0.3f;
            }

            //Hat!!!!!! You should have seen the weird bug I somehow caused!

            if (__instance.QualifiedItemId.Equals("(H)4")) //Straw Hat
            {
             effects.FarmingLevel.Value += 2f;
            }

            if (__instance.QualifiedItemId.Equals("(H)5")) //Cop Hat
            {
                effects.AttackMultiplier.Value += 0.1f;
            }

            if (__instance.QualifiedItemId.Equals("(H)7")) //Plum Chapeau
            {
                effects.Speed.Value += 1f;
                effects.ForagingLevel.Value += 1f;
            }

            if (__instance.QualifiedItemId.Equals("(H)19")) //Fedora
            {
                effects.Speed.Value += 1f;
                effects.AttackMultiplier.Value += 0.1f;
            }

            if (__instance.QualifiedItemId.Equals("(H)21")) //LuckyBow
            {
                effects.LuckLevel.Value += 3;
            }

            if (__instance.QualifiedItemId.Equals("(H)26")) //Tiara
            {
                effects.Speed.Value += 1f;
                effects.AttackMultiplier.Value += 0.3f;
            }
            if (__instance.QualifiedItemId.Equals("(H)27")) //Hard Hat
            {
                effects.MiningLevel.Value += 3;
            }
            if (__instance.QualifiedItemId.Equals("(H)28")) //Souwester
            {
                effects.FishingLevel.Value += 3;
            }

            if (__instance.QualifiedItemId.Equals("(H)30")) //Watermelon Band
            {
                effects.Speed.Value += 1f;
                effects.FarmingLevel.Value += 1f;
            }

            if (__instance.QualifiedItemId.Equals("(H)33")) //CowGal
            {
                effects.FarmingLevel.Value += 3f;
                effects.AttackMultiplier.Value += 0.3f;
            }

            if (__instance.QualifiedItemId.Equals("(H)34")) //CowPoke
            {
                effects.FarmingLevel.Value += 3f;
                effects.AttackMultiplier.Value += 0.1f;
            }

            if (__instance.QualifiedItemId.Equals("(H)38")) //CowRed
            {
                effects.FarmingLevel.Value += 3f;
                effects.AttackMultiplier.Value += 0.1f;
            }

            if (__instance.QualifiedItemId.Equals("(H)37")) //CowBlue
            {
                effects.FarmingLevel.Value += 3f;
                effects.AttackMultiplier.Value += 0.1f;
            }

            if (__instance.QualifiedItemId.Equals("(H)47")) //Fashion Hat - Zorra Hat!
            {
                effects.LuckLevel.Value += 1f;
                effects.AttackMultiplier.Value += 0.3f;
            }

            if (__instance.QualifiedItemId.Equals("(H)50")) //Knight 
            {
                effects.AttackMultiplier.Value += 0.2f;
                effects.Defense.Value += 4;
            }

            if (__instance.QualifiedItemId.Equals("(H)51")) //Squire
            {
                effects.AttackMultiplier.Value += 0.1f;
                effects.Defense.Value += 2;
            }

            if (__instance.QualifiedItemId.Equals("(H)55")) //Fishing Hat
            {
                effects.FishingLevel.Value += 2;
            }

            if (__instance.QualifiedItemId.Equals("(H)62")) //Pirate Hat
            {
                effects.AttackMultiplier.Value += 0.2f;
            }
            if (__instance.QualifiedItemId.Equals("(H)66")) //Garbage Hat
            {
                effects.AttackMultiplier.Value += 0.4f;
                effects.Speed.Value += 2;
                effects.LuckLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(H)69")) //Bridal Veil
            {
                effects.LuckLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(H)70")) //Witch Hat
            {
                effects.AttackMultiplier.Value += 0.4f;
                effects.LuckLevel.Value += 2;
            }
            if (__instance.QualifiedItemId.Equals("(H)73")) //Magic Cowboy Hat
            {
                effects.LuckLevel.Value += 2f;
                effects.FarmingLevel.Value += 3f;
            }
            if (__instance.QualifiedItemId.Equals("(H)75")) //Golden Hat
            {
                effects.LuckLevel.Value += 3f;
            }

            if (__instance.QualifiedItemId.Equals("(H)76")) //Deluxe Pirate Hat
            {
                effects.AttackMultiplier.Value += 0.3f;
                effects.LuckLevel.Value += 1;
            }
            if (__instance.QualifiedItemId.Equals("(H)81")) //Deluxe CowPerson Hat
            {
                effects.LuckLevel.Value += 1f;
                effects.FarmingLevel.Value += 3f;
            }
            if (__instance.QualifiedItemId.Equals("(H)83")) //Dark CowPerson Hat
            {
                effects.LuckLevel.Value += 1f;
                effects.FarmingLevel.Value += 3f;
            }
            if (__instance.QualifiedItemId.Equals("(H)85")) //Swashbuckler Hat
            {
                effects.AttackMultiplier.Value += 0.3f;
            }
            if (__instance.QualifiedItemId.Equals("(H)90")) //Forager Hat
            {
                effects.ForagingLevel.Value += 3;
            }
            if (__instance.QualifiedItemId.Equals("(H)91")) //Tiger Hat
            {
                effects.AttackMultiplier.Value += 0.3f;
            }
            if (__instance.QualifiedItemId.Equals("(H)93")) //Warrior Hat
            {
                effects.AttackMultiplier.Value += 0.3f;
            }
            if (__instance.QualifiedItemId.Equals("(H)AbigailsBow")) //Abigail's Bow
            {
                effects.AttackMultiplier.Value += 0.3f;
            }
            if (__instance.QualifiedItemId.Equals("(H)TricornHat")) //Tricorn
            {
                effects.AttackMultiplier.Value += 0.3f;
            }
            if (__instance.QualifiedItemId.Equals("(H)BlueBow")) //
            {
                effects.AttackMultiplier.Value += 0.3f;
            }
            if (__instance.QualifiedItemId.Equals("(H)DarkVelvetBow")) //
            {
                effects.AttackMultiplier.Value += 0.3f;
            }
            if (__instance.QualifiedItemId.Equals("(H)WhiteBow")) //
            {
                effects.AttackMultiplier.Value += 0.3f;
            }
            if (__instance.QualifiedItemId.Equals("(H)LeprechuanHat")) //
            {
                effects.LuckLevel.Value += 3;
            }
            if (__instance.QualifiedItemId.Equals("(H)JunimoHat")) //
            {
                effects.ForagingLevel.Value += 3;
            }


        }

        //public static string dialogueBikini = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.1";

        public static string dialogueBikini()
        {
            string bikini = string.Empty; 

            switch (Game1.random.Next(4))
            {
                case 0:
                   // bikini = Game1.content.LoadString("Strings\\StringsFromCSFiles:MermaidArmourBuffs.1");
                    bikini = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.1";
                    break;
                case 1:
                   // bikini = Game1.content.LoadString("Strings\\StringsFromCSFiles:MermaidArmourBuffs.2");
                    bikini = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.2";
                    break;
                case 2:
                   // bikini = Game1.content.LoadString("Strings\\StringsFromCSFiles: MermaidArmourBuffs.3");
                    bikini = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.3";
                    break;
                case 3:
                   // bikini = Game1.content.LoadString("Strings\\StringsFromCSFiles: MermaidArmourBuffs.4");
                    bikini = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.4";
                    break;


            }


            return bikini;
        }

        public static string dialogueBridal()
        {
            string bridal = string.Empty;

            switch (Game1.random.Next(4))
            {
                case 0:
                    bridal = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.5";
                    break;
                case 1:
                    bridal = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.6";
                    break;
                case 2:
                    bridal = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.7";
                    break;
                case 3:
                    bridal = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.8";
                    break;


            }


            return bridal;
        }
        public static string dialogueMockery(NPC cynic)
        {
            string scorn = string.Empty;

            if (cynic.Manners.Equals("Rude"))
            {
                switch (Game1.random.Next(2))
                {
                    case 0:
                        scorn = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.9";
                        break;
                    case 1:
                        scorn = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.10";
                        break;
                }
            }
            else
            {
                switch (Game1.random.Next(2))
                {
                    case 0:
                        scorn = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.11";
                        break;
                    case 1:
                        scorn = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.12";
                        break;
                }
            }




                return scorn;
        }

        public static string dialogueStrawHat()
        {
            string lines = string.Empty;

            switch (Game1.random.Next(2))
            {
                case 0:
                    lines = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.13";
                    break;
                case 1:
                    lines = "Strings\\StringsFromCSFiles:MermaidArmourBuffs.14";
                    break;
            }

            return lines;
        }

        public static HashSet<string> bridalDialogue = new HashSet<string>();

        public static HashSet<string> bikiniDIalogue = new HashSet<string>();

        public static HashSet<string> dialogueFriendshipGain10 = new HashSet<string>();

        public static HashSet<string> Mockery = new HashSet<string>();
        
        public static HashSet<string> StrawHat = new HashSet<string>();

        public static HashSet<string> HowdyPilgrim = new HashSet<string>();


        private static void NPC_grantConversationFriendshipPostfix(NPC __instance , Farmer who, int amount = 20)
       
       // private static void NPC_checkActionPostfix(NPC __instance, Farmer who, GameLocation l, ref bool __result)
        {
            //dialogueBikini = "Oooooh! You look super cute!";
            // if(Game1.player.shirt.Equals("1265"))
            //{
            //   amount = 30;
            //  who.changeFriendship(amount, __instance);
            // }

            //              case "(S)1265": //Bridal Top

            if (Game1.player.hat.Value != null)
            {
                switch (Game1.player.hat.Value.QualifiedItemId)
                {
                    case "(H)69": //Wedding Veil
                        {
                            if (!bridalDialogue.Contains(__instance.Name))
                            {
                                __instance.CurrentDialogue.Push(new Dialogue(__instance, dialogueBridal()));
                                bridalDialogue.Add(__instance.Name);
                            }
                        }
                        break;
                    case "(H)68":
                        {
                            if (!Mockery.Contains(__instance.Name))
                            {
                                __instance.CurrentDialogue.Push(new Dialogue(__instance, dialogueMockery(__instance)));
                                Mockery.Add(__instance.Name);
                            }
                        }
                        break;
                    case "(H)5":
                        {

                            if (!StrawHat.Contains(__instance.Name))
                            {
                                __instance.CurrentDialogue.Push(new Dialogue(__instance, dialogueStrawHat()));
                                StrawHat.Add(__instance.Name);
                            }

                        }
                        break;
                    case "(H)63":
                        {

                            if (!HowdyPilgrim.Contains(__instance.Name))
                            {
                                if(Game1.random.NextDouble() <= .4)
                                __instance.CurrentDialogue.Push(new Dialogue(__instance, "Howdy, Pilgrim!"));
                                HowdyPilgrim.Add(__instance.Name);
                            }

                        }
                        break;
                }
            }


            if (Game1.player.shirtItem.Value != null)
            {
                switch (Game1.player.shirtItem.Value.QualifiedItemId)
                {
                    case "(S)1265": //Bridal Top

                        {
                            if (!bridalDialogue.Contains(__instance.Name))
                            {
                                __instance.CurrentDialogue.Push(new Dialogue(__instance, dialogueBridal()));
                                bridalDialogue.Add(__instance.Name);
                            }
                        }
                        break;

                    case "(S)1201": //Meh Classy Top of Blandness!!!
                    case "(S)1202": //Classy Top For Ladies!!!
                    case "(S)1277": //Silky Top

                        if (!dialogueFriendshipGain10.Contains(__instance.Name))
                        {
                            amount = 10;
                            who.changeFriendship(amount, __instance);
                            dialogueFriendshipGain10.Add(__instance.Name);
                        }
                        break;

                    case "(S)1297": //Island Bikini
                    case "(S)1134": //Regular Bikini
                        CharacterData data = __instance.GetData();
                        if (data.CustomFields.ContainsKey("ClothesGiveBuffs.NotFlirt"))
                        {
                            break;
                        }
                        // if (BikiniFlirt == false)
                        if (!bikiniDIalogue.Contains(__instance.Name) && Config.Flirt)
                        {
                            if (Config.SameSexFlirtOnly)
                            {
                                if (__instance.IsVillager && __instance.Gender == Game1.player.Gender)
                                {
                                    __instance.CurrentDialogue.Push(new Dialogue(__instance, dialogueBikini()));
                                    bikiniDIalogue.Add(__instance.Name);
                                    break;
                                }
                            }
                            else if (!Config.SameSexFlirtOnly && __instance.IsVillager)
                            {
                                __instance.CurrentDialogue.Push(new Dialogue(__instance, dialogueBikini()));
                                bikiniDIalogue.Add(__instance.Name);
                            }
                        }
                        // BikiniFlirt = true;
                        break;
                }
            }
        }


        private static void GameLocation_drawAboveAlwaysFrontLayer_Postfix(SpriteBatch b)
        {
            if (isWearingIridiumArmour)
            {
                Vector2 v;
                v = default(Vector2);
                for (float x = -256 + (int)(chaosPos.X % 256f); x < (float)Game1.graphics.GraphicsDevice.Viewport.Width; x += 256f)
                {
                    for (float y = -256 + (int)(chaosPos.Y % 256f); y < (float)Game1.graphics.GraphicsDevice.Viewport.Height; y += 256f)
                    {
                        v.X = (int)x;
                        v.Y = (int)y;
                        b.Draw(Game1.mouseCursors, v, chaosSource, Color.Crimson, 0f, Vector2.Zero, 4.001f, SpriteEffects.None, 1f);
                    }
                }
            }
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            BikiniFlirt = false;

            bikiniDIalogue.Clear();
            bridalDialogue.Clear();
            Mockery.Clear();
            HowdyPilgrim.Clear();
            dialogueFriendshipGain10.Clear();

        }
    }
}
