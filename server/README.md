# Leaderboard Server

Tiny Express API that sits between the Unity client and a Neon (Postgres) database.

## First-time setup

1. **Create a Neon project** at <https://neon.tech>. Copy the connection string (it looks like `postgres://...@ep-xxx.neon.tech/neondb?sslmode=require`).
2. **Apply the schema.** Open the Neon SQL editor, paste the contents of `schema.sql`, run it.
3. **Configure env.** Copy `.env.example` to `.env` and fill in:
   - `DATABASE_URL` — the Neon connection string.
   - `WRITE_KEY` — any random string; Unity clients must send it in the `x-write-key` header to upsert.
   - `PORT` — defaults to `3000`.
4. **Install + run.**
   ```bash
   cd server
   npm install
   npm start
   ```
   You should see `leaderboard server on :3000`.

## Endpoints

### `POST /api/stats`
Header: `x-write-key: <WRITE_KEY>`
Body:
```json
{ "player_id": "uuid-string", "display_name": "Sayam", "money": 150, "happiness": 42 }
```
Response: the upserted row including computed `score`.

### `GET /api/leaderboard?limit=20`
Response:
```json
{ "entries": [{ "display_name": "Sayam", "money": 150, "happiness": 42, "score": 192, "rank": 1 }] }
```

### `GET /api/health`
Returns `{ "ok": true }`. Useful smoke test from the Unity editor menu.

## Deploying (for demos / multiplayer)

Any Node host works. Easiest free options:

- **Render** — New Web Service → connect the repo → root dir `server`, build `npm install`, start `npm start`. Add `DATABASE_URL` and `WRITE_KEY` in the env vars dashboard.
- **Railway** — `railway init` inside `server/`, set the same env vars, `railway up`.
- **Fly.io** — `fly launch` inside `server/`, pick a region, set secrets with `fly secrets set DATABASE_URL=... WRITE_KEY=...`.

After deploying, update Unity's `LeaderboardConfig.baseUrl` to the public URL.
