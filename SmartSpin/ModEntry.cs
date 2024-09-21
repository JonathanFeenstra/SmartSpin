/* Smart Spin - Pre-fill Optimal Wager
 *
 * SMAPI mod that automatically calculates and pre-fills the optimal
 * wager when placing a bet on the Spinning Wheel in the Stardew Valley
 * Fair.
 * 
 * Copyright (C) 2024 Jonathan Feenstra
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

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace SmartSpin;

internal sealed class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        helper.Events.Display.MenuChanged += OnMenuChanged;
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (Game1.CurrentEvent?.isSpecificFestival("fall16") is not true || Game1.currentLocation?.lastQuestionKey != "wheelBet") return;
        if (e.NewMenu is not NumberSelectionMenu wagerSelectionMenu) return;
        SetOptimalWager(wagerSelectionMenu);
    }

    private void SetOptimalWager(NumberSelectionMenu wagerSelectionMenu)
    {
        var optimalWagerFraction = CalculateOptimalWagerFraction(Game1.player.LuckLevel, isBetOnGreen: Game1.currentLocation.currentEvent.specialEventVariable2);
        var maxWinnableStarTokens = 9999 - Game1.player.festivalScore;
        var optimalWager = Math.Min(Convert.ToInt32(optimalWagerFraction * Game1.player.festivalScore), maxWinnableStarTokens);
        SetWager(wagerSelectionMenu, optimalWager);
    }

    private static float CalculateOptimalWagerFraction(int luckLevel, bool isBetOnGreen)
    {
        // See StardewValley.Menus.WheelSpinGame ctor: out of 30 possible initial velocities, 22 land on green (11 / 15) and 8 on orange (4 / 15)
        const float baseGreenProbability = 11f / 15f, baseOrangeProbability = 4f / 15f;
        float baseWinProbability, baseLoseProbability, luckySpeedupProbability;

        // See StardewValley.Menus.WheelSpinGame.Update: lucky speedup probability = luck level / 15 for green and / 20 for orange 
        if (isBetOnGreen)
        {
            luckySpeedupProbability = Math.Clamp(luckLevel / 15f, 0, 1);
            baseWinProbability = baseGreenProbability;
            baseLoseProbability = baseOrangeProbability;
        }
        else
        {
            luckySpeedupProbability = Math.Clamp(luckLevel / 20f, 0, 1);
            baseWinProbability = baseOrangeProbability;
            baseLoseProbability = baseGreenProbability;
        }
        
        var winProbability = baseWinProbability + baseLoseProbability * luckySpeedupProbability;
        
        // Kelly criterion: https://en.wikipedia.org/wiki/Kelly_criterion#Gambling_formula
        // optimal fraction = win probability - (lose probability / win proportion)
        //                  = win probability - ((1 - win probability) / 1)
        //                  = 2 * win probability - 1
        return Math.Clamp(2 * winProbability - 1, 0, 1);
    }
    
    private void SetWager(NumberSelectionMenu wagerSelectionMenu, int wager)
    {
        var wagerField = Helper.Reflection.GetField<int>(wagerSelectionMenu, "currentValue");
        var textBox = Helper.Reflection.GetField<TextBox>(wagerSelectionMenu, "numberSelectedBox").GetValue();
        wagerField.SetValue(wager);
        textBox.Text = wager.ToString();
    }
}