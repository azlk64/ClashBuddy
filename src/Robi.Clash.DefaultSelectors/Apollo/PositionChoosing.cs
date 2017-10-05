﻿using Robi.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Robi.Clash.DefaultSelectors.Apollo
{
    class PositionChoosing
    {
        private static readonly ILogger Logger = LogProvider.CreateLogger<PositionChoosing>();

        public static VectorAI GetNextSpellPosition(FightState gameState, Handcard hc, Playfield p)
        {
            if (hc == null || hc.card == null)
                return null;

            VectorAI choosedPosition = null;


            if (hc.card.type == boardObjType.AOE || hc.card.type == boardObjType.PROJECTILE)
            {
                Logger.Debug("AOE or PROJECTILE");
                return GetPositionOfTheBestDamagingSpellDeploy(p);
            }

            // ToDo: Handle Defense Gamestates
            switch (gameState)
            {
                case FightState.UAKTL1:
                    choosedPosition = UAKT(p, hc, 1);
                    break;
                case FightState.UAKTL2:
                    choosedPosition = UAKT(p, hc, 2);
                    break;
                case FightState.UAPTL1:
                    choosedPosition = UAPTL1(p, hc);
                    break;
                case FightState.UAPTL2:
                    choosedPosition = UAPTL2(p, hc);
                    break;
                case FightState.AKT:
                    choosedPosition = AKT(p);
                    break;
                case FightState.APTL1:
                    choosedPosition = APTL1(p);
                    break;
                case FightState.APTL2:
                    choosedPosition = APTL2(p);
                    break;
                case FightState.DKT:
                    choosedPosition = DKT(p, hc,0);
                    break;
                case FightState.DPTL1:
                    choosedPosition = DPTL1(p, hc);
                    break;
                case FightState.DPTL2:
                    choosedPosition = DPTL2(p, hc);
                    break;
                default:
                    //Logger.Debug("GameState unknown");
                    break;
            }

            if (choosedPosition == null)
                return null;

            //Logger.Debug("GameState: {GameState}", gameState.ToString());
            //Logger.Debug("nextPosition: " + nextPosition);

            return choosedPosition;
        }

        #region UnderAttack
        private static VectorAI UAKT(Playfield p, Handcard hc, int line)
        {
            return DKT(p, hc, line);
        }

        private static VectorAI UAPTL1(Playfield p, Handcard hc)
        {
            return DPTL1(p, hc);
        }
        private static VectorAI UAPTL2(Playfield p, Handcard hc)
        {
            return DPTL2(p, hc);
        }
        #endregion

        #region Defense
        private static VectorAI DKT(Playfield p, Handcard hc, int line)
        {
            // ToDo: Improve
            if(line == 0)
            {
                if (p.enemyPrincessTower1.HP < p.enemyPrincessTower2.HP)
                    line = 1;
                else
                    line = 2;
            }

            if (hc.card.type == boardObjType.MOB)
            {
                // Debugging: try - catch is just for debugging
                try
                {
                    if (hc.card.MaxHP >= Setting.MinHealthAsTank)
                    {

                        // TODO: Analyse which is the most dangerous line
                        if (line == 2)
                        {
                            Logger.Debug("KT RightUp");
                            VectorAI v = p.getDeployPosition(p.ownKingsTower.Position, deployDirectionRelative.RightUp, 100);
                            return v;
                        }
                        else
                        {
                            Logger.Debug("KT LeftUp");
                            VectorAI v = p.getDeployPosition(p.ownKingsTower.Position, deployDirectionRelative.LeftUp, 100);
                            return v;
                        }
                    }
                }
                catch (Exception) { }

                if (hc.card.Transport == transportType.AIR)
                {
                    // TODO: Analyse which is the most dangerous line
                    if (line == 2)
                        return p.getDeployPosition(deployDirectionAbsolute.ownPrincessTowerLine2);
                    else
                        return p.getDeployPosition(deployDirectionAbsolute.ownPrincessTowerLine1);
                }
                else
                {
                    if (line == 2)
                    {
                        Logger.Debug("BehindKT: Line2");
                        VectorAI position = p.getDeployPosition(deployDirectionAbsolute.behindKingsTowerLine2);
                        return position;
                    }
                    else
                    {
                        Logger.Debug("BehindKT: Line1");
                        VectorAI position = p.getDeployPosition(deployDirectionAbsolute.behindKingsTowerLine1);
                        return position;
                    }
                }
            }
            else if (hc.card.type == boardObjType.BUILDING)
            {
                //switch ((cardToDeploy as CardBuilding).Type)
                //{
                //    case BuildingType.BuildingDefense:
                //    case BuildingType.BuildingSpawning:
                return GetPositionOfTheBestBuildingDeploy(p);
                //}
            }
            else if (hc.card.type == boardObjType.AOE || hc.card.type == boardObjType.PROJECTILE)
                return GetPositionOfTheBestDamagingSpellDeploy(p);
            else
            {
                Logger.Debug("DKT: Handcard equals NONE!");
                return p.ownKingsTower?.Position;
            }

        }
        private static VectorAI DPTL1(Playfield p, Handcard hc)
        {
            BoardObj lPT = p.ownPrincessTower1;

            if (lPT == null || lPT.Position == null)
                return DKT(p, hc,1);

            VectorAI lPTP = lPT.Position;
            VectorAI correctedPosition = PrincessTowerCharacterDeploymentCorrection(lPTP, p, hc);
            return correctedPosition;
        }
        private static VectorAI DPTL2(Playfield p, Handcard hc)
        {
            BoardObj rPT = p.ownPrincessTower2;

            if (rPT == null && rPT.Position == null)
                return DKT(p, hc,2);

            VectorAI rPTP = rPT.Position;
            VectorAI correctedPosition = PrincessTowerCharacterDeploymentCorrection(rPTP, p, hc);
            return correctedPosition;
        }
        #endregion

        #region Attack
        private static VectorAI AKT(Playfield p)
        {
            Logger.Debug("AKT");

            if (p.enemyPrincessTowers.Count == 2)
            {
                if (p.enemyPrincessTower1.HP < p.enemyPrincessTower2.HP)
                    return APTL1(p);
                else
                    return APTL2(p);
            }

            if (p.enemyPrincessTower1.HP == 0)
                return APTL1(p);

            if (p.enemyPrincessTower2.HP == 0)
                return APTL2(p);

            VectorAI position = p.enemyKingsTower?.Position;

            if (Decision.SupportDeployment(p, 1))
                position = p.getDeployPosition(position, deployDirectionRelative.Down, 500);

            return position;
        }
        private static VectorAI APTL1(Playfield p)
        {
            Logger.Debug("ALPT");

            VectorAI behindTank = Helper.DeployBehindTank(p, 1);

            if (behindTank != null)
                return behindTank;

            VectorAI lPT = p.getDeployPosition(deployDirectionAbsolute.enemyPrincessTowerLine1);

            if (Decision.SupportDeployment(p, 1))
                lPT = p.getDeployPosition(lPT, deployDirectionRelative.Down, 500);

            return lPT;
        }
        private static VectorAI APTL2(Playfield p)
        {
            Logger.Debug("ARPT");

            VectorAI behindTank = Helper.DeployBehindTank(p, 2);

            if (behindTank != null)
                return behindTank;

            VectorAI rPT = p.getDeployPosition(deployDirectionAbsolute.enemyPrincessTowerLine2);

            if (Decision.SupportDeployment(p, 2))
                rPT = p.getDeployPosition(rPT, deployDirectionRelative.Down, 500);

            return rPT;
        }
        #endregion

        public static VectorAI GetPositionOfTheBestDamagingSpellDeploy(Playfield p)
        {
            // Prio1: Hit Enemy King Tower if health is low
            // Prio2: Every damaging spell if there is a big group of enemies
            Logger.Debug("GetPositionOfTheBestDamaingSpellDeploy");

            // Debugging: try - catch is just for debugging
            try
            {
                if (p.enemyKingsTower?.HP < Setting.KingTowerSpellDamagingHealth || (p.enemyMinions.Count + p.enemyBuildings.Count) < 1)
                    return p.enemyKingsTower?.Position;
            }
            catch (Exception)
            {
                BoardObj enemy = Helper.EnemyCharacterWithTheMostEnemiesAround(p, out int count, transportType.NONE);

                if (enemy != null && enemy.Position != null)
                {
                    // Debugging: try - catch is just for debugging
                    try
                    {
                        // ToDo: Use a mix of the HP and count of the Enemy Units
                        // How fast are the enemy units, needed for a better correction
                        if (Helper.HowManyNFCharactersAroundCharacter(p, enemy) >= Setting.SpellCorrectionConditionCharCount)
                        {
                            Logger.Debug("With correction; enemy.Name = {Name}", enemy.Name);
                            if (enemy.Position != null)
                            {
                                Logger.Debug("enemy.Position = {position}", enemy.Position);
                                return p.getDeployPosition(enemy.Position, deployDirectionRelative.Down, 500);
                            }
                        }
                        else
                        {
                            Logger.Debug("No correction; enemy.Name = {Name}", enemy.Name);
                            if (enemy.Position != null)
                            {
                                Logger.Debug("enemy.Position = {position}", enemy.Position);
                                return enemy.Position;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //enemy.Position.AddYInDirection(p, 3000); // Position Correction
                        VectorAI result = p.getDeployPosition(enemy.Position, deployDirectionRelative.Down, 500);

                        Logger.Debug("enemy.Name = {Name}", enemy.Name);
                        if (enemy.Position != null) Logger.Debug("enemy.Position = {position}", enemy.Position);
                        Logger.Debug("result = {position}", result);

                        return result;
                    }
                }
                Logger.Debug("enemy = null?{enemy} ; enemy.position = null?{position}", enemy == null, enemy.Position == null);
            }

            Logger.Debug("Error: 0/0");
            return new VectorAI(0, 0);
        }

        public static VectorAI GetPositionOfTheBestBuildingDeploy(Playfield p)
        {
            // ToDo: Find the best position
            VectorAI betweenBridges = p.getDeployPosition(deployDirectionAbsolute.betweenBridges);
            VectorAI result = p.getDeployPosition(betweenBridges, deployDirectionRelative.Down, 4000);
            return result;
        }

        private static VectorAI PrincessTowerCharacterDeploymentCorrection(VectorAI position, Playfield p, Handcard hc)
        {
            if (hc == null || hc.card == null || position == null)
                return null;

            //Logger.Debug("PT Characer Position Correction: Name und Typ {0} " + cardToDeploy.Name, (cardToDeploy as CardCharacter).Type);
            if (hc.card.type == boardObjType.MOB)
            {
                // Debugging: try - catch is just for debugging
                try
                {
                    if (hc.card.MaxHP >= Setting.MinHealthAsTank)
                    {
                        //position.SubtractYInDirection(p);
                        return p.getDeployPosition(position, deployDirectionRelative.Up, 100);
                    }
                    else return p.getDeployPosition(position, deployDirectionRelative.Down, 2000);
                }
                catch (Exception) { }
                {
                    return p.getDeployPosition(position, deployDirectionRelative.Down, 2000);
                }

            }
            else if (hc.card.type == boardObjType.BUILDING)
                return GetPositionOfTheBestBuildingDeploy(p);
            else
                Logger.Debug("Tower Correction: No Correction!!!");

            return position;
        }


    }
}