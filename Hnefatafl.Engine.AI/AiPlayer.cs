using Hnefatafl.Engine.Enums;
using Hnefatafl.Engine.Models;
using Hnefatafl.Engine.Models.Pawns;

namespace Hnefatafl.Engine.AI
{
    public static class AiPlayer
    {
        private static Random Random { get; } = new();

        public static void GetMove(Game game, out Pawn pawn, out Field field)
        {
            if (game.IsGameOver)
                throw new InvalidOperationException("Cannot make move: game is over.");

            (pawn, field) = FindBestMove(game) ?? (null!, null!);
        }

        private static (Pawn pawn, Field target)? FindBestMove(Game game)
        {
            var availablePawns = game.Board.GetPawns(game.CurrentSide).Where(game.Board.CanMove).ToArray();
            if (availablePawns.Length == 0)
                return null;

            long bestScore = long.MinValue;
            (Pawn pawn, Field target)? bestMove = null;

            foreach (var pawn in availablePawns)
            {
                var targets = game.Board.GetPawnAvailableFields(pawn);
                foreach (Field target in targets)
                {
                    Game clone = game.Clone();
                    Pawn simPawn = clone.Board[pawn.Field.Coordinates].Pawn!;
                    Field simTarget = clone.Board[target.Coordinates];
                    MoveResult simResult = clone.MakeMove(simPawn, simTarget, out _);
                    int score = ScoreMove(simResult, clone) * 10 + Random.Next(10);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = (pawn, target);
                    }
                }
            }

            return bestMove;
        }

        private static int ScoreMove(MoveResult result, Game simulatedGame)
        {
            // Big priorities: immediate game end
            if (result.IsGameOverMove())
                return 1_000_000;

            int score = 0;
            if (result.HasFlag(MoveResult.OpponentPawnCaptured))
                score += 200;
            if (result.HasFlag(MoveResult.PawnMoved))
                score += 1;

            // Mobility: prefer moves that increase AI available moves and reduce opponent's
            int myMobility = GetMobility(simulatedGame, simulatedGame.CurrentSide);
            int oppMobility = GetMobility(simulatedGame, simulatedGame.CurrentSide.GetOpponent());
            score += (myMobility - oppMobility) * 5;

            return score;

            int GetMobility(Game simulatedGame, Side side) => simulatedGame.Board.GetPawns(side).Where(simulatedGame.Board.CanMove).Count();
        }
    }
}
