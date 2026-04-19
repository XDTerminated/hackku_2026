-- Run this once against your Neon database (SQL editor in the Neon console).
-- Safe to re-run: it drops the old players table first.

drop table if exists players;

create table players (
    player_id       uuid primary key,
    display_name    text not null,
    money           integer not null default 0,
    happiness       integer not null default 0,
    hunger          integer not null default 0,
    hygiene         integer not null default 0,
    debt            integer not null default 0,
    starting_debt   integer not null default 0,
    invested        integer not null default 0,
    year            integer not null default 0,
    composite_score integer not null default 0,
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now()
);

create index players_composite_idx on players (composite_score desc);
create index players_updated_at_idx on players (updated_at desc);
