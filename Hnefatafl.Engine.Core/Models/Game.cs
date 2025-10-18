using Hnefatafl.Engine.Enums;
using Hnefatafl.Engine.Models.Pawns;

namespace Hnefatafl.Engine.Models
{
    public class Game
    {
        public bool IsGameOver { get; private set; }

        public Board Board { get; }

        public Player CurrentPlayer { get; private set; } = Player.Attacker;

        public IEnumerable<Pawn> CurrentPlayerAvailablePawns => Board.GetPawns(CurrentPlayer).Where(Board.CanMove);

        public Game()
        {
            Board = new(this);
        }

        public void Start()
        {
            Board.Reset();
            CurrentPlayer = Player.Attacker;
            IsGameOver = false;
        }

        public MoveResult CanMakeMove(Pawn pawn, Field field)
        {
            if (!CurrentPlayerAvailablePawns.Contains(pawn))
                return MoveResult.NonCurrentPlayerPawn;

            if (field == pawn.Field)
                return MoveResult.PawnAlreadyHere;

            if (!Board.CanMove(pawn))
                return MoveResult.PawnCannotMove;

            if (pawn.Field.Coordinates.Column != field.Coordinates.Column
            && pawn.Field.Coordinates.Row != field.Coordinates.Row)
                return MoveResult.NotInLine;

            for (int column = pawn.Field.Coordinates.Column; column <= field.Coordinates.Column; column++)
                for (int row = pawn.Field.Coordinates.Row; row <= field.Coordinates.Row; row++)
                    if (Board[column, row].Pawn is not null && Board[column, row].Pawn != pawn)
                        return MoveResult.PathBlocked;

            return MoveResult.Success;
        }

        public MoveResult MakeMove(Pawn pawn, Field field)
        {
            MoveResult canMakeMoveResult = CanMakeMove(pawn, field);
            if (canMakeMoveResult is MoveResult.Success)
            {
                // make move here
                // check if fight or game over
            }
            return canMakeMoveResult;
        }

        public bool IsInLine(Pawn pawn, Coordinates target)
        {
            return pawn.Field.Coordinates.Column == target.Column
                || pawn.Field.Coordinates.Row == target.Row;
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
    }
}
