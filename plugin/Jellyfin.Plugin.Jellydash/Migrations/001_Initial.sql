CREATE TABLE IF NOT EXISTS PlaybackEntries (
    Id                          INTEGER PRIMARY KEY AUTOINCREMENT,
    PlaybackId                  TEXT    NOT NULL,

    -- Identity
    ItemId                      TEXT    NULL,
    ParentItemId                TEXT    NULL,
    ContentKind                 INTEGER NOT NULL,
    Title                       TEXT    NOT NULL,
    Genres                      TEXT    NULL,
    Year                        INTEGER NULL,
    SeriesName                  TEXT    NULL,
    SeasonNumber                INTEGER NULL,
    EpisodeNumber               INTEGER NULL,

    -- User
    UserId                      TEXT    NOT NULL,
    UserName                    TEXT    NOT NULL,

    -- Client
    ClientName                  TEXT    NOT NULL,
    DeviceName                  TEXT    NOT NULL,
    DeviceId                    TEXT    NULL,

    -- Timing / progress
    StartUtc                    TEXT    NOT NULL,
    EndUtc                      TEXT    NULL,
    RuntimeTicks                INTEGER NULL,
    StartPositionTicks          INTEGER NULL,
    EndPositionTicks            INTEGER NULL,
    IsCompleted                 BOOLEAN NOT NULL,
    IsPaused                    BOOLEAN NOT NULL,

    -- Stream: video
    VideoCodec                  TEXT    NULL,
    VideoContainer              TEXT    NULL,
    VideoRange                  TEXT    NULL,
    VideoBitrate                INTEGER NULL,
    VideoBitDepth               INTEGER NULL,
    VideoHeight                 INTEGER NULL,
    VideoWidth                  INTEGER NULL,

    -- Stream: audio
    AudioLanguage               TEXT    NULL,
    AudioCodec                  TEXT    NULL,
    AudioLayout                 TEXT    NULL,
    AudioBitrate                INTEGER NULL,
    AudioSampleRate             INTEGER NULL,

    -- Stream: subtitle
    SubtitleIsForced            BOOLEAN NULL,
    SubtitleIsHearingImpaired   BOOLEAN NULL,
    SubtitleCodec               TEXT    NULL,
    SubtitleLanguage            TEXT    NULL,

    -- Transcoding
    IsVideoDirect               BOOLEAN NOT NULL,
    IsAudioDirect               BOOLEAN NOT NULL,
    TranscodeBitrate            INTEGER NULL,
    HardwareAcceleration        TEXT    NULL,
    TranscodedVideoContainer    TEXT    NULL,
    TranscodedVideoCodec        TEXT    NULL,
    TranscodedAudioCodec        TEXT    NULL,
    TranscodeReasonsJson        TEXT    NULL,
    TranscodeCompletionPercentage REAL   NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_PlaybackEntries_PlaybackId
    ON PlaybackEntries (PlaybackId)
    WHERE PlaybackId IS NOT NULL AND IsCompleted = 0;
