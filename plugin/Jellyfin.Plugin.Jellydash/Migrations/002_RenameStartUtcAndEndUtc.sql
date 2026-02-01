-- Rename StartUtc/EndUtc to StartTime/EndTime with ISO 8601 format
-- SQLite doesn't support ALTER COLUMN, so we recreate the table

-- Step 1: Create new table with StartTime/EndTime columns (StartTime is NOT NULL)
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
    TranscodeCompletionPercentage REAL
);

-- Step 2: Copy data with timestamp transformation
-- Convert "2026-01-31 09:28:35.4632879" to "2026-01-31T09:28:35.4632879+00:00"
INSERT INTO PlaybackEntries_new SELECT
    Id, PlaybackId, ItemId, ParentItemId, ContentType, Title, Genres, Year,
    SeriesName, SeasonNumber, EpisodeNumber, UserId, UserName, UserPrimaryImageTag,
    ClientName, DeviceName, DeviceId,
    CASE
        WHEN StartUtc LIKE '%+%' OR StartUtc LIKE '%-%' THEN StartUtc
        ELSE REPLACE(StartUtc, ' ', 'T') || '+00:00'
    END AS StartTime,
    CASE
        WHEN EndUtc IS NULL THEN NULL
        WHEN EndUtc LIKE '%+%' OR EndUtc LIKE '%-%' THEN EndUtc
        ELSE REPLACE(EndUtc, ' ', 'T') || '+00:00'
    END AS EndTime,
    RuntimeTicks, StartPositionTicks, EndPositionTicks, IsCompleted, IsPaused,
    VideoCodec, VideoContainer, VideoRange, VideoBitrate, VideoBitDepth,
    VideoHeight, VideoWidth, AudioLanguage, AudioCodec, AudioLayout,
    AudioBitrate, AudioSampleRate, SubtitleIsForced, SubtitleIsHearingImpaired,
    SubtitleCodec, SubtitleLanguage, IsVideoDirect, IsAudioDirect,
    TranscodeBitrate, HardwareAcceleration, TranscodedVideoContainer,
    TranscodedVideoCodec, TranscodedAudioCodec, TranscodeReasonsJson,
    TranscodeCompletionPercentage
FROM PlaybackEntries;

-- Step 3: Replace old table with new one
DROP TABLE PlaybackEntries;
ALTER TABLE PlaybackEntries_new RENAME TO PlaybackEntries;

CREATE UNIQUE INDEX IF NOT EXISTS IX_PlaybackEntries_PlaybackId
    ON PlaybackEntries (PlaybackId)
    WHERE PlaybackId IS NOT NULL AND IsCompleted = 0;
