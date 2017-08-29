﻿using Buddy.Clash.DefaultSelectors.Enemy;
using Buddy.Clash.DefaultSelectors.Player;
using Buddy.Clash.DefaultSelectors.Utilities;
using Buddy.Clash.Engine;
using Buddy.Clash.Engine.NativeObjects.Logic.GameObjects;
using Buddy.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Buddy.Clash.DefaultSelectors.Game
{
    enum FightState
    {
        DLPT,       // Defense LeftPrincessTower
        DKT,        // Defense KingTower
        DRPT,       // Defense RightPrincessTower
        UALPT,      // UnderAttack LeftPrincessTower
        UAKT,       // UnderAttack KingTower
        UARPT,      // UnderAttack RightPrincessTower
        ALPT,       // Attack LeftPrincessTower
        AKT,        // Attack KingTower
        ARPT,        // Attack RightPrincessTower
        START
    };

    enum EnemyPrincessTowerState
    {
        NPTD,       // No PrincessTower is down
        LPTD,       // Left PrincessTower is down
        RPTD,       // Right PrincessTower is down
        BPTD        // Both PrincessTower are down
    }

    enum PlayerPrincessTowerState
    {
        NPTD,       // No PrincessTower is down
        LPTD,       // Left PrincessTower is down
        RPTD,       // Right PrincessTower is down
        BPTD        // Both PrincessTower are down
    }

    enum GameMode
    {
        ONE_VERSUS_ONE,
        TWO_VERSUS_TWO,
        NOT_IMPLEMENTED
    }

    class GameStateHandling
    {
        private static readonly ILogger Logger = LogProvider.CreateLogger<GameStateHandling>();
        public static bool GameBeginning = true;

        public static FightState CurrentFightState
        {
            get
            {
                switch (PlayerProperties.FightStyle)
                {
                    case FightStyle.Defensive:
                        return GetCurrentFightStateDefensive();
                    case FightStyle.Balanced:
                        return GetCurrentFightStateBalanced();
                    case FightStyle.Rusher:
                        return GetCurrentFightStateRusher();
                    default:
                        return FightState.DKT;
                }
            }
        }

        private static FightState GetCurrentFightStateBalanced()
        {
            if (GameBeginning)
                return GameBeginningDecision();

            if (EnemyCharacterHandling.IsAnEnemyOnOurSide())
                return EnemyIsOnOurSideDecision();
            else if (EnemyCharacterHandling.EnemiesWithoutTower.Count() > 1)
                return EnemyHasCharsOnHisSideDecision();
            else
                return AttackDecision();
        }

        private static FightState GetCurrentFightStateRusher()
        {
            if (EnemyCharacterHandling.IsAnEnemyOnOurSide())
                return EnemyIsOnOurSideDecision();
            else
                return AttackDecision();
        }

        private static FightState GetCurrentFightStateDefensive()
        {
            if (GameBeginning)
                return GameBeginningDecision();

            if (EnemyCharacterHandling.IsAnEnemyOnOurSide())
                return EnemyIsOnOurSideDecision();
            else if (EnemyCharacterHandling.EnemiesWithoutTower.Count() > 1)
                return EnemyHasCharsOnHisSideDecision();
            else
                return DefenseDecision();
        }

        public static EnemyPrincessTowerState CurrentEnemyPrincessTowerState
        {
            get
            {
                int stateCode = 0;

                if (EnemyCharacterHandling.EnemyLeftPrincessTower == null)
                    stateCode += 1;

                if (EnemyCharacterHandling.EnemyRightPrincessTower == null)
                    stateCode += 2;

                return (EnemyPrincessTowerState)stateCode;
            }
        }

        public static PlayerPrincessTowerState CurrentPlayerPrincessTowerState
        {
            get
            {
                int stateCode = 0;

                if (PlayerCharacterHandling.LeftPrincessTower == null)
                    stateCode += 1;

                if (PlayerCharacterHandling.RightPrincessTower == null)
                    stateCode += 2;

                return (PlayerPrincessTowerState)stateCode;
            }
        }

        public static GameMode CurrentGameMode
        {
            get
            {
                if (StaticValues.PlayerCount == 2)
                    return GameMode.ONE_VERSUS_ONE;
                else if (StaticValues.PlayerCount == 4)
                    return GameMode.TWO_VERSUS_TWO;
                else
                {
                    Logger.Debug("GameMode: Seems to be not 1v1 or 2v2!");
                    return GameMode.NOT_IMPLEMENTED;
                }
                    
            }
        }



        #region GameState-Decisions

        private static FightState AttackDecision()
        {
            Character princessTower = EnemyCharacterHandling.GetEnemyPrincessTowerWithLowestHealth(StaticValues.Player.OwnerIndex);

            if(princessTower == null)
                return FightState.AKT;

            if (CurrentEnemyPrincessTowerState > 0)
                return FightState.AKT;

            if (PlaygroundPositionHandling.IsPositionOnTheRightSide(princessTower.StartPosition))
                return FightState.ARPT;
            else
                return FightState.ALPT;
        }

        private static FightState DefenseDecision()
        {
            Character princessTower = EnemyCharacterHandling.GetEnemyPrincessTowerWithLowestHealth(StaticValues.Player.OwnerIndex);

            if (princessTower == null)
                return FightState.AKT;

            if (CurrentEnemyPrincessTowerState > 0)
                return FightState.AKT;

            if (PlaygroundPositionHandling.IsPositionOnTheRightSide(princessTower.StartPosition))
                return FightState.DRPT;
            else
                return FightState.DLPT;
        }

        private static FightState GameBeginningDecision()
        {
            if (StaticValues.Player.Mana < 9)
            {
                if (EnemyCharacterHandling.IsAnEnemyOnOurSide())
                    GameBeginning = false;

                return FightState.START;
            }
            else
            {
                GameBeginning = false;

                if (PlaygroundPositionHandling.IsPositionOnTheRightSide(EnemyCharacterHandling.NearestEnemy.StartPosition))
                    return FightState.DRPT;
                else
                    return FightState.DLPT;
            }
        }

        private static FightState EnemyIsOnOurSideDecision()
        {
            if (PlayerCharacterHandling.PrincessTower.Count() > 1)
            {
                if (PlaygroundPositionHandling.IsPositionOnTheRightSide(EnemyCharacterHandling.NearestEnemy.StartPosition))
                    return FightState.UARPT;
                else
                    return FightState.UALPT;
            }
            else
            {
                return FightState.UAKT;
            }
        }

        private static FightState EnemyHasCharsOnHisSideDecision()
        {
            if (PlayerCharacterHandling.PrincessTower.Count() > 1)
            {
                if (PlaygroundPositionHandling.IsPositionOnTheRightSide(EnemyCharacterHandling.NearestEnemy.StartPosition))
                    return FightState.DRPT;
                else
                    return FightState.DLPT;
            }
            else
            {
                return FightState.DKT;
            }
        }
        #endregion
    }
}
