using System;

namespace Lancebound.Match
{
    public enum MatchPhase
    {
        Countdown,
        Charging,
        Impact,
        RoundResult,
        MatchResult
    }

    public enum HitZone
    {
        Miss,
        Shield,
        Torso,
        Helmet
    }

    [Serializable]
    public readonly struct ImpactInput
    {
        public readonly float AimAccuracy;
        public readonly float BraceAccuracy;
        public readonly float Speed;
        public readonly float DefenderBalance;
        public readonly HitZone HitZone;

        public ImpactInput(
            float aimAccuracy,
            float braceAccuracy,
            float speed,
            float defenderBalance,
            HitZone hitZone)
        {
            AimAccuracy = Clamp01(aimAccuracy);
            BraceAccuracy = Clamp01(braceAccuracy);
            Speed = Math.Max(0f, speed);
            DefenderBalance = Math.Max(1f, defenderBalance);
            HitZone = hitZone;
        }

        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }

    [Serializable]
    public readonly struct ImpactResult
    {
        public readonly float Force;
        public readonly int Points;
        public readonly bool LanceBroken;
        public readonly bool Unhorsed;

        public ImpactResult(float force, int points, bool lanceBroken, bool unhorsed)
        {
            Force = force;
            Points = points;
            LanceBroken = lanceBroken;
            Unhorsed = unhorsed;
        }
    }

    /// <summary>
    /// Deterministic match rules. This contains no Unity APIs so the same rules can later run
    /// on an authoritative multiplayer server.
    /// </summary>
    [Serializable]
    public sealed class JoustMatch
    {
        public const int MaxRounds = 3;

        public int Round { get; private set; } = 1;
        public int PlayerScore { get; private set; }
        public int OpponentScore { get; private set; }
        public MatchPhase Phase { get; private set; } = MatchPhase.Countdown;
        public ImpactResult LastPlayerImpact { get; private set; }
        public ImpactResult LastOpponentImpact { get; private set; }

        public void BeginCharge()
        {
            if (Phase != MatchPhase.Countdown && Phase != MatchPhase.RoundResult)
                throw new InvalidOperationException($"Cannot begin a charge during {Phase}.");

            Phase = MatchPhase.Charging;
        }

        public void ResolvePass(ImpactInput player, ImpactInput opponent, float variance = 1f)
        {
            if (Phase != MatchPhase.Charging)
                throw new InvalidOperationException($"Cannot resolve impact during {Phase}.");

            Phase = MatchPhase.Impact;
            LastPlayerImpact = ResolveImpact(player, variance);
            LastOpponentImpact = ResolveImpact(opponent, variance);
            PlayerScore += LastPlayerImpact.Points;
            OpponentScore += LastOpponentImpact.Points;
            Phase = Round >= MaxRounds || LastPlayerImpact.Unhorsed || LastOpponentImpact.Unhorsed
                ? MatchPhase.MatchResult
                : MatchPhase.RoundResult;
        }

        public void AdvanceRound()
        {
            if (Phase != MatchPhase.RoundResult)
                throw new InvalidOperationException($"Cannot advance round during {Phase}.");

            Round++;
            Phase = MatchPhase.Countdown;
        }

        public static ImpactResult ResolveImpact(ImpactInput input, float variance = 1f)
        {
            if (input.HitZone == HitZone.Miss)
                return new ImpactResult(0f, 0, false, false);

            variance = Math.Max(0.95f, Math.Min(1.05f, variance));
            var zoneMultiplier = input.HitZone switch
            {
                HitZone.Shield => 0.72f,
                HitZone.Torso => 1f,
                HitZone.Helmet => 1.12f,
                _ => 0f
            };

            var force = input.Speed
                        * (0.35f + input.AimAccuracy * 0.65f)
                        * (0.45f + input.BraceAccuracy * 0.55f)
                        * zoneMultiplier
                        * variance;

            var lanceBroken = force >= 8.5f;
            var unhorsed = force >= input.DefenderBalance;
            var points = unhorsed ? 5 : lanceBroken ? 3 : input.HitZone == HitZone.Shield ? 1 : 2;
            return new ImpactResult(force, points, lanceBroken, unhorsed);
        }
    }
}
