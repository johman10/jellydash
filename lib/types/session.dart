// Progress: PlayState.PlayState / NowPlayingItem.RunTimeTicks
// Subtitles: NowPlayingItem.MediaStreams.find(mediaStream => mediaStream.Type == 'Subtitle' && mediaStream.Index == PlayState.SubtitleStreamIndex)
// Audio: NowPlayingItem.MediaStreams.find(mediaStream => mediaStream.Type == 'Audio' && mediaStream.Index == PlayState.AudioStreamIndex)
// Video: NowPlayingItem.MediaStreams.find(mediaStream => mediaStream.Type == 'Video')
// Season: NowPlayingItem.ParentIndexNumber
// Episode: NowPlayingItem.IndexNumber
// Year: NowPlayingItem.ProductionYear
// DirectSteam: TranscodingInfo.IsVideoDirect, TranscodingInfo.IsAudioDirect
// Trancoding Progress: TranscodingInfo.CompletionPercentage
// Original AudioCodec: {{Audio}}.Codec
// Transcoded AudioCodec: TranscodingInfo.AudioCodec
// Image path: /Items/NowPlayingItem.ItemId/Images/Primary

class SessionAudio {
  String? language;
  String? codec;
  String? layout;
  int? bitRate;
  int? sampleRate;
  bool isDirectStream = false;

  SessionAudio({
    this.language,
    this.codec,
    this.layout,
    this.bitRate,
    this.sampleRate,
    this.isDirectStream = false,
  });

  factory SessionAudio.fromJson(Map<String, dynamic> json,
      {bool? isDirectStream}) {
    return SessionAudio(
      language: json['Language'] as String?,
      codec: json['Codec'] as String?,
      layout: json['ChannelLayout'] as String?,
      bitRate: json['BitRate'] as int?,
      sampleRate: json['SampleRate'] as int?,
      isDirectStream: isDirectStream ?? false,
    );
  }
}

class SessionVideo {
  String? codec;
  String? container;
  String? videoRange;
  int? bitRate;
  int? bitDepth;
  int? height;
  int? width;
  bool isDirectStream;

  SessionVideo({
    this.codec,
    this.container,
    this.videoRange,
    this.bitRate,
    this.bitDepth,
    this.height,
    this.width,
    this.isDirectStream = false,
  });

  factory SessionVideo.fromJson(Map<String, dynamic> json,
      {String? container, bool? isDirectStream}) {
    return SessionVideo(
      codec: json['Codec'] as String?,
      container: container,
      videoRange: json['VideoRange'] as String?,
      bitRate: json['BitRate'] as int?,
      bitDepth: json['BitDepth'] as int?,
      height: json['Height'] as int?,
      width: json['Width'] as int?,
      isDirectStream: isDirectStream ?? false,
    );
  }
}

class SessionSubtitle {
  bool isForced;
  bool isHearingImpaired;
  String? codec;
  String? language;

  SessionSubtitle({
    this.isForced = false,
    this.isHearingImpaired = false,
    this.codec,
    this.language,
  });

  factory SessionSubtitle.fromJson(Map<String, dynamic> json) {
    return SessionSubtitle(
      isForced: json['IsForced'] as bool? ?? false,
      isHearingImpaired: json['IsHearingImpaired'] as bool? ?? false,
      codec: json['Codec'] as String?,
      language: json['Language'] as String?,
    );
  }
}

class TranscodingInfo {
  SessionVideo video;
  SessionAudio audio;
  List<String> reasons;
  double progress;
  String? hardwareAcceleration;

  TranscodingInfo({
    required this.video,
    required this.audio,
    required this.reasons,
    this.progress = 0.0,
    this.hardwareAcceleration,
  });

  factory TranscodingInfo.fromJson(Map<String, dynamic> json) {
    return TranscodingInfo(
      video: SessionVideo(
        container: json['Container'] as String?,
        codec: json['VideoCodec'] as String?,
        width: json['Width'] as int?,
        height: json['Height'] as int?,
        isDirectStream: json['IsVideoDirect'] as bool? ?? false,
      ),
      audio: SessionAudio(
        codec: json['AudioCodec'] as String?,
        isDirectStream: json['IsAudioDirect'] as bool? ?? false,
      ),
      reasons: (json['TranscodingReasons'] as List<dynamic>?)
              ?.map((e) => e as String)
              .toList() ??
          [],
      progress: (json['CompletionPercentage'] as num?)?.toDouble() ?? 0.0,
      hardwareAcceleration: json['HardwareAccelerationType'] as String?,
    );
  }
}

class Session {
  String? userName;
  String? client;
  String? deviceName;
  String? name;
  int? season;
  int? episode;
  int? year;
  String? imagePath;
  SessionVideo video;
  SessionAudio audio;
  SessionSubtitle subtitles;
  TranscodingInfo transcodingInfo;
  double progress;
  bool isPlaying;
  bool isPaused;
  bool isMuted;

  Session({
    this.userName,
    this.client,
    this.deviceName,
    this.name,
    this.season,
    this.episode,
    this.year,
    this.imagePath,
    required this.video,
    required this.audio,
    required this.subtitles,
    required this.transcodingInfo,
    required this.progress,
    this.isPlaying = false,
    this.isPaused = false,
    this.isMuted = false,
  });

  factory Session.fromJson(Map<String, dynamic> json) {
    var videoStream = json['NowPlayingItem']?['MediaStreams']?.firstWhere(
            (stream) => stream['Type'] == 'Video',
            orElse: () => null) as Map<String, dynamic>? ??
        {};
    var audioStream = json['NowPlayingItem']?['MediaStreams']?.firstWhere(
            (stream) =>
                stream['Type'] == 'Audio' &&
                stream['Index'] == json['PlayState']?['AudioStreamIndex'],
            orElse: () => null) as Map<String, dynamic>? ??
        {};
    var subtitleStream = json['NowPlayingItem']?['MediaStreams']?.firstWhere(
            (stream) =>
                stream['Type'] == 'Subtitle' &&
                stream['Index'] == json['PlayState']?['SubtitleStreamIndex'],
            orElse: () => null) as Map<String, dynamic>? ??
        {};
    var positionTicks = json['PlayState']?['PositionTicks'] as int? ?? 0;
    var runTimeTicks = json['NowPlayingItem']?['RunTimeTicks'] as int? ?? 1;

    return Session(
        userName: json['UserName'] as String?,
        client: json['Client'] as String?,
        deviceName: json['DeviceName'] as String?,
        season: json['NowPlayingItem']?['ParentIndexNumber'] as int?,
        episode: json['NowPlayingItem']?['IndexNumber'] as int?,
        name: json['NowPlayingItem']?['SeriesName'] as String?,
        year: json['NowPlayingItem']?['ProductionYear'] as int?,
        imagePath: json['NowPlayingItem']?['Id'] != null
            ? '/Items/${json['NowPlayingItem']['Id']}/Images/Primary'
            : null,
        video: SessionVideo.fromJson(videoStream,
            container: json['NowPlayingItem']?['Container'] as String?,
            isDirectStream: json['TranscodingInfo']?['IsVideoDirect'] as bool?),
        audio: SessionAudio.fromJson(audioStream,
            isDirectStream: json['TranscodingInfo']?['IsAudioDirect'] as bool?),
        subtitles: SessionSubtitle.fromJson(subtitleStream),
        transcodingInfo: TranscodingInfo.fromJson(
            json['TranscodingInfo'] as Map<String, dynamic>? ?? {}),
        progress: positionTicks / runTimeTicks * 100,
        isPlaying: json['NowPlayingItem'] != null,
        isPaused: json['PlayState']?['IsPaused'] as bool? ?? false,
        isMuted: json['PlayState']?['IsMuted'] as bool? ?? false);
  }
}
