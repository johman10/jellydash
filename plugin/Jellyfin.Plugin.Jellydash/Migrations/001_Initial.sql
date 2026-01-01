CREATE TABLE IF NOT EXISTS PlaybackEntries (
    Id                          INTEGER PRIMARY KEY AUTOINCREMENT,

    -- Identity
    ItemId                      TEXT    NOT NULL,
    ContentKind                 INTEGER NOT NULL,
    DisplayTitle                TEXT    NOT NULL,
    PrimaryImageUrl             TEXT    NULL,
    PrimaryGenre                TEXT    NULL,
    Year                        INTEGER NULL,
    SeriesName                  TEXT    NULL,
    SeasonNumber                INTEGER NULL,
    EpisodeNumber               INTEGER NULL,

    -- User
    UserId                      TEXT    NOT NULL,
    UserName                    TEXT    NOT NULL,
    UserImageUrl                TEXT    NULL,

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
    StartPercentage             REAL    NULL,
    EndPercentage               REAL    NULL,
    IsCompleted                 INTEGER NOT NULL,
    IsPaused                    INTEGER NOT NULL,

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
    SubtitleIsForced            INTEGER NULL,
    SubtitleIsHearingImpaired   INTEGER NULL,
    SubtitleCodec               TEXT    NULL,
    SubtitleLanguage            TEXT    NULL,

    -- Transcoding
    IsVideoDirect               INTEGER NOT NULL,
    IsAudioDirect               INTEGER NOT NULL,
    TranscodeBitrate            INTEGER NULL,
    HardwareAcceleration        TEXT    NULL,
    TranscodedVideoCodec        TEXT    NULL,
    TranscodedAudioCodec        TEXT    NULL,
    TranscodeReasonsJson        TEXT    NULL
);
