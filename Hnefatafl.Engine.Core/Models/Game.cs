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
        public GameOverReason? GameOverReason { get; private set; }

        public Board Board { get; }

        public Side CurrentSide { get; private set; } = Side.Attackers;

        public IEnumerable<Pawn> CurrentPlayerAvailablePawns => Board.GetPawns(CurrentSide).Where(Board.CanMove);
        public IEnumerable<Pawn> AllPawns { get; private set; } = [];
        public IEnumerable<Pawn> AttackerPawns => AllPawns.Where(pawn => pawn is Attacker);
        public IEnumerable<Pawn> DefenderPawns => AllPawns.Where(pawn => pawn is Defender);

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

        internal Game(Field[,] fields)
        {
            Board = new(this, fields);
            AllPawns = Board.GetPawns(Side.All).ToFrozenSet();
        }

        public void Restart()
        {
            Board.Reset();
            AllPawns = Board.GetPawns(Side.All).ToFrozenSet();
            CurrentSide = Side.Attackers;
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

            MoveList.Add((movingPawn.Field.Coordinates, targetField.Coordinates));
            Board.MovePawn(movingPawn, targetField);
            MoveResult moveResult = MoveResult.PawnMoved;

            if (movingPawn is King && targetField.IsCorner)
                moveResult |= MoveResult.KingEscaped;

            Parallel.ForEach(Board.GetAdjacentFields(targetField).Where(IsOccupiedByOpponent), adjacentField =>
            {
                if (adjacentField.Pawn is King attackedKing)
                {
                    if (IsKingCaptured(attackedKing, out var assistingFields))
                    {
                        moveResult |= MoveResult.KingCaptured;
                        OnPawnCaptured.Invoke(attackedKing, movingPawn, assistingFields);
                    }
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
            void SwapCurrentPlayer() => CurrentSide ^= Side.All;
            void EndGame(GameOverReason reason)
            {
                IsGameOver = true;
                GameOverReason = reason;
                OnGameOver.Invoke(reason, CurrentSide);
            }
        }

        public Game Clone()
        {
            Field[,] clonedTable = Board.GetEmptyTable();
            foreach (Field field in Board.Where(field => !field.IsEmpty))
            {
                var (row, column) = field.Coordinates;
                clonedTable[row, column].Pawn = Board[row, column].Pawn switch
                {
                    King => new King(clonedTable[row, column]),
                    Defender => new Defender(clonedTable[row, column]),
                    Attacker => new Attacker(clonedTable[row, column]),
                    _ => null
                };
            }
            return new(clonedTable) { CurrentSide = CurrentSide };
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
