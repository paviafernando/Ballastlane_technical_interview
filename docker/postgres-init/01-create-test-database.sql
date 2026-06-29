-- Creates a separate database for running integration tests against a real
-- Postgres instance, so test runs never touch dev/demo seeded data.
CREATE DATABASE task_management_system_test;
