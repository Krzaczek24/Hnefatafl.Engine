using Hnefatafl.Engine.Enums;
using Hnefatafl.Engine.Models;
using Hnefatafl.Engine.Models.Pawns;
using KrzaqTools.Extensions;
using static Hnefatafl.Engine.Enums.MoveResult;

namespace Hnefatafl.Engine.Tests
{
    public class GameTests
    {
        [Test]
        public void MoveResult_Flags()
        {
            // --- Arrange ---
            const string SEPARATOR = " | ";
            var moveResults = Enum.GetValues<MoveResult>().WithIndex().Skip(1);
            int rowHeaderLength = moveResults.Count().ToString().Length + 2
                + moveResults.Select(x => x.Item.ToString().Length).Max();
            string Pad(string rowHeader) => rowHeader.PadRight(rowHeaderLength);

            // --- Act ---
            Console.WriteLine($"{Pad(string.Empty)}{SEPARATOR}{string.Join(SEPARATOR, moveResults.Select(x => x.Index))}{SEPARATOR}");
            foreach ((MoveResult moveResult, int index) in moveResults)
            {
                string rowHeader = $"{index}. {moveResult}";
                string body = string.Join(SEPARATOR, moveResults.Select(x => moveResult.HasFlag(x.Item) ? 'X' : ' '));
                Console.WriteLine($"{Pad(rowHeader)}{SEPARATOR}{body}{SEPARATOR}");
            }

            // --- Assert ---
            Assert.Pass();
        }


        [Test]
        public void StartingBoard_PopulatedWithFields()
        {
            // --- Arrange ---
            Game game = new();

            // --- Act ---

            // --- Assert ---
            Assert.That(game.Board, Has.All.Not.Null.And.All.TypeOf<Field>());
        }

        [Test]
        public void StartingBoard_PopulatedWithPawns()
        {
            // --- Arrange ---
            Game game = new();

            // --- Act ---
            var pawns = game.Board.Where(field => !field.IsEmpty).Select(field => field.Pawn).ToList();

            // --- Assert ---
            Assert.Multiple(() =>
            {
                Assert.That(pawns, Has.Exactly(24).TypeOf<Attacker>());
                Assert.That(pawns, Has.Exactly(12).TypeOf<Defender>());
                Assert.That(pawns, Has.Exactly(1).TypeOf<King>());
            });
        }

        [Test]
        public void MoveResult_NonCurrentPlayerPawn()
        {
            // --- Arrange ---
            Game game = new();

            // --- Act ---
            MoveValidationResult moveResult = game.CanMakeMove("f6", "f6");

            // --- Assert ---
            Assert.That(moveResult, Is.EqualTo(MoveValidationResult.NonCurrentPlayerPawn));
        }

        [Test]
        public void MoveResult_PawnAlreadyOnField()
        {
            // --- Arrange ---
            Game game = new();

            // --- Act ---
            MoveValidationResult moveResult = game.CanMakeMove("a4", "a4");

            // --- Assert ---
            Assert.That(moveResult, Is.EqualTo(MoveValidationResult.PawnAlreadyOnField));
        }

        [Test]
        public void MoveResult_PawnCannotMove()
        {
            // --- Arrange ---
            Game game = new();

            // --- Act ---
            MoveValidationResult moveResult = game.CanMakeMove("a6", "a4");

            // --- Assert ---
            Assert.That(moveResult, Is.EqualTo(MoveValidationResult.PawnCannotMove));
        }

        [Test]
        public void MoveResult_NotInLine()
        {
            // --- Arrange ---
            Game game = new();

            // --- Act ---
            MoveValidationResult moveResult = game.CanMakeMove("a4", "b3");

            // --- Assert ---
            Assert.That(moveResult, Is.EqualTo(MoveValidationResult.NotInLine));
        }

        [Test]
        public void MoveResult_PathBlocked()
        {
            // --- Arrange ---
            Game game = new();
            var moves = new (string from, string to)[]
            {
                ("a4", "f4"),
                ("a4", "h4"),
                ("d11", "d3"),
                ("k7", "f7")
            };

            // --- Act ---
            var results = moves.Select(move => game.CanMakeMove(move.from, move.to));

            // --- Assert ---
            Assert.That(results, Has.All.EqualTo(MoveValidationResult.PathBlocked));
        }

        [Test]
        public void StartingBoard_CheckAvailableToMovePawns()
        {
            // -- Arrange --
            Game game = new();

            // --- Act ---
            var pawnsAbleToMove = game.AllPawns.Where(game.Board.CanMove).ToList();

            // --- Assert ---
            Assert.Multiple(() =>
            {
                Assert.That(pawnsAbleToMove, Has.Exactly(20).TypeOf<Attacker>());
                Assert.That(pawnsAbleToMove, Has.Exactly(8).TypeOf<Defender>());
                Assert.That(pawnsAbleToMove, Has.Exactly(0).TypeOf<King>());
            });
        }

        [Test]
        public void EndGame_AllAttackerPawnsCaptured()
        {
            // --- Assert ---
            CheckGame(
                // ----- Attacker ----- // ----- Defender -----
                ("f10", "e10", PawnMoved), ("d6", "d10", PawnMoved),
                ("k4", "g4", PawnMoved), ("f8", "f10", AttackerPawnCaptured),
                ("k5", "h5", PawnMoved), ("f10", "i10", PawnMoved),
                ("a5", "d5", PawnMoved), ("i10", "i4", PawnMoved),
                ("g11", "g8", PawnMoved), ("i4", "h4", AttackerPawnCaptured), // x2
                ("e11", "e10", PawnMoved), ("f7", "f10", AttackerPawnCaptured),
                ("g1", "g4", PawnMoved), ("e6", "c6", PawnMoved),
                ("d11", "b11", PawnMoved), ("c6", "c11", AttackerPawnCaptured),
                ("g8", "f8", PawnMoved), ("g7", "g11", PawnMoved),
                ("f8", "f7", PawnMoved), ("f10", "f8", AttackerPawnCaptured),
                ("d5", "d9", PawnMoved), ("f8", "d8", AttackerPawnCaptured),
                ("b6", "e6", PawnMoved), ("e7", "e11", AttackerPawnCaptured),
                ("k7", "g7", PawnMoved), ("e11", "e7", AttackerPawnCaptured),
                ("a6", "e6", PawnMoved), ("d8", "d6", AttackerPawnCaptured),
                ("a7", "d7", PawnMoved), ("g11", "g8", AttackerPawnCaptured),
                ("a8", "f8", PawnMoved), ("d10", "d8", AttackerPawnCaptured),
                ("d1", "d5", PawnMoved), ("e7", "e8", AttackerPawnCaptured),
                ("h11", "h7", PawnMoved), ("f4", "d4", AttackerPawnCaptured),
                ("e1", "e4", PawnMoved), ("f5", "f4", AttackerPawnCaptured), // x2
                ("f1", "e1", PawnMoved), ("g8", "h8", AttackerPawnCaptured),
                ("h1", "g1", PawnMoved), ("g5", "k5", PawnMoved),
                ("e1", "e4", PawnMoved), ("f4", "f3", PawnMoved),
                ("g1", "g4", PawnMoved), ("f3", "f4", AttackerPawnCaptured), // x2
                ("a4", "a5", PawnMoved), ("g6", "g2", PawnMoved),
                ("a5", "d5", PawnMoved), ("c11", "c5", AttackerPawnCaptured),
                ("k8", "i8", PawnMoved), ("e5", "e2", AttackerPawnCaptured),
                ("i8", "i5", PawnMoved), ("d6", "d7", PawnMoved),
                ("j6", "i6", PawnMoved), ("d7", "k7", AttackerPawnCaptured),
                ("i5", "d5", PawnMoved), ("k5", "e5", AttackerPawnCaptured),
                ("i6", "k6", PawnMoved), ("e5", "k5", AllAttackerPawnsCaptured));
        }

        [Test]
        public void EndGame_KingCaptured()
        {
            // --- Assert ---
            CheckGame(
                // ----- Attacker ----- // ----- Defender -----
                ("e1", "e4", PawnMoved), ("f4", "j4", PawnMoved),
                ("g1", "g3", PawnMoved), ("f5", "f4", PawnMoved),
                ("d1", "d3", PawnMoved), ("f4", "i4", PawnMoved),
                ("d3", "e3", PawnMoved), ("f6", "f3", PawnMoved),
                ("e4", "f4", KingCaptured));
        }

        [Test]
        public void EndGame_KingEscape()
        {
            // --- Assert ---
            CheckGame(
                // ----- Attacker ----- // ----- Defender -----
                ("a4", "c4", PawnMoved), ("d6", "d2", PawnMoved),
                ("b6", "b11", PawnMoved), ("e6", "b6", PawnMoved),
                ("c4", "e4", PawnMoved), ("f6", "c6", PawnMoved),
                ("a5", "a2", PawnMoved), ("c6", "c1", PawnMoved),
                ("f10", "f9", PawnMoved), ("c1", "a1", KingEscaped)
            );
        }

        private static void CheckGame(params (string From, string To, MoveResult ExpectedMoveResult)[] moves)
        {
            // -- Arrange --
            GameOverReason? expectedGameOverReason = moves[^1].ExpectedMoveResult.AsGameOverReason();
            GameOverReason? actualGameOverReason = null;
            Side? expectedGameWinner = expectedGameOverReason?.AsWinner();
            Side? actualGameWinner = null;

            Game game = new();
            game.OnGameOver += (reason, winner) => { actualGameOverReason = reason; actualGameWinner = winner; };

            // -- Act --
            PlayGame(game, moves);

            // --- Assert ---
            Assert.Multiple(() =>
            {
                Assert.That(actualGameOverReason, Is.EqualTo(expectedGameOverReason));
                Assert.That(actualGameWinner, Is.EqualTo(expectedGameWinner));
            });
        }

        private static void PlayGame(Game game, params (string From, string To, MoveResult expectedMoveResult)[] moves)
        {
            foreach (var (from, to, expectedMoveResult) in moves)
                Assert.That(game.MakeMove(from, to), Is.EqualTo(expectedMoveResult));
        }
    }

    public static class TestGameExtensions
    {
        public static MoveResult MakeMove(this Game game, string from, string to)
        {
            (Field pawnField, Field targetField) = game.GetFields(from, to);
            return game.MakeMove(pawnField.Pawn!, targetField, out _);
        }

        public static MoveValidationResult CanMakeMove(this Game game, string from, string to)
        {
            (Field pawnField, Field targetField) = game.GetFields(from, to);
            return game.CanMakeMove(pawnField.Pawn!, targetField);
        }

        public static (Field From, Field To) GetFields(this Game game, string from, string to)
            => (game.Board[new(from)], game.Board[new(to)]);
    }
}