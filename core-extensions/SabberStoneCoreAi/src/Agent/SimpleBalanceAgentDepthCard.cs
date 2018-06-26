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
    class SimpleBalanceAgentDepthCards : SimpleBalanceAgent
    {
        private Queue<PlayerTask> Plan;
        protected override PlayerTask BestCard(List<PlayerTask> cards, POGame.POGame poGame)
        {
            if (cards.Count > 0)
            {
                if (Plan == null || Plan.Count == 0)
                {
                    Schedule(cards, poGame);
                }

                if (Plan == null || Plan.Count == 0)
                {
                    return null;
                }
                else
                {
                    var nextMove = Plan.Dequeue();
                    if (cards.Any(it => nextMove.Compare(it)))
                    {
                        return nextMove;
                    }
                    else
                    {
                        Plan = null;
                        Schedule(cards, poGame);
                    }
                }

            }
            return null;
        }

        private void Schedule(List<PlayerTask> cards, POGame.POGame poGame)
        {
            var simulation = DepthCardSearch(cards, poGame);
            var best = simulation.Where(it => it.Item2 != null).MaxElement(Comp, (it => it.Item2));
            if (best.Item1 != null && best.Item1.Count > 0 && Comp.Compare(best.Item2, poGame) >= 0)
            {
                Plan = new Queue<PlayerTask>(best.Item1);
            }
        }

        private List<Tuple<List<PlayerTask>, POGame.POGame>> DepthCardSearch(List<PlayerTask> cards, POGame.POGame poGame)
        {
            var result = new List<Tuple<List<PlayerTask>, POGame.POGame>>();
            result.Add(Tuple.Create(new List<PlayerTask>(), poGame));
            if (TurnStopwatch.Elapsed.TotalSeconds > 70)
            {
                var simulation = poGame.Simulate(cards)
                                 .Where(it => it.Value != null
                                              && Comp.Compare(it.Value, poGame) > 0);
                foreach (var (card, nextState) in simulation)
                {
                    var nextDive = DepthCardSearch(nextState.Cards(), nextState);
                    for (int i = 0; i < nextDive.Count; i++)
                    {
                        nextDive[i].Item1.Insert(0, card);
                    }
                    var best = nextDive.Where(it => it.Item2 != null).MaxElement(Comp, (it => it.Item2));
                    result.Add(best);
                }
            }
            return result;
        }


    }
}
