using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SabberStoneCore.Model;
using SabberStoneCore.Tasks;
using SabberStoneCoreAi.Agent;
using SabberStoneCoreAi.POGame;
using Agent.Utils;
using System.Collections;
using SabberStoneCore.Model.Entities;
using System.Diagnostics;

namespace SabberStoneCoreAi.src.Agent
{
    class SimpleBalanceAgent : AbstractAgent
    {
        static bool DEBUG = true;
        protected Stopwatch TurnStopwatch = new Stopwatch();
        private bool NewTurnStarted = true;
        private const double TauntMultiplier = 1.5;
        private const double HeroHealthMultiplier = 1.5;
        private Random Rnd = new Random();
        protected GameComparer Comp = new GameComparer();

        public override PlayerTask GetMove(SabberStoneCoreAi.POGame.POGame poGame)
        {
            if (NewTurnStarted)
            {
                NewTurnStarted = false;
                TurnStopwatch.Start();
            }
            PlayerTask nextTask = PlanNextTurn(poGame);
            if (nextTask.PlayerTaskType == PlayerTaskType.END_TURN)
            {
                if (DEBUG)
                {
                    TurnStopwatch.Stop();
                    if (DEBUG && TurnStopwatch.Elapsed.TotalSeconds > 75)
                    {
                        Console.WriteLine("Exceeded turn time: {0} seconds", TurnStopwatch.Elapsed.TotalSeconds);
                    }
                    TurnStopwatch.Reset();
                }
                NewTurnStarted = true;
            }
            return nextTask;
        }

        private PlayerTask PlanNextTurn(POGame.POGame poGame)
        {
            var options = poGame.CurrentPlayer.Options();


            if (options.Count == 1)
            {
                return options[0];
            }
            else
            {
                var minionAttacks = new List<PlayerTask>();
                var faceAttacks = new List<PlayerTask>();
                var heroAttacks = new List<PlayerTask>();
                PlayerTask heroFace = null;
                var cards = new List<PlayerTask>();
                int ourHealth = poGame.CurrentPlayer.Hero.Health + poGame.CurrentPlayer.Hero.Armor;
                int hisHealth = poGame.CurrentOpponent.Hero.Health + poGame.CurrentOpponent.Hero.Armor;
                int totalFaceDamage = 0;
                int totalIncomingFaceDamage = 0;

                // sort the moves
                foreach (PlayerTask task in options)
                {
                    if (task.PlayerTaskType == PlayerTaskType.MINION_ATTACK)
                    {
                        if (task.Target == poGame.CurrentOpponent.Hero)
                        {
                            faceAttacks.Add(task);
                            if (task.Source is Minion)
                            {
                                totalFaceDamage += ((Minion)task.Source).AttackDamage;
                            }
                        }
                        else
                        {
                            minionAttacks.Add(task);
                        }
                    }
                    else if (task.PlayerTaskType == PlayerTaskType.PLAY_CARD || task.PlayerTaskType == PlayerTaskType.HERO_POWER)
                    {
                        cards.Add(task);
                    }
                    else if (task.PlayerTaskType == PlayerTaskType.HERO_ATTACK)
                    {
                        heroAttacks.Add(task);
                        if (task.Target == poGame.CurrentOpponent.Hero)
                        {
                            totalFaceDamage += poGame.CurrentPlayer.Hero.AttackDamage;
                            heroFace = task;
                        }
                    }
                }
                foreach (var card in poGame.CurrentOpponent.BoardZone)
                {
                    totalIncomingFaceDamage += card.AttackDamage * (card.HasWindfury ? 2 : 1);
                }

                // section card play
                var cardToPlay = BestCard(cards, poGame);
                if (cardToPlay != null)
                {
                    return cardToPlay;
                }


                // section minion attacks
                else if (faceAttacks.Count > 0 || minionAttacks.Count > 0)
                {
                    if (faceAttacks.Count > 0 && (totalFaceDamage > totalIncomingFaceDamage && ourHealth > hisHealth || totalFaceDamage >= hisHealth))
                    {
                        return faceAttacks[0];
                    }
                    else if (minionAttacks.Count > 0 && totalIncomingFaceDamage > ourHealth)
                    {
                        return BestBoardCleaner(minionAttacks);
                    }
                    else if (faceAttacks.Count > 0)
                    {
                        return faceAttacks[0];
                    }
                    else if (minionAttacks.Count > 0)
                    {
                        return BestBoardCleaner(minionAttacks);
                    }
                }

                // section hero attacks
                else if (heroAttacks.Count > 0)
                {
                    if (heroFace != null && totalFaceDamage >= hisHealth)
                    {
                        return heroFace;
                    }
                    else if (ourHealth > 15)
                    {
                        PlayerTask best = null;
                        var bestScore = 0;
                        foreach (var attack in heroAttacks.Except(new PlayerTask[] { heroFace }))
                        {
                            var target = (Minion)attack.Target;
                            var currentScore = (target.HasDivineShield || target.Health > poGame.CurrentPlayer.Hero.AttackDamage) ? 0 : target.AttackDamage;
                            if (currentScore > bestScore)
                            {
                                best = attack;
                                bestScore = currentScore;
                            }

                        }
                        if (best != null && ourHealth - bestScore > 10)
                        {
                            return best;
                        }
                    }
                    else if (heroFace != null)
                    {
                        return heroFace;
                    }
                }
            }
            return options[0];
        }

        protected virtual PlayerTask BestCard(List<PlayerTask> cards, POGame.POGame poGame)
        {
            if (cards.Count > 0)
            {
                var simulation = poGame.Simulate(cards);
                var unpredicted = simulation.Where(it => it.Value == null).ToList();
                /*if (unpredicted.Count > 0)
                {
                    if (System.Diagnostics.Debugger.IsAttached)
                        System.Diagnostics.Debugger.Break();
                }*/
                var best = simulation.Where(it => it.Value != null).MaxElement(Comp, (it => it.Value));
                if (best.Key != null && Comp.Compare(best.Value, poGame) >= 0)
                {
                    return best.Key;
                }
            }
            return null;
        }

        private static PlayerTask BestBoardCleaner(List<PlayerTask> minionAttacks)
        {
            Dictionary<Minion, List<PlayerTask>> minionToTasks = new Dictionary<Minion, List<PlayerTask>>();
            foreach (var task in minionAttacks)
            {
                Minion minion = (Minion)task.Source;
                if (!minionToTasks.ContainsKey(minion))
                {
                    minionToTasks[minion] = new List<PlayerTask>();
                }
                minionToTasks[minion].Add(task);
            }
            int[] bounds = minionToTasks.Select(it => it.Value.Count).ToArray();
            int[] bestSolution = Enumerable.Repeat(0, bounds.Length).ToArray();
            int[] currentSolution = Enumerable.Repeat(0, bounds.Length).ToArray();
            int bestScore = SolutionScore(bestSolution, minionToTasks);
            for (int i = 0; i < bounds.Length; i++)
            {
                for (int j = 0; j < bounds[i]; j++)
                {
                    currentSolution[i] = j;
                    var currentScore = SolutionScore(currentSolution, minionToTasks);
                    if (currentScore > bestScore)
                    {
                        Array.Copy(currentSolution, bestSolution, bestSolution.Length);
                        bestScore = currentScore;
                    }
                }
            }

            return minionToTasks[minionToTasks.Keys.First()][bestSolution[0]];
        }

        private static int SolutionScore(int[] bestSolution, Dictionary<Minion, List<PlayerTask>> minionToTasks)
        {
            int removedAttack = 0;
            foreach (var ((minion, tasks), index) in minionToTasks.Zip(bestSolution, (a, b) => Tuple.Create(a, b)))
            {
                Minion target = (Minion)tasks[index].Target;
                if (target.Health <= minion.AttackDamage)
                {
                    removedAttack += target.AttackDamage;
                }
            }
            return removedAttack;
        }

        public override void InitializeAgent()
        {
            Rnd = new Random();
        }

        public override void InitializeGame()
        {
            NewTurnStarted = true;
        }

        public override void FinalizeAgent()
        {
            TurnStopwatch.Stop();
            TurnStopwatch.Reset();
        }

        public override void FinalizeGame()
        {
            TurnStopwatch.Stop();
            TurnStopwatch.Reset();
        }

    }

    class GameComparer : IComparer<POGame.POGame>
    {
        public int Compare(POGame.POGame stateX, POGame.POGame stateY)
        {
            int ourHealthX = stateX.CurrentPlayer.Hero.Health + stateX.CurrentPlayer.Hero.Armor;
            int ourHealthY = stateY.CurrentPlayer.Hero.Health + stateY.CurrentPlayer.Hero.Armor;

            int hisHealthX = stateX.CurrentOpponent.Hero.Health + stateX.CurrentOpponent.Hero.Armor;
            int hisHealthY = stateY.CurrentOpponent.Hero.Health + stateY.CurrentOpponent.Hero.Armor;

            int ourAttackX = stateX.CurrentPlayer.Hero.AttackDamage;
            int ourAttackY = stateY.CurrentPlayer.Hero.AttackDamage;

            int hisAttackX = stateX.CurrentOpponent.Hero.AttackDamage;
            int hisAttackY = stateY.CurrentOpponent.Hero.AttackDamage;

            foreach (var card in stateX.CurrentPlayer.BoardZone)
            {
                ourAttackX += EstimateMinionAttack(card);
                if (card.HasTaunt)
                {
                    ourHealthX += card.Health;
                }
            }

            foreach (var card in stateY.CurrentPlayer.BoardZone)
            {
                ourAttackY += EstimateMinionAttack(card);
                if (card.HasTaunt)
                {
                    ourHealthY += card.Health;
                }
            }

            foreach (var card in stateX.CurrentOpponent.BoardZone)
            {
                hisAttackX += EstimateMinionAttack(card);
                if (card.HasTaunt)
                {
                    hisHealthX += card.Health;
                }
            }

            foreach (var card in stateY.CurrentOpponent.BoardZone)
            {
                hisAttackY += EstimateMinionAttack(card);
                if (card.HasTaunt)
                {
                    hisHealthY += card.Health;
                }
            }

            int ourHealthDiff = ourHealthX - ourHealthY;
            int hisHealthDiff = hisHealthX - hisHealthY;
            int ourHandSizeDiff = stateX.CurrentPlayer.HandZone.Count - stateY.CurrentPlayer.HandZone.Count;
            int ourTotalDamageDiff = ourAttackX - ourAttackY;
            int hisTotalDamageDiff = hisAttackX - hisAttackY;

            if (ourHealthDiff == 0 && hisHealthDiff == 0 && ourTotalDamageDiff == 0 && hisTotalDamageDiff == 0 && ourHandSizeDiff == 0)
            {
                return 0;
            }
            else if (ourHealthDiff > -2 * ourHandSizeDiff && hisHealthDiff == 0 && ourTotalDamageDiff == 0 && hisTotalDamageDiff == 0)
            {
                return ourHandSizeDiff;
            }
            else
            {
                var totalDiff = ourHealthDiff - hisHealthDiff + 1.5 * ourTotalDamageDiff - 1.5 * hisTotalDamageDiff;
                return totalDiff > 0 ? 1 : -1;
            }
        }

        static int EstimateMinionAttack(Minion minion)
        {
            return minion.AttackDamage * (minion.HasWindfury ? 2 : 1) * (minion.IsFrozen ? 0 : 1);
        }
    }
}
