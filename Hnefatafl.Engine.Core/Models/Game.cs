using Hnefatafl.Engine.Enums;
using Hnefatafl.Engine.Events;
using Hnefatafl.Engine.Models.Pawns;
using System.Collections.Frozen;

namespace Hnefatafl.Engine.Models
{
    public class Game
    {
        public event GameOverEventHandler OnGameOver = delegate { };
        public event InvalidMoveEventHandler OnInvalidMove = delegate { };
        public event PawnCapturedEventHandler OnPawnCaptured = delegate { };

        public bool IsGameOver { get; private set; }

        public Board Board { get; }

        public Player CurrentPlayer { get; private set; } = Player.Attacker;

        public IReadOnlyCollection<Pawn> CurrentPlayerAvailablePawns => Board.GetPawns(CurrentPlayer).Where(Board.CanMove).ToList().AsReadOnly();
        public IReadOnlyCollection<Pawn> AllPawns { get; private set; } = [];
        public IReadOnlyCollection<Pawn> AttackerPawns => AllPawns.Where(pawn => pawn is Attacker).ToList().AsReadOnly();
        public IReadOnlyCollection<Pawn> DefenderPawns => AllPawns.Where(pawn => pawn is Defender).ToList().AsReadOnly();

        private List<(Coordinates From, Coordinates To)> MoveList { get; } = [];
        public IReadOnlyCollection<(Coordinates From, Coordinates To)> Moves => MoveList.AsReadOnly();
        public IEnumerable<Board> History
        {
            get
            {
                Game game = new();
                foreach (var (from, to) in Moves)
                {
                    game.MakeMove(game.Board[from].Pawn!, game.Board[to], out _);
                    yield return game.Board;
                }
            }
        }

        public Game()
        {
            Board = new(this);
            Restart();
        }

        public void Restart()
        {
            Board.Reset();
            AllPawns = Board.GetPawns(Player.All).ToFrozenSet();
            CurrentPlayer = Player.Attacker;
            IsGameOver = false;
        }

        public MoveValidationResult CanMakeMove(Pawn pawn, Field field)
        {
            if (!CurrentPlayerAvailablePawns.Contains(pawn))
                return MoveValidationResult.NonCurrentPlayerPawn;

            if (field == pawn.Field)
                return MoveValidationResult.PawnAlreadyHere;

            if (!Board.CanMove(pawn))
                return MoveValidationResult.PawnCannotMove;

            if (pawn.Field.Coordinates.Row != field.Coordinates.Row
            && pawn.Field.Coordinates.Column != field.Coordinates.Column)
                return MoveValidationResult.NotInLine;

            for (int row = pawn.Field.Coordinates.Row; row <= field.Coordinates.Row; row++)
                for (int column = pawn.Field.Coordinates.Column; column <= field.Coordinates.Column; column++)
                    if (!Board[row, column].IsEmpty && Board[row, column].Pawn != pawn)
                        return MoveValidationResult.PathBlocked;

            return MoveValidationResult.Success;
        }

        public MoveResult MakeMove(Pawn movingPawn, Field targetField, out MoveValidationResult moveValidationResult)
        {
            moveValidationResult = CanMakeMove(movingPawn, targetField);
            if (moveValidationResult is not MoveValidationResult.Success)
            {
                OnInvalidMove.Invoke(movingPawn, targetField, moveValidationResult);
                return MoveResult.None;
            }

            Board.MovePawn(movingPawn, targetField);
            MoveList.Add((movingPawn.Field.Coordinates, targetField.Coordinates));
            MoveResult moveResult = MoveResult.PawnMoved;

            if (movingPawn is King && targetField.IsCorner)
                moveResult |= MoveResult.KingEscaped;

            Parallel.ForEach(Board.GetAdjacentFields(targetField).Where(IsOccupiedByOpponent), adjacentField =>
            {
                if (movingPawn is Attacker
                && adjacentField.Pawn is King attackedKing
                && IsKingCaptured(attackedKing, out var assistingFields))
                {
                    moveResult |= MoveResult.KingCaptured;
                    OnPawnCaptured.Invoke(attackedKing, movingPawn, assistingFields);
                    return;
                }

                if (IsPawnCaptured(movingPawn, adjacentField.Pawn!, out Field assistingField))
                {
                    moveResult |= adjacentField.Pawn is Defender
                        ? MoveResult.DefenderPawnCaptured
                        : MoveResult.AttackerPawnCaptured;
                    OnPawnCaptured.Invoke(adjacentField.Pawn!, movingPawn, [assistingField]);
                }
            });

            if (!AttackerPawns.Where(pawn => pawn.Field is not null).Any())
                moveResult |= MoveResult.AllAttackerPawnsCaptured;

            if (moveResult.IsGameOverMove())
                EndGame(moveResult.AsGameOverReason()!.Value);
            else
                SwapCurrentPlayer();

            return moveResult;

            bool IsOccupiedByOpponent(Field field) => !field.IsEmpty && field.Pawn!.Player != movingPawn.Player;
            void SwapCurrentPlayer() => CurrentPlayer ^= Player.All;
            void EndGame(GameOverReason reason)
            {
                IsGameOver = true;
                OnGameOver.Invoke(reason, CurrentPlayer);
            }
        }

        private bool IsKingCaptured(King king, out IEnumerable<Field> assistingFields)
        {
            assistingFields = Board.GetAdjacentFields(king.Field).Where(field => field.IsCenter || field.Pawn is Attacker);
            return assistingFields.Count() is 4;
        }

        private bool IsPawnCaptured(Pawn movedPawn, Pawn attackedPawn, out Field assistingField)
        {
            assistingField = null!;

            var vector = attackedPawn.Field.Coordinates - movedPawn.Field.Coordinates;
            Coordinates coordinates = attackedPawn.Field.Coordinates + vector;
            if (!Board.AreValid(coordinates))
                return false;

            assistingField = Board[coordinates];
            if (assistingField.Pawn?.Player != movedPawn.Player && !assistingField.IsCorner)
                return false;

            attackedPawn.Field.Pawn = null;
            attackedPawn.Field = null!;
            return true;
        }
    }
}
