-- Add ItemImageHash column for storing cached image hashes
ALTER TABLE PlaybackEntries ADD COLUMN ItemImageHash TEXT NULL;

-- Create index for deduplication queries
CREATE INDEX IF NOT EXISTS IX_PlaybackEntries_ItemImageHash
    ON PlaybackEntries (ItemImageHash)
    WHERE ItemImageHash IS NOT NULL;
