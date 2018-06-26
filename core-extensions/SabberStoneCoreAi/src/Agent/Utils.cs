using System;
using System.Collections.Generic;
using System.Linq;
using SabberStoneCore.Tasks;
using SabberStoneCoreAi.POGame;

namespace Agent.Utils
{
    public struct GamePath
    {
        public List<PlayerTask> Path;
        public double Cost;

        internal GamePath(List<PlayerTask> Path, double Cost)
        {
            this.Path = Path;
            this.Cost = Cost;
        }
    }
    public static class Utils
    {
        public static T MaxElement<T>(this IEnumerable<T> it, Func<T, IComparable> selector)
        {
            T result = default(T);
            bool isEmpty = true;
            foreach (T elem in it)
            {
                if (isEmpty || selector(result).CompareTo(selector(elem)) < 0)
                {
                    result = elem;
                }
                isEmpty = false;
            }
            return result;
        }

        public static T MaxElement<T>(this IEnumerable<T> it, Comparer<T> selector)
        {
            T result = default(T);
            bool isEmpty = true;
            foreach (T elem in it)
            {
                if (isEmpty || selector.Compare(result, elem) < 0)
                {
                    result = elem;
                }
                isEmpty = false;
            }
            return result;
        }

        public static T MaxElement<T, K>(this IEnumerable<T> it, IComparer<K> selector, Func<T, K> key)
        {
            T result = default(T);
            bool isEmpty = true;
            foreach (T elem in it)
            {
                if (isEmpty || selector.Compare(key(result), key(elem)) < 0)
                {
                    result = elem;
                }
                isEmpty = false;
            }
            return result;
        }

        internal static List<PlayerTask> Cards(this SabberStoneCoreAi.POGame.POGame game)
        {
            return game.CurrentPlayer.Options()
                   .Where(it => it.PlayerTaskType == PlayerTaskType.HERO_POWER
                                || it.PlayerTaskType == PlayerTaskType.PLAY_CARD).ToList();
        }


        public static bool Compare(this PlayerTask task1, PlayerTask task2)
        {
            return task1.FullPrint().Equals(task2.FullPrint());
        }
    }
}