# Lancebound

Competitive mobile jousting with ranked duels, tournaments, progression, and short skill-based matches.

## First playable prototype

The repository targets Unity `6000.6.8f1`. Open it with Unity Hub, create or open an empty scene, and press Play. A runtime installer creates a zero-asset prototype containing:

- A three-pass jousting match
- Touch-drag lance aiming
- Timed bracing
- AI opponent accuracy and timing
- Lance breaks, unhorsing, scoring, and rematches
- Landscape mobile presentation at a 60 FPS target

The primitive geometry is intentional: it lets us validate whether the charge, aim, brace, and impact loop is fun before replacing it with production art.

## Controls

- Mobile: drag vertically on the right side to aim; tap **Brace** shortly before impact.
- Editor: drag vertically with the mouse to aim and press Space to brace.

## Project direction

Android is the first target, with iOS planned from the same Unity project. Live multiplayer, accounts, ranked matchmaking, tournaments, progression, and cosmetic inventory will be added around the deterministic match rules in `JoustMatch`.

