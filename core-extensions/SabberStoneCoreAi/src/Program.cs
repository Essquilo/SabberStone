using System;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCoreAi.POGame;
using SabberStoneCoreAi.Agent.ExampleAgents;
using SabberStoneCoreAi.Agent;
using SabberStoneCoreAi.Meta;
using SabberStoneCoreAi.src.Agent;

namespace SabberStoneCoreAi
{
    internal class Program
    {
        private static GameConfig SHAMAN_SHAMAN = new GameConfig
        {
            StartPlayer = 1,
            Player1HeroClass = CardClass.SHAMAN,
            Player1Deck = Decks.MidrangeJadeShaman,
            Player2HeroClass = CardClass.SHAMAN,
            Player2Deck = Decks.MidrangeJadeShaman,
            FillDecks = true,
            Logging = false
        };

        private static GameConfig SHAMAN_MAGE = new GameConfig
        {
            StartPlayer = 1,
            Player1HeroClass = CardClass.SHAMAN,
            Player1Deck = Decks.MidrangeJadeShaman,
            Player2HeroClass = CardClass.MAGE,
            Player2Deck = Decks.RenoKazakusMage,
            FillDecks = true,
            Logging = false
        };

        private static GameConfig SHAMAN_WAR = new GameConfig
        {
            StartPlayer = 1,
            Player1HeroClass = CardClass.SHAMAN,
            Player1Deck = Decks.MidrangeJadeShaman,
            Player2HeroClass = CardClass.WARRIOR,
            Player2Deck = Decks.AggroPirateWarrior,
            FillDecks = true,
            Logging = false
        };

        private static GameConfig MAGE_SHAMAN = new GameConfig
        {
            StartPlayer = 1,
            Player1HeroClass = CardClass.MAGE,
            Player1Deck = Decks.RenoKazakusMage,
            Player2HeroClass = CardClass.SHAMAN,
            Player2Deck = Decks.MidrangeJadeShaman,
            FillDecks = true,
            Logging = false
        };

        private static GameConfig MAGE_MAGE = new GameConfig
        {
            StartPlayer = 1,
            Player1HeroClass = CardClass.MAGE,
            Player1Deck = Decks.RenoKazakusMage,
            Player2HeroClass = CardClass.MAGE,
            Player2Deck = Decks.RenoKazakusMage,
            FillDecks = true,
            Logging = false
        };

        private static GameConfig MAGE_WAR = new GameConfig
        {
            StartPlayer = 1,
            Player1HeroClass = CardClass.MAGE,
            Player1Deck = Decks.RenoKazakusMage,
            Player2HeroClass = CardClass.WARRIOR,
            Player2Deck = Decks.AggroPirateWarrior,
            FillDecks = true,
            Logging = false
        };

        private static GameConfig WAR_SHAMAN = new GameConfig
        {
            StartPlayer = 1,
            Player1HeroClass = CardClass.WARRIOR,
            Player1Deck = Decks.AggroPirateWarrior,
            Player2HeroClass = CardClass.SHAMAN,
            Player2Deck = Decks.MidrangeJadeShaman,
            FillDecks = true,
            Logging = false
        };

        private static GameConfig WAR_MAGE = new GameConfig
        {
            StartPlayer = 1,
            Player1HeroClass = CardClass.WARRIOR,
            Player1Deck = Decks.AggroPirateWarrior,
            Player2HeroClass = CardClass.MAGE,
            Player2Deck = Decks.RenoKazakusMage,
            FillDecks = true,
            Logging = false
        };

        private static GameConfig WAR_WAR = new GameConfig
        {
            StartPlayer = 1,
            Player1HeroClass = CardClass.WARRIOR,
            Player1Deck = Decks.AggroPirateWarrior,
            Player2HeroClass = CardClass.WARRIOR,
            Player2Deck = Decks.AggroPirateWarrior,
            FillDecks = true,
            Logging = false
        };

        private static void Main(string[] args)
        {

            Console.WriteLine("Setup gameConfig");

            //todo: rename to Main
            GameConfig[] gameConfig =  {
                SHAMAN_SHAMAN, SHAMAN_MAGE, SHAMAN_WAR,
                MAGE_SHAMAN, MAGE_MAGE, MAGE_WAR,
                WAR_SHAMAN, WAR_MAGE, WAR_WAR
            };

            foreach (var config in gameConfig)
            {
                foreach (AbstractAgent opponent in new AbstractAgent[] { new RandomAgent(), new RandomAgentLateEnd(), new FaceHunter() })
                {
                    Console.WriteLine("Playing {0} vs {1} against {2}", config.Player1HeroClass, config.Player2HeroClass, opponent.GetType());

                    AbstractAgent player1 = new SimpleBalanceAgentDepthCards();
                    var gameHandler = new POGameHandler(config, player1, opponent, debug: false);

                    //gameHandler.PlayGame();
                    gameHandler.PlayGames(100);
                    GameStats gameStats = gameHandler.getGameStats();

                    gameStats.printResults();


                    Console.WriteLine("Test successful");
                }
            }
            Console.ReadLine();
        }
    }
}
