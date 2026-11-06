-- Insert the migration history record for the initial create migration
-- This marks the migration as already applied without actually running it

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES ('20240101000000_InitialCreate', '8.0.0');
