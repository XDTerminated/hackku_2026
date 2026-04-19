import 'dotenv/config';
import express from 'express';
import { neon } from '@neondatabase/serverless';

const PORT = parseInt(process.env.PORT ?? '3000', 10);
const WRITE_KEY = process.env.WRITE_KEY ?? '';

if (!process.env.DATABASE_URL) {
    console.error('DATABASE_URL is not set. Copy .env.example to .env and fill it in.');
    process.exit(1);
}

const sql = neon(process.env.DATABASE_URL);
const app = express();

app.use(express.json({ limit: '64kb' }));

app.use((req, res, next) => {
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Headers', 'content-type, x-write-key');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    if (req.method === 'OPTIONS') return res.sendStatus(204);
    next();
});

app.get('/api/health', (_req, res) => res.json({ ok: true }));

app.post('/api/stats', async (req, res) => {
    if (WRITE_KEY && req.get('x-write-key') !== WRITE_KEY) {
        return res.status(401).json({ error: 'invalid write key' });
    }

    const {
        player_id,
        display_name,
        money,
        happiness,
        hunger,
        hygiene,
        debt,
        starting_debt,
        invested,
        year,
        composite_score,
    } = req.body ?? {};

    if (!isUuid(player_id)) {
        return res.status(400).json({ error: 'player_id must be a uuid' });
    }
    if (typeof display_name !== 'string' || display_name.trim().length === 0 || display_name.length > 64) {
        return res.status(400).json({ error: 'display_name required (1-64 chars)' });
    }

    const ints = { money, happiness, hunger, hygiene, debt, starting_debt, invested, year, composite_score };
    for (const [k, v] of Object.entries(ints)) {
        if (!Number.isInteger(v)) return res.status(400).json({ error: `${k} must be an integer` });
    }

    try {
        const rows = await sql`
            insert into players (
                player_id, display_name,
                money, happiness, hunger, hygiene,
                debt, starting_debt, invested, year,
                composite_score, updated_at
            )
            values (
                ${player_id}, ${display_name.trim()},
                ${money}, ${happiness}, ${hunger}, ${hygiene},
                ${debt}, ${starting_debt}, ${invested}, ${year},
                ${composite_score}, now()
            )
            on conflict (player_id) do update
                set display_name    = excluded.display_name,
                    money           = excluded.money,
                    happiness       = excluded.happiness,
                    hunger          = excluded.hunger,
                    hygiene         = excluded.hygiene,
                    debt            = excluded.debt,
                    starting_debt   = excluded.starting_debt,
                    invested        = excluded.invested,
                    year            = excluded.year,
                    composite_score = excluded.composite_score,
                    updated_at      = now()
            returning player_id, display_name, money, happiness, hunger, hygiene,
                      debt, starting_debt, invested, year, composite_score
        `;
        res.json(rows[0]);
    } catch (err) {
        console.error('[stats] ', err);
        res.status(500).json({ error: String(err?.message ?? err) });
    }
});

app.get('/api/leaderboard', async (req, res) => {
    const parsed = parseInt(String(req.query.limit ?? '20'), 10);
    const limit = Math.max(1, Math.min(Number.isFinite(parsed) ? parsed : 20, 100));
    try {
        const rows = await sql`
            select display_name, money, happiness, hunger, hygiene,
                   debt, starting_debt, invested, year, composite_score,
                   rank() over (order by composite_score desc) as rank
            from players
            order by composite_score desc, updated_at asc
            limit ${limit}
        `;
        res.json({ entries: rows });
    } catch (err) {
        console.error('[leaderboard] ', err);
        res.status(500).json({ error: String(err?.message ?? err) });
    }
});

app.listen(PORT, () => console.log(`leaderboard server on :${PORT}`));

function isUuid(value) {
    return typeof value === 'string' &&
        /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
}
