-- Add UpdatedAt column to track when entries are last modified
-- SQLite doesn't support non-constant defaults in ALTER TABLE, so we recreate the table

-- Step 1: Create new table with UpdatedAt column
CREATE TABLE PlaybackEntries_new (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PlaybackId TEXT NOT NULL,
    ItemId TEXT,
    ParentItemId TEXT,
    ContentType INTEGER NOT NULL,
    Title TEXT NOT NULL,
    Genres TEXT,
    Year INTEGER,
    SeriesName TEXT,
    SeasonNumber INTEGER,
    EpisodeNumber INTEGER,
    UserId TEXT NOT NULL,
    UserName TEXT NOT NULL,
    UserPrimaryImageTag TEXT,
    ClientName TEXT NOT NULL,
    DeviceName TEXT NOT NULL,
    DeviceId TEXT,
    StartTime TEXT NOT NULL,
    EndTime TEXT,
    RuntimeTicks INTEGER,
    StartPositionTicks INTEGER,
    EndPositionTicks INTEGER,
    IsCompleted INTEGER NOT NULL,
    IsPaused INTEGER NOT NULL,
    VideoCodec TEXT,
    VideoContainer TEXT,
    VideoRange TEXT,
    VideoBitrate INTEGER,
    VideoBitDepth INTEGER,
    VideoHeight INTEGER,
    VideoWidth INTEGER,
    AudioLanguage TEXT,
    AudioCodec TEXT,
    AudioLayout TEXT,
    AudioBitrate INTEGER,
    AudioSampleRate INTEGER,
    SubtitleIsForced INTEGER,
    SubtitleIsHearingImpaired INTEGER,
    SubtitleCodec TEXT,
    SubtitleLanguage TEXT,
    IsVideoDirect INTEGER NOT NULL,
    IsAudioDirect INTEGER NOT NULL,
    TranscodeBitrate INTEGER,
    HardwareAcceleration TEXT,
    TranscodedVideoContainer TEXT,
    TranscodedVideoCodec TEXT,
    TranscodedAudioCodec TEXT,
    TranscodeReasonsJson TEXT,
    TranscodeCompletionPercentage REAL,
    ItemImageHash TEXT,
    SessionId TEXT,
    PlaylistItemId TEXT,
    UpdatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%f+00:00', 'now'))
);

-- Step 2: Copy data with UpdatedAt backfilled from EndTime or StartTime
INSERT INTO PlaybackEntries_new SELECT
    Id, PlaybackId, ItemId, ParentItemId, ContentType, Title, Genres, Year,
    SeriesName, SeasonNumber, EpisodeNumber, UserId, UserName, UserPrimaryImageTag,
    ClientName, DeviceName, DeviceId, StartTime, EndTime,
    RuntimeTicks, StartPositionTicks, EndPositionTicks, IsCompleted, IsPaused,
    VideoCodec, VideoContainer, VideoRange, VideoBitrate, VideoBitDepth,
    VideoHeight, VideoWidth, AudioLanguage, AudioCodec, AudioLayout,
    AudioBitrate, AudioSampleRate, SubtitleIsForced, SubtitleIsHearingImpaired,
    SubtitleCodec, SubtitleLanguage, IsVideoDirect, IsAudioDirect,
    TranscodeBitrate, HardwareAcceleration, TranscodedVideoContainer,
    TranscodedVideoCodec, TranscodedAudioCodec, TranscodeReasonsJson,
    TranscodeCompletionPercentage, ItemImageHash, SessionId, PlaylistItemId,
    COALESCE(EndTime, StartTime) AS UpdatedAt
FROM PlaybackEntries;

-- Step 3: Replace old table with new one
DROP TABLE PlaybackEntries;
ALTER TABLE PlaybackEntries_new RENAME TO PlaybackEntries;

-- Step 4: Recreate indexes
CREATE UNIQUE INDEX IF NOT EXISTS IX_PlaybackEntries_PlaybackId
    ON PlaybackEntries (PlaybackId)
    WHERE PlaybackId IS NOT NULL AND IsCompleted = 0;

-- Step 5: Create trigger to automatically update UpdatedAt on record updates
CREATE TRIGGER IF NOT EXISTS UpdatePlaybackEntriesTimestamp
AFTER UPDATE ON PlaybackEntries
FOR EACH ROW
BEGIN
    UPDATE PlaybackEntries SET UpdatedAt = strftime('%Y-%m-%dT%H:%M:%f+00:00', 'now') WHERE Id = NEW.Id;
END;
