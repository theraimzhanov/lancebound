using Lancebound.Match;
using NUnit.Framework;

namespace Lancebound.Tests
{
    public sealed class JoustMatchTests
    {
        [Test]
        public void MissScoresNothing()
        {
            var result = JoustMatch.ResolveImpact(new ImpactInput(1f, 1f, 18f, 13f, HitZone.Miss));
            Assert.That(result.Points, Is.Zero);
            Assert.That(result.Force, Is.Zero);
        }

        [Test]
        public void AccurateBracedHitCanUnhorseOpponent()
        {
            var result = JoustMatch.ResolveImpact(new ImpactInput(1f, 1f, 18f, 13f, HitZone.Torso));
            Assert.That(result.Unhorsed, Is.True);
            Assert.That(result.Points, Is.EqualTo(5));
        }

        [Test]
        public void MatchFinishesAfterThreePasses()
        {
            var match = new JoustMatch();
            var weakHit = new ImpactInput(0.2f, 0.2f, 6f, 100f, HitZone.Shield);

            for (var round = 1; round <= JoustMatch.MaxRounds; round++)
            {
                match.BeginCharge();
                match.ResolvePass(weakHit, weakHit);
                if (round < JoustMatch.MaxRounds)
                    match.AdvanceRound();
            }

            Assert.That(match.Phase, Is.EqualTo(MatchPhase.MatchResult));
            Assert.That(match.Round, Is.EqualTo(3));
        }
    }
}
