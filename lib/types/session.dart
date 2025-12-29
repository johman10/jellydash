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
  int? bitrate;
  int? sampleRate;
  bool isDirectStream = false;

  SessionAudio({
    this.language,
    this.codec,
    this.layout,
    this.bitrate,
    this.sampleRate,
    this.isDirectStream = false,
  });

  @override
  String toString() {
    return 'SessionAudio(language: $language, codec: $codec, layout: $layout, bitrate: $bitrate, sampleRate: $sampleRate, isDirectStream: $isDirectStream)';
  }

  factory SessionAudio.fromJson(Map<String, dynamic> json,
      {bool? isDirectStream}) {
    return SessionAudio(
      language: json['Language'] as String?,
      codec: json['Codec'] as String?,
      layout: json['ChannelLayout'] as String?,
      bitrate: json['BitRate'] as int?,
      sampleRate: json['SampleRate'] as int?,
      isDirectStream: isDirectStream ?? false,
    );
  }
}

class SessionVideo {
  String? codec;
  String? container;
  String? videoRange;
  int? bitrate;
  int? bitDepth;
  int? height;
  int? width;
  bool isDirectStream;

  SessionVideo({
    this.codec,
    this.container,
    this.videoRange,
    this.bitrate,
    this.bitDepth,
    this.height,
    this.width,
    this.isDirectStream = false,
  });

  @override
  String toString() {
    return 'SessionVideo(codec: $codec, container: $container, videoRange: $videoRange, bitrate: $bitrate, bitDepth: $bitDepth, height: $height, width: $width, isDirectStream: $isDirectStream)';
  }

  factory SessionVideo.fromJson(Map<String, dynamic> json,
      {String? container, bool? isDirectStream}) {
    return SessionVideo(
      codec: json['Codec'] as String?,
      container: container,
      videoRange: json['VideoRange'] as String?,
      bitrate: json['BitRate'] as int?,
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

  @override
  String toString() {
    return 'SessionSubtitle(isForced: $isForced, isHearingImpaired: $isHearingImpaired, codec: $codec, language: $language)';
  }

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
  int? bitrate;

  TranscodingInfo({
    required this.video,
    required this.audio,
    required this.reasons,
    this.progress = 0.0,
    this.hardwareAcceleration,
    this.bitrate,
  });

  @override
  String toString() {
    return 'TranscodingInfo(video: $video, audio: $audio, reasons: $reasons, progress: $progress, hardwareAcceleration: $hardwareAcceleration, bitrate: $bitrate)';
  }

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
      bitrate: json['Bitrate'] as int?,
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
  String? genre;
  String? imageUrl;
  SessionVideo video;
  SessionAudio audio;
  SessionSubtitle subtitles;
  TranscodingInfo transcodingInfo;
  Duration progress;
  Duration duration;
  bool isPlaying;
  bool isPaused;
  bool isMuted;
  int? bitrate;
  String? userImageUrl;
  DateTime? dateCreated;

  Session({
    this.userName,
    this.client,
    this.deviceName,
    this.name,
    this.season,
    this.episode,
    this.year,
    this.genre,
    this.imageUrl,
    required this.video,
    required this.audio,
    required this.subtitles,
    required this.transcodingInfo,
    required this.progress,
    required this.duration,
    this.isPlaying = false,
    this.isPaused = false,
    this.isMuted = false,
    this.bitrate,
    this.userImageUrl,
    this.dateCreated
  });

  @override
  String toString() {
    return 'Session(userName: $userName, client: $client, deviceName: $deviceName, name: $name, season: $season, episode: $episode, year: $year, genre: $genre, imageUrl: $imageUrl, video: $video, audio: $audio, subtitles: $subtitles, transcodingInfo: $transcodingInfo, progress: $progress, duration: $duration, isPlaying: $isPlaying, isPaused: $isPaused, isMuted: $isMuted, bitrate: $bitrate, userImageUrl: $userImageUrl)';
  }

  factory Session.fromJson(String baseUrl, Map<String, dynamic> json) {
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
    var transcodingInfo = TranscodingInfo.fromJson(
        json['TranscodingInfo'] as Map<String, dynamic>? ?? {});
    var video = SessionVideo.fromJson(videoStream,
        container: json['NowPlayingItem']?['Container'] as String?,
        isDirectStream: json['TranscodingInfo']?['IsVideoDirect'] as bool?);
    var audio = SessionAudio.fromJson(audioStream,
        isDirectStream: json['TranscodingInfo']?['IsAudioDirect'] as bool?);

    return Session(
      userName: json['UserName'] as String?,
      userImageUrl: json['UserId'] != null && json['UserPrimaryImageTag'] != null ? '$baseUrl/Users/${json['UserId']}/Images/Primary?width=80&height=80&quality=90${json['UserPrimaryImageTag']}' : null,
      client: json['Client'] as String?,
      deviceName: json['DeviceName'] as String?,
      season: json['NowPlayingItem']?['ParentIndexNumber'] as int?,
      episode: json['NowPlayingItem']?['IndexNumber'] as int?,
      name: json['NowPlayingItem']?['SeriesName'] ?? json['NowPlayingItem']?['Name'] as String?,
      year: json['NowPlayingItem']?['ProductionYear'] as int?,
      genre: json['NowPlayingItem']?['Genres'] != null &&
              (json['NowPlayingItem']['Genres'] as List).isNotEmpty
          ? (json['NowPlayingItem']['Genres'] as List).first as String
          : null,
      imageUrl: json['NowPlayingItem']?['Id'] != null
          ? '$baseUrl/Items/${json['NowPlayingItem']['ParentThumbItemId'] ?? json['NowPlayingItem']['Id']}/Images/Primary?width=200&height=300'
          : null,
      video: video,
      audio: audio,
      subtitles: SessionSubtitle.fromJson(subtitleStream),
      transcodingInfo: transcodingInfo,
      progress: Duration(microseconds: positionTicks ~/ 10),
      duration: Duration(microseconds: runTimeTicks ~/ 10),
      isPlaying: json['NowPlayingItem'] != null,
      isPaused: json['PlayState']?['IsPaused'] as bool? ?? false,
      isMuted: json['PlayState']?['IsMuted'] as bool? ?? false,
      bitrate: ((video.isDirectStream || transcodingInfo.bitrate == null
              ? (video.bitrate ?? 0) + (audio.bitrate ?? 0)
              : transcodingInfo.bitrate) ??
          0),
      dateCreated: json['NowPlayingItem']?['DateCreated'] != null ? DateTime.parse(json['NowPlayingItem']['DateCreated']) : null
    );

  }
}
