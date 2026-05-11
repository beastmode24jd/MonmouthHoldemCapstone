using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Background;
using NUnit.Framework;

namespace MH.Capstone.Domain.Tests.Unit.Services.Background
{
    [TestFixture]
    [Parallelizable]
    [ExcludeFromCodeCoverage]
    public class LeaderboardChangeWatcherTests
    {
        private static ApplicationUser User(string id, string name, int points) =>
            new() { Id = id, DisplayName = name, Points = points };

        [Test]
        public void DetectChanges_EmptyCurrentAndEmptyPrevious_ReturnsNoUpdates()
        {
            var result = LeaderboardChangeWatcher
                .DetectChanges(new List<ApplicationUser>(), new Dictionary<string, int>())
                .ToList();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void DetectChanges_NewUserNotInPrevious_EmitsUpdate()
        {
            var current = new List<ApplicationUser> { User("u1", "Alex", 100) };
            var previous = new Dictionary<string, int>();

            var result = LeaderboardChangeWatcher.DetectChanges(current, previous).ToList();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].UserId, Is.EqualTo("u1"));
            Assert.That(result[0].Points, Is.EqualTo(100));
            Assert.That(result[0].Rank, Is.EqualTo(1));
        }

        [Test]
        public void DetectChanges_PointsUnchanged_EmitsNoUpdate()
        {
            var current = new List<ApplicationUser> { User("u1", "Alex", 100) };
            var previous = new Dictionary<string, int> { ["u1"] = 100 };

            var result = LeaderboardChangeWatcher.DetectChanges(current, previous).ToList();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void DetectChanges_PointsChanged_EmitsUpdateWithNewValue()
        {
            var current = new List<ApplicationUser> { User("u1", "Alex", 150) };
            var previous = new Dictionary<string, int> { ["u1"] = 100 };

            var result = LeaderboardChangeWatcher.DetectChanges(current, previous).ToList();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Points, Is.EqualTo(150));
        }

        [Test]
        public void DetectChanges_MixedChanges_EmitsOnlyChangedAndNewEntries()
        {
            var current = new List<ApplicationUser>
            {
                User("u1", "Alex",     150), // changed
                User("u2", "Patricia", 100), // unchanged
                User("u3", "Lily",      80)  // new
            };
            var previous = new Dictionary<string, int>
            {
                ["u1"] = 100,
                ["u2"] = 100
            };

            var result = LeaderboardChangeWatcher.DetectChanges(current, previous).ToList();

            Assert.That(result.Select(r => r.UserId), Is.EquivalentTo(new[] { "u1", "u3" }));
        }

        [Test]
        public void DetectChanges_RankReflectsPositionInCurrentList()
        {
            var current = new List<ApplicationUser>
            {
                User("u1", "Alex",     150),
                User("u2", "Patricia", 100),
                User("u3", "Lily",      80)
            };
            var previous = new Dictionary<string, int>();

            var result = LeaderboardChangeWatcher.DetectChanges(current, previous).ToList();

            Assert.That(result.Single(r => r.UserId == "u1").Rank, Is.EqualTo(1));
            Assert.That(result.Single(r => r.UserId == "u2").Rank, Is.EqualTo(2));
            Assert.That(result.Single(r => r.UserId == "u3").Rank, Is.EqualTo(3));
        }

        [Test]
        public void DetectRankChanges_NoPreviousRanks_ReturnsNoChanges()
        {
            var current = new List<ApplicationUser> { User("u1", "Alex", 100) };
            var previousRanks = new Dictionary<string, int>();

            var result = LeaderboardChangeWatcher.DetectRankChanges(current, previousRanks).ToList();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void DetectRankChanges_RankUnchanged_EmitsNothing()
        {
            var current = new List<ApplicationUser> { User("u1", "Alex", 150) };
            var previousRanks = new Dictionary<string, int> { ["u1"] = 1 };

            var result = LeaderboardChangeWatcher.DetectRankChanges(current, previousRanks).ToList();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void DetectRankChanges_UserMovedUp_EmitsChangeWithBothRanks()
        {
            // Patricia was rank 2; she's now rank 1 (passed Alex)
            var current = new List<ApplicationUser>
            {
                User("u2", "Patricia", 200),
                User("u1", "Alex",     150)
            };
            var previousRanks = new Dictionary<string, int>
            {
                ["u1"] = 1,
                ["u2"] = 2
            };

            var result = LeaderboardChangeWatcher.DetectRankChanges(current, previousRanks).ToList();

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Single(r => r.UserId == "u2").OldRank, Is.EqualTo(2));
            Assert.That(result.Single(r => r.UserId == "u2").NewRank, Is.EqualTo(1));
            Assert.That(result.Single(r => r.UserId == "u1").OldRank, Is.EqualTo(1));
            Assert.That(result.Single(r => r.UserId == "u1").NewRank, Is.EqualTo(2));
        }

        [Test]
        public void DetectRankChanges_NewEntrant_NotInPreviousMap_IsExcluded()
        {
            var current = new List<ApplicationUser> { User("uNew", "Newbie", 100) };
            var previousRanks = new Dictionary<string, int>(); // Newbie wasn't seen last tick

            var result = LeaderboardChangeWatcher.DetectRankChanges(current, previousRanks).ToList();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void DetectRankChanges_TopN_ExcludesShufflesEntirelyOutsideWindow()
        {
            // u3 and u4 swap ranks 3↔4; with topN=2, neither change should be emitted.
            var current = new List<ApplicationUser>
            {
                User("u1", "User1", 300), // rank 1 (unchanged)
                User("u2", "User2", 200), // rank 2 (unchanged)
                User("u4", "User4", 100), // rank 3 (was 4)
                User("u3", "User3", 90)   // rank 4 (was 3)
            };
            var previousRanks = new Dictionary<string, int>
            {
                ["u1"] = 1, ["u2"] = 2, ["u3"] = 3, ["u4"] = 4
            };

            var result = LeaderboardChangeWatcher.DetectRankChanges(current, previousRanks, topN: 2).ToList();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void DetectRankChanges_TopN_IncludesUserWhoDroppedOutOfWindow()
        {
            // u2 was rank 2 (in top-2), now rank 3 (out). Should still emit because oldRank ≤ topN.
            var current = new List<ApplicationUser>
            {
                User("u1", "User1", 300), // rank 1
                User("u3", "User3", 250), // rank 2 (was 3, moved up)
                User("u2", "User2", 200)  // rank 3 (was 2, dropped)
            };
            var previousRanks = new Dictionary<string, int>
            {
                ["u1"] = 1, ["u2"] = 2, ["u3"] = 3
            };

            var result = LeaderboardChangeWatcher.DetectRankChanges(current, previousRanks, topN: 2).ToList();

            Assert.That(result.Single(r => r.UserId == "u2").OldRank, Is.EqualTo(2));
            Assert.That(result.Single(r => r.UserId == "u2").NewRank, Is.EqualTo(3));
            Assert.That(result.Any(r => r.UserId == "u3"), Is.True); // moved into window
        }
    }
}
