-- Add SessionId and PlaylistItemId columns to enable debugging of PlaybackId changes
-- These fields are used to generate the PlaybackId and will help identify when any of the source values change unexpectedly

ALTER TABLE PlaybackEntries ADD COLUMN SessionId TEXT NULL;
ALTER TABLE PlaybackEntries ADD COLUMN PlaylistItemId TEXT NULL;
