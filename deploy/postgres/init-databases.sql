-- Database per service: every service owns its schema and connects with its own credentials.
-- (One PostgreSQL server is shared only to keep the local setup light.)

CREATE ROLE partners LOGIN PASSWORD 'partners';
CREATE DATABASE partners OWNER partners;

CREATE ROLE events LOGIN PASSWORD 'events';
CREATE DATABASE events OWNER events;

CREATE ROLE commissions LOGIN PASSWORD 'commissions';
CREATE DATABASE commissions OWNER commissions;

CREATE ROLE wallets LOGIN PASSWORD 'wallets';
CREATE DATABASE wallets OWNER wallets;

CREATE ROLE payouts LOGIN PASSWORD 'payouts';
CREATE DATABASE payouts OWNER payouts;
