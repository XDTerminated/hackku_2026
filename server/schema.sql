-- Run this once against your Neon database (SQL editor in the Neon console).

create table if not exists players (
    player_id     uuid primary key,
    display_name  text not null,
    money         integer not null default 0,
    happiness     integer not null default 0,
    score         integer generated always as (money + happiness) stored,
    created_at    timestamptz not null default now(),
    updated_at    timestamptz not null default now()
);

create index if not exists players_score_idx on players (score desc);
create index if not exists players_updated_at_idx on players (updated_at desc);
