using System;
using System.Diagnostics;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks;
using SabberStoneCoreAi.Agent;

namespace SabberStoneCoreAi.POGame
{
    class POGameHandler
    {
        private bool debug;

        private AbstractAgent player1;
        private AbstractAgent player2;

        private GameConfig gameConfig;
        private bool setupHeroes = true;

        private GameStats gameStats;
        private static readonly Random Rnd = new Random();


        public POGameHandler(GameConfig gameConfig, AbstractAgent player1, AbstractAgent player2, bool setupHeroes = true, bool debug = false)
        {
            this.gameConfig = gameConfig;
            this.setupHeroes = setupHeroes;
            this.player1 = player1;
            player1.InitializeAgent();

            this.player2 = player2;
            player2.InitializeAgent();

            gameStats = new GameStats();
            this.debug = debug;
        }

        public bool PlayGame(bool addToGameStats = true)
        {
            Game game = new Game(gameConfig, setupHeroes);
            player1.InitializeGame();
            player2.InitializeGame();

            AbstractAgent currentAgent;
            Stopwatch currentStopwatch;
            POGame poGame;
            PlayerTask playertask = null;
            Stopwatch[] watches = new[] { new Stopwatch(), new Stopwatch() };
            var cursorPosition = Console.CursorLeft;

            game.StartGame();
            try
            {
                while (game.State != State.COMPLETE && game.State != State.INVALID)
                {
                    var player = game.CurrentPlayer == game.Player1 ? "Player1" : "Player2";
                    Controller currentPlayer = game.CurrentPlayer;

                    if (debug)
                    {
                        Console.WriteLine("---------------------------------------------------");
                        Console.WriteLine("Turn {0}. {1}:[{2}/{3}]", game.Turn, player, currentPlayer.RemainingMana, currentPlayer.BaseMana);
                    }
                    else
                    {
                        Console.CursorLeft = cursorPosition;
                        Console.Write(new string(' ', Console.BufferWidth - (cursorPosition + 1)));
                        Console.CursorLeft = cursorPosition;
                        Console.Write(" Turn {0}. {1}:[{2}/{3}]", game.Turn, player, currentPlayer.RemainingMana, currentPlayer.BaseMana);
                    }

                    currentAgent = game.CurrentPlayer == game.Player1 ? player1 : player2;
                    currentStopwatch = game.CurrentPlayer == game.Player1 ? watches[0] : watches[1];
                    poGame = new POGame(game, debug);

                    var turnWatch = new Stopwatch();
                    turnWatch.Start();
                    currentStopwatch.Start();
                    playertask = currentAgent.GetMove(poGame);
                    currentStopwatch.Stop();
                    turnWatch.Stop();
                    if (debug)
                    {
                        Console.WriteLine("Elapsed seconds for {0} : {1}", player, turnWatch.Elapsed.TotalSeconds);
                    }
                    game.CurrentPlayer.Game = game;
                    game.CurrentOpponent.Game = game;

                    if (debug)
                    {
                        Console.WriteLine(playertask.FullPrint());
                    }
                    game.Process(playertask);
                }
            }
            catch (Exception e)
            //Current Player loses if he throws an exception
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                game.State = State.COMPLETE;
                game.CurrentPlayer.PlayState = PlayState.CONCEDED;
                game.CurrentOpponent.PlayState = PlayState.WON;

                if (addToGameStats && game.State != State.INVALID)
                    gameStats.registerException(game, e);
            }

            if (game.State == State.INVALID)
                return false;

            if (addToGameStats)
                gameStats.addGame(game, watches);

            player1.FinalizeGame();
            player2.FinalizeGame();
            return true;
        }

        public void PlayGames(int nr_of_games, bool addToGameStats = true)
        {
            for (int i = 0; i < nr_of_games; i++)
            {
                if (debug)
                {
                    Console.WriteLine("Playing game {0}", i);
                }
                else
                {
                    Console.Write("\rPlaying game {0}", i);
                }
                if (!PlayGame(addToGameStats))
                    i -= 1;     // invalid game
            }
            Console.WriteLine();
        }

        public GameStats getGameStats()
        {
            return gameStats;
        }
    }

}
