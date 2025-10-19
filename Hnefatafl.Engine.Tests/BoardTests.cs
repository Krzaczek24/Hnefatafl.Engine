using Hnefatafl.Engine.Enums;
using Hnefatafl.Engine.Models;
using Hnefatafl.Engine.Models.Pawns;

namespace Hnefatafl.Engine.Tests
{
    public class BoardTests
    {
        [Test]
        public void IsEmptyBoardPopulatedWithFields()
        {
            // -- Arrange --
            var game = new Game();

            // -- Act --

            // -- Assert --
            Assert.Multiple(() =>
            {
                Assert.That(game.Board, Has.All.Not.Null);
                Assert.That(game.Board.Select(field => field.Pawn), Has.All.Null);
            });
        }

        [Test]
        public void IsResetBoardPopulatedWithPawns()
        {
            // -- Arrange --
            var game = new Game();

            // -- Act --
            game.Start();
            var pawns = game.Board.Where(field => !field.IsEmpty).Select(field => field.Pawn).ToList();

            // -- Assert --
            Assert.Multiple(() =>
            {
                Assert.That(game.Board, Has.All.Not.Null);
                Assert.That(pawns, Has.Exactly(24).TypeOf<Attacker>());
                Assert.That(pawns, Has.Exactly(12).TypeOf<Defender>());
                Assert.That(pawns, Has.Exactly(1).TypeOf<King>());
            });
        }

        [Test]
        public void CheckAvailableToMovePawns()
        {
            // -- Arrange --
            var game = new Game();

            // -- Act --
            game.Start();
            var pawnsAbleToMove = game.Board.GetPawns(Player.All, true).ToList();

            // -- Assert --
            Assert.Multiple(() =>
            {
                Assert.That(pawnsAbleToMove, Has.Exactly(20).TypeOf<Attacker>());
                Assert.That(pawnsAbleToMove, Has.Exactly(8).TypeOf<Defender>());
                Assert.That(pawnsAbleToMove, Has.Exactly(0).TypeOf<King>());
            });
        }
    }
}