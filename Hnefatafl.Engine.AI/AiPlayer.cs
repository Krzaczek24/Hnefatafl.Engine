using Hnefatafl.Engine.Enums;
using Hnefatafl.Engine.Models;
using Hnefatafl.Engine.Models.Pawns;

namespace Hnefatafl.Engine.AI
{
    public class AiPlayer(AiLevel level, Side side)
    {
        private Random Random { get; } = new();
        public Side Side { get; } = side;

        public void GetMove(Game game, out Pawn pawn, out Field field)
        {
            if (game.IsGameOver)
                throw new InvalidOperationException("Cannot make move: game is over.");

            if (game.CurrentSide != Side)
                throw new InvalidOperationException("Cannot make move: not AI's turn.");

            (pawn, field) = FindBestMove(game, Side, (int)level - 1, out _) ?? (null!, null!);
        }

        private (Pawn pawn, Field target)? FindBestMove(Game game, Side currentSide, int deepLevel, out long score)
        {
            score = 0;
            var availablePawns = game.Board.GetPawns(currentSide).Where(game.Board.CanMove).ToArray();
            if (availablePawns.Length == 0)
                return null;

            long bestScore = long.MinValue;
            (Pawn pawn, Field target)? bestMove = null;

            foreach (var pawn in availablePawns)
            {
                var targets = game.Board.GetPawnAvailableFields(pawn);
                foreach (Field target in targets)
                {
                    // simulate the move on a cloned game
                    Game clone = game.Clone();
                    Pawn simPawn = clone.Board[pawn.Field.Coordinates].Pawn!;
                    Field simTarget = clone.Board[target.Coordinates];
                    MoveResult simResult = clone.MakeMove(simPawn, simTarget, out _);
                    score = ScoreMove(simResult, clone, currentSide) * 10 + Random.Next(10);

                    if (deepLevel > 0)
                    {
                        _ = FindBestMove(clone, currentSide ^ Side.All, deepLevel - 1, out long nextStepScore);
                        score += currentSide == Side ? nextStepScore : -nextStepScore;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = (pawn, target);
                    }
                }
            }

            return bestMove;
        }

        private static long ScoreMove(MoveResult result, Game simulatedGame, Side currentSide)
        {
            // Big priorities: immediate game end
            if (result.IsGameOverMove())
                return 1_000_000;

            long score = 0;
            if (result.HasFlag(MoveResult.OpponentPawnCaptured))
                score += 200;
            if (result.HasFlag(MoveResult.PawnMoved))
                score += 1;

            // Mobility: prefer moves that increase AI available moves and reduce opponent's
            int myMobility = simulatedGame.Board.GetPawns(currentSide).Where(simulatedGame.Board.CanMove).Count();
            int oppMobility = simulatedGame.Board.GetPawns(currentSide ^ Side.All).Where(simulatedGame.Board.CanMove).Count();
            score += (myMobility - oppMobility) * 5;

            return score;
        }
    }
}
