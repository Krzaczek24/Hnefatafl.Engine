using Hnefatafl.Engine.Enums;
using Hnefatafl.Engine.Models.Pawns;
using System.Collections.Frozen;
using System.ComponentModel;

namespace Hnefatafl.Engine.Models
{
    public class Game
    {
        // EVENTS OnGameOver OnPawnKilled etc ...
        public bool IsGameOver { get; private set; }

        public Board Board { get; }

        public Player CurrentPlayer { get; private set; } = Player.Attacker;

        public IEnumerable<Pawn> CurrentPlayerAvailablePawns => Board.GetPawns(CurrentPlayer, true);
        public IEnumerable<Pawn> AllPawns { get; private set; } = [];

        public Game()
        {
            Board = new(this);
        }

        public void Start()
        {
            Board.Reset();
            AllPawns = Board.GetPawns(Player.All, false).ToFrozenSet();
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

        public MoveResult MakeMove(Pawn pawn, Field field)
        {
            MoveValidationResult moveValidationResult = CanMakeMove(pawn, field);
            if (moveValidationResult is not MoveValidationResult.Success)
                return MoveResult.None;

            Board.MovePawn(pawn, field);
            MoveResult moveResult = MoveResult.PawnMoved;

            if (pawn is King && field.IsCorner)
                moveResult |= MoveResult.KingEscaped;

            foreach (Field adjacentField in Board.GetAdjacentFields(field).Where(IsOccupiedByOpponent))
                moveResult |= pawn is Attacker && adjacentField.Pawn is King king
                    ? CheckKingCapture(king)
                    : CheckPawnsCapture(pawn, adjacentField.Pawn!);

            if (moveResult is MoveResult.KingEscaped or MoveResult.KingKilled)
                EndGame();
            else
                SwapCurrentPlayer();

            return moveResult;

            bool IsOccupiedByOpponent(Field field) => !field.IsEmpty && field.Pawn!.Player != pawn.Player;
        }

        private MoveResult CheckKingCapture(King king)
        {
            var attackersCount = Board
                .GetAdjacentFields(king.Field)
                .Where(field => field.IsCenter || field.Pawn is Attacker)
                .Count();

            return attackersCount is 4
                ? MoveResult.KingKilled
                : MoveResult.None;
        }

        private MoveResult CheckPawnsCapture(Pawn movedPawn, Pawn attackedPawn)
        {
            var vector = attackedPawn.Field.Coordinates - movedPawn.Field.Coordinates;
            Coordinates coordinates = attackedPawn.Field.Coordinates + vector;
            if (!Board.AreValid(coordinates))
                return MoveResult.None;

            Field oppositeField = Board[coordinates];
            if (oppositeField.Pawn?.Player != movedPawn.Player && !oppositeField.IsCorner)
                return MoveResult.None;

            attackedPawn.Field.Pawn = null;
            return attackedPawn is Attacker
                ? MoveResult.AttackerPawnKilled
                : MoveResult.DefenderPawnKilled;
        }

        private void SwapCurrentPlayer()
        {
            CurrentPlayer = CurrentPlayer switch
            {
                Player.Attacker => Player.Defender,
                Player.Defender => Player.Attacker,
                _ => throw new InvalidOperationException(),
            };
        }

        private void EndGame()
        {
            IsGameOver = true;
            // print winner (current player)
        }
    }
}
