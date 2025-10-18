/* Smart Spin - Pre-fill Optimal Wager
 *
 * SMAPI mod that automatically calculates and pre-fills the optimal
 * wager when placing a bet on the Spinning Wheel in the Stardew Valley
 * Fair.
 * 
 * Copyright (C) 2024-2025 Jonathan Feenstra
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using SmartSpin.GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace SmartSpin;

internal sealed class ModEntry : Mod
{
    private ModConfig? _config;
    
    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (Game1.CurrentEvent?.isSpecificFestival("fall16") is not true || Game1.currentLocation?.lastQuestionKey != "wheelBet") return;
        if (e.NewMenu is NumberSelectionMenu wagerSelectionMenu) SetOptimalWager(wagerSelectionMenu);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is not null) RegisterGenericModConfigMenu(configMenu);
    }

    private void RegisterGenericModConfigMenu(IGenericModConfigMenuApi configMenu)
    {
        configMenu.Register(ModManifest, () => _config = new ModConfig(), () => Helper.WriteConfig(_config!));
        
        configMenu.AddNumberOption(
            ModManifest,
            getValue: () => _config!.GreenProbability,
            setValue: x => _config!.GreenProbability = x,
            name: () => Helper.Translation.Get("green-probability-name"),
            tooltip: () =>Helper.Translation.Get("green-probability-tooltip"),
            min: 0f,
            max: 1f,
            interval: 0.0001f, // Due to floating point precision, not all values between 0 and 1 are representable with increments of 0.0001
            formatValue: x => x.ToString("P2"));
        
        configMenu.AddBoolOption(
            ModManifest,
            getValue: () => _config!.EnableLuckySpeedups,
            setValue: x => _config!.EnableLuckySpeedups = x,
            name: () => Helper.Translation.Get("enable-lucky-speedups-name"),
            tooltip: () =>Helper.Translation.Get("enable-lucky-speedups-tooltip"));
        
        configMenu.AddNumberOption(
            ModManifest,
            getValue: () => _config!.KellyFractionMultiplier,
            setValue: x => _config!.KellyFractionMultiplier = x,
            name: () => Helper.Translation.Get("kelly-fraction-multiplier-name"),
            tooltip: () =>Helper.Translation.Get("kelly-fraction-multiplier-tooltip"),
            min: 0f,
            max: 2f,
            interval: 0.01f);
    }

    private void SetOptimalWager(NumberSelectionMenu wagerSelectionMenu)
    {
        var optimalWagerFraction = CalculateOptimalWagerFraction(Game1.player.LuckLevel);
        var maxWinnableStarTokens = 9999 - Game1.player.festivalScore;
        var optimalWager = Math.Min(Convert.ToInt32(optimalWagerFraction * Game1.player.festivalScore), maxWinnableStarTokens);
        SetWager(wagerSelectionMenu, optimalWager);
    }

    private float CalculateOptimalWagerFraction(int luckLevel)
    {
        float baseWinProbability, baseLoseProbability, luckySpeedupProbability;

        // See StardewValley.Menus.WheelSpinGame.Update: lucky speedup probability = luck level / 15 for green and / 20 for orange
        var isBetOnGreen = Game1.currentLocation.currentEvent.specialEventVariable2;
        if (isBetOnGreen)
        {
            luckySpeedupProbability = _config!.EnableLuckySpeedups ? Math.Clamp(luckLevel / 15f, 0, 1) : 0;
            baseWinProbability = _config.GreenProbability;
            baseLoseProbability = 1 - baseWinProbability;
        }
        else
        {
            luckySpeedupProbability = _config!.EnableLuckySpeedups ? Math.Clamp(luckLevel / 20f, 0, 1) : 0;
            baseLoseProbability = _config.GreenProbability;
            baseWinProbability = 1 - baseLoseProbability;
        }
        
        var winProbability = baseWinProbability + baseLoseProbability * luckySpeedupProbability;
        
        // Kelly criterion: https://en.wikipedia.org/wiki/Kelly_criterion#Gambling_Formula
        // optimal fraction = win probability - (lose probability / win proportion)
        //                  = win probability - ((1 - win probability) / 1)
        //                  = 2 * win probability - 1
        var kellyFraction = 2 * winProbability - 1;
        return Math.Clamp(_config.KellyFractionMultiplier * kellyFraction, 0, 1);
    }
    
    private void SetWager(NumberSelectionMenu wagerSelectionMenu, int wager)
    {
        var wagerField = Helper.Reflection.GetField<int>(wagerSelectionMenu, "currentValue");
        var textBox = Helper.Reflection.GetField<TextBox>(wagerSelectionMenu, "numberSelectedBox").GetValue();
        wagerField.SetValue(wager);
        textBox.Text = wager.ToString();
    }
}