enum ContentType {
  movie('Movie'),
  episode('Episode'),
  other('Other');

  final String sessionValue;

  const ContentType(this.sessionValue);

  static ContentType fromApiKind(String? kind) {
    return values.firstWhere(
      (ct) => ct.sessionValue == kind,
      orElse: () => ContentType.other,
    );
  }
}

class ContentIdentity {
  final String? primaryImageUrl;
  final String title;
  final List<String> genres;
  final int? year;
  final int? seasonNumber;
  final int? episodeNumber;
  final String? seriesName;

  const ContentIdentity({
    this.primaryImageUrl,
    required this.title,
    this.genres = const [],
    this.year,
    this.seasonNumber,
    this.episodeNumber,
    this.seriesName,
  });

  factory ContentIdentity.fromJson(String baseUrl, Map<String, dynamic> json) {
    final primaryImagePath = json['primary_image_path'] as String?;

    return ContentIdentity(
      primaryImageUrl:
          primaryImagePath != null ? '$baseUrl$primaryImagePath' : null,
      title: json['title'] as String? ?? '',
      genres: (json['genres'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          const [],
      year: json['year'] as int?,
      seasonNumber: json['season_number'] as int?,
      episodeNumber: json['episode_number'] as int?,
      seriesName: json['series_name'] as String?,
    );
  }

  factory ContentIdentity.fromSessionJson(
    String baseUrl,
    Map<String, dynamic> json,
  ) {
    final nowPlayingItem = json['NowPlayingItem'] as Map<String, dynamic>?;

    if (nowPlayingItem == null) {
      return ContentIdentity(title: '');
    }

    final itemId = nowPlayingItem['Id']?.toString();
    final parentItemId = nowPlayingItem['ParentId']?.toString();
    final itemType = nowPlayingItem['Type'] as String?;
    final contentType = ContentType.fromApiKind(itemType);

    String? primaryImagePath;
    if (contentType == ContentType.episode &&
        parentItemId != null &&
        parentItemId.isNotEmpty) {
      primaryImagePath = '/Items/$parentItemId/Images/Primary';
    } else if (itemId != null && itemId.isNotEmpty) {
      primaryImagePath = '/Items/$itemId/Images/Primary';
    }

    return ContentIdentity(
      primaryImageUrl:
          primaryImagePath != null ? '$baseUrl$primaryImagePath' : null,
      title: nowPlayingItem['Name'] as String? ?? '',
      genres: (nowPlayingItem['Genres'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          const [],
      year: nowPlayingItem['ProductionYear'] as int?,
      seasonNumber: nowPlayingItem['ParentIndexNumber'] as int?,
      episodeNumber: nowPlayingItem['IndexNumber'] as int?,
      seriesName: nowPlayingItem['SeriesName'] as String?,
    );
  }
}

class UserInfo {
  final String userId;
  final String userName;
  final String? userImageUrl;

  const UserInfo({
    required this.userId,
    required this.userName,
    this.userImageUrl,
  });

  factory UserInfo.fromJson(String baseUrl, Map<String, dynamic> json) {
    final userImagePath = json['user_image_path'] as String?;

    return UserInfo(
      userId: json['user_id'] as String? ?? '',
      userName: json['user_name'] as String? ?? '',
      userImageUrl: userImagePath != null ? '$baseUrl$userImagePath' : null,
    );
  }

  factory UserInfo.fromSessionJson(String baseUrl, Map<String, dynamic> json) {
    final userId = json['UserId']?.toString();
    final userPrimaryImageTag = json['UserPrimaryImageTag']?.toString();

    return UserInfo(
      userId: userId ?? '',
      userName: json['UserName'] as String? ?? '',
      userImageUrl: userId != null &&
              userPrimaryImageTag != null &&
              userPrimaryImageTag.isNotEmpty
          ? '$baseUrl/Users/$userId/Images/Primary?tag=$userPrimaryImageTag'
          : null,
    );
  }
}

class ClientInfo {
  final String deviceName;
  final String clientName;
  final String? deviceId;

  const ClientInfo({
    required this.deviceName,
    required this.clientName,
    this.deviceId,
  });

  factory ClientInfo.fromJson(Map<String, dynamic> json) {
    return ClientInfo(
      deviceName: json['device_name'] as String? ?? '',
      clientName: json['client_name'] as String? ?? '',
      deviceId: json['device_id'] as String?,
    );
  }

  factory ClientInfo.fromSessionJson(Map<String, dynamic> json) {
    return ClientInfo(
      deviceName: json['DeviceName'] as String? ?? '',
      clientName: json['Client'] as String? ?? '',
      deviceId: json['DeviceId'] as String?,
    );
  }
}

class TimingInfo {
  final DateTime? startUtc;
  final DateTime? endUtc;
  final int? runtimeTicks;
  final int? startPositionTicks;
  final int? endPositionTicks;
  final double? startPercentage;
  final double? endPercentage;

  const TimingInfo({
    this.startUtc,
    this.endUtc,
    this.runtimeTicks,
    this.startPositionTicks,
    this.endPositionTicks,
    this.startPercentage,
    this.endPercentage,
  });

  factory TimingInfo.fromJson(Map<String, dynamic> json) {
    final startUtcStr = json['start_utc'] as String?;
    final endUtcStr = json['end_utc'] as String?;

    return TimingInfo(
      startUtc: startUtcStr != null ? DateTime.parse(startUtcStr) : null,
      endUtc: endUtcStr != null ? DateTime.parse(endUtcStr) : null,
      runtimeTicks: json['runtime_ticks'] as int?,
      startPositionTicks: json['start_position_ticks'] as int?,
      endPositionTicks: json['end_position_ticks'] as int?,
      startPercentage: (json['start_percentage'] as num?)?.toDouble(),
      endPercentage: (json['end_percentage'] as num?)?.toDouble(),
    );
  }

  factory TimingInfo.fromSessionJson(Map<String, dynamic> json) {
    final nowPlayingItem = json['NowPlayingItem'] as Map<String, dynamic>?;
    final playState = json['PlayState'] as Map<String, dynamic>?;

    final runTimeTicks = (nowPlayingItem?['RunTimeTicks'] as num?)?.toInt();
    final positionTicks = (playState?['PositionTicks'] as num?)?.toInt();

    double? endPercentage;
    if (runTimeTicks != null && positionTicks != null && runTimeTicks > 0) {
      endPercentage =
          (positionTicks.toDouble() / runTimeTicks.toDouble()) * 100;
    }

    return TimingInfo(
      startUtc: null,
      endUtc: null,
      runtimeTicks: runTimeTicks,
      startPositionTicks: 0,
      endPositionTicks: positionTicks,
      startPercentage: 0.0,
      endPercentage: endPercentage,
    );
  }
}

class VideoTrack {
  final String? codec;
  final String? container;
  final String? videoRange;
  final int? bitrate;
  final int? bitDepth;
  final int? height;
  final int? width;

  const VideoTrack({
    this.codec,
    this.container,
    this.videoRange,
    this.bitrate,
    this.bitDepth,
    this.height,
    this.width,
  });

  factory VideoTrack.fromJson(Map<String, dynamic> json) {
    return VideoTrack(
      codec: json['codec'] as String?,
      container: json['container'] as String?,
      videoRange: json['video_range'] as String?,
      bitrate: json['bitrate'] as int?,
      bitDepth: json['bit_depth'] as int?,
      height: json['height'] as int?,
      width: json['width'] as int?,
    );
  }

  factory VideoTrack.fromSessionJson(Map<String, dynamic> json) {
    final nowPlayingItem = json['NowPlayingItem'] as Map<String, dynamic>?;
    final mediaStreams = nowPlayingItem?['MediaStreams'] as List<dynamic>?;
    final transcodingInfo = json['TranscodingInfo'] as Map<String, dynamic>?;

    // Find the video stream from media streams
    Map<String, dynamic>? videoStream;
    if (mediaStreams != null) {
      for (final stream in mediaStreams) {
        final streamMap = stream as Map<String, dynamic>;
        if (streamMap['Type'] == 'Video') {
          videoStream = streamMap;
          break;
        }
      }
    }

    // If transcoding, use transcoding info; otherwise use media stream
    if (transcodingInfo != null) {
      return VideoTrack(
        codec: transcodingInfo['VideoCodec'] as String?,
        container: transcodingInfo['Container'] as String?,
        videoRange: videoStream?['VideoRange'] as String?,
        bitrate: transcodingInfo['Bitrate'] as int?,
        bitDepth: videoStream?['BitDepth'] as int?,
        height: transcodingInfo['Height'] as int?,
        width: transcodingInfo['Width'] as int?,
      );
    }

    if (videoStream != null) {
      return VideoTrack(
        codec: videoStream['Codec'] as String?,
        container: nowPlayingItem?['Container'] as String?,
        videoRange: videoStream['VideoRange'] as String?,
        bitrate: videoStream['BitRate'] as int?,
        bitDepth: videoStream['BitDepth'] as int?,
        height: videoStream['Height'] as int?,
        width: videoStream['Width'] as int?,
      );
    }

    return const VideoTrack();
  }
}

class AudioTrack {
  final String? language;
  final String? codec;
  final String? layout;
  final int? bitrate;
  final int? sampleRate;

  const AudioTrack(
      {this.language, this.codec, this.layout, this.bitrate, this.sampleRate});

  factory AudioTrack.fromJson(Map<String, dynamic> json) {
    return AudioTrack(
      language: json['language'] as String?,
      codec: json['codec'] as String?,
      layout: json['layout'] as String?,
      bitrate: json['bitrate'] as int?,
      sampleRate: json['sample_rate'] as int?,
    );
  }

  factory AudioTrack.fromSessionJson(Map<String, dynamic> json) {
    final mediaStreams = (json['NowPlayingItem']
        as Map<String, dynamic>?)?['MediaStreams'] as List<dynamic>?;
    final playState = json['PlayState'] as Map<String, dynamic>?;
    final audioStreamIndex = playState?['AudioStreamIndex'];
    final transcodingInfo = json['TranscodingInfo'] as Map<String, dynamic>?;

    // Find the active audio stream
    Map<String, dynamic>? audioStream;
    if (mediaStreams != null && audioStreamIndex != null) {
      for (final stream in mediaStreams) {
        final streamMap = stream as Map<String, dynamic>;
        if (streamMap['Type'] == 'Audio' &&
            streamMap['Index'] == audioStreamIndex) {
          audioStream = streamMap;
          break;
        }
      }
    }

    // If transcoding, use transcoding info; otherwise use media stream
    if (transcodingInfo != null) {
      return AudioTrack(
        language: audioStream?['Language'] as String?,
        codec: transcodingInfo['AudioCodec'] as String?,
        layout: audioStream?['ChannelLayout'] as String?,
        bitrate: transcodingInfo['Bitrate'] as int?,
        sampleRate: audioStream?['SampleRate'] as int?,
      );
    }

    if (audioStream != null) {
      return AudioTrack(
        language: audioStream['Language'] as String?,
        codec: audioStream['Codec'] as String?,
        layout: audioStream['ChannelLayout'] as String?,
        bitrate: audioStream['BitRate'] as int?,
        sampleRate: audioStream['SampleRate'] as int?,
      );
    }

    return const AudioTrack();
  }
}

class SubtitleTrack {
  final bool isForced;
  final bool isHearingImpaired;
  final String? codec;
  final String? language;

  const SubtitleTrack({
    required this.isForced,
    required this.isHearingImpaired,
    this.codec,
    this.language,
  });

  factory SubtitleTrack.fromJson(Map<String, dynamic> json) {
    return SubtitleTrack(
      isForced: json['is_forced'] as bool? ?? false,
      isHearingImpaired: json['is_hearing_impaired'] as bool? ?? false,
      codec: json['codec'] as String?,
      language: json['language'] as String?,
    );
  }

  factory SubtitleTrack.fromSessionJson(Map<String, dynamic> json) {
    final mediaStreams = (json['NowPlayingItem']
        as Map<String, dynamic>?)?['MediaStreams'] as List<dynamic>?;
    final playState = json['PlayState'] as Map<String, dynamic>?;
    final subtitleStreamIndex = playState?['SubtitleStreamIndex'];

    // Find the active subtitle stream
    if (mediaStreams != null && subtitleStreamIndex != null) {
      for (final stream in mediaStreams) {
        final streamMap = stream as Map<String, dynamic>;
        if (streamMap['Type'] == 'Subtitle' &&
            streamMap['Index'] == subtitleStreamIndex) {
          return SubtitleTrack(
            isForced: streamMap['IsForced'] as bool? ?? false,
            isHearingImpaired: streamMap['IsHearingImpaired'] as bool? ?? false,
            codec: streamMap['Codec'] as String?,
            language: streamMap['Language'] as String?,
          );
        }
      }
    }

    return const SubtitleTrack(
      isForced: false,
      isHearingImpaired: false,
    );
  }
}

class StreamInfo {
  final VideoTrack? video;
  final AudioTrack? audio;
  final SubtitleTrack? subtitle;

  const StreamInfo({
    this.video,
    this.audio,
    this.subtitle,
  });

  factory StreamInfo.fromJson(Map<String, dynamic> json) {
    final videoJson = json['video'] as Map<String, dynamic>?;
    final audioJson = json['audio'] as Map<String, dynamic>?;
    final subtitleJson = json['subtitle'] as Map<String, dynamic>?;

    return StreamInfo(
      video: videoJson != null ? VideoTrack.fromJson(videoJson) : null,
      audio: audioJson != null ? AudioTrack.fromJson(audioJson) : null,
      subtitle:
          subtitleJson != null ? SubtitleTrack.fromJson(subtitleJson) : null,
    );
  }

  factory StreamInfo.fromSessionJson(Map<String, dynamic> json) {
    final playState = json['PlayState'] as Map<String, dynamic>?;
    final hasSubtitles = playState?['SubtitleStreamIndex'] != null;

    final video = VideoTrack.fromSessionJson(json);
    final audio = AudioTrack.fromSessionJson(json);
    final subtitle = hasSubtitles ? SubtitleTrack.fromSessionJson(json) : null;

    return StreamInfo(
      video: video,
      audio: audio,
      subtitle: subtitle,
    );
  }
}

class TranscodingInfo {
  final bool isVideoDirect;
  final bool isAudioDirect;
  final String? hardwareAcceleration;
  final int? bitrate;
  final VideoTrack? transcodedVideo;
  final AudioTrack? transcodedAudio;
  final List<String> reasons;
  final double? completionPercentage;

  const TranscodingInfo(
      {required this.isVideoDirect,
      required this.isAudioDirect,
      this.hardwareAcceleration,
      this.bitrate,
      this.transcodedVideo,
      this.transcodedAudio,
      required this.reasons,
      this.completionPercentage});

  factory TranscodingInfo.fromJson(Map<String, dynamic> json) {
    final transcodedVideoJson =
        json['transcoded_video'] as Map<String, dynamic>?;
    final transcodedAudioJson =
        json['transcoded_audio'] as Map<String, dynamic>?;

    return TranscodingInfo(
      isVideoDirect: json['is_video_direct'] as bool? ?? false,
      isAudioDirect: json['is_audio_direct'] as bool? ?? false,
      hardwareAcceleration: json['hardware_acceleration'] as String?,
      bitrate: json['bitrate'] as int?,
      transcodedVideo: transcodedVideoJson != null
          ? VideoTrack.fromJson(transcodedVideoJson)
          : null,
      transcodedAudio: transcodedAudioJson != null
          ? AudioTrack.fromJson(transcodedAudioJson)
          : null,
      reasons: (json['reasons'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          const [],
      completionPercentage: (json['completion_percentage'] as num?)?.toDouble(),
    );
  }

  factory TranscodingInfo.fromSessionJson(Map<String, dynamic> json) {
    return TranscodingInfo(
      isVideoDirect: json['IsVideoDirect'] as bool? ?? false,
      isAudioDirect: json['IsAudioDirect'] as bool? ?? false,
      hardwareAcceleration: json['HardwareAccelerationType'] as String?,
      bitrate: json['Bitrate'] as int?,
      transcodedVideo: VideoTrack(
        codec: json['VideoCodec'] as String?,
        container: json['Container'] as String?,
        bitrate: json['Bitrate'] as int?,
        height: json['Height'] as int?,
        width: json['Width'] as int?,
      ),
      transcodedAudio: AudioTrack(
        codec: json['AudioCodec'] as String?,
        bitrate: json['Bitrate'] as int?,
      ),
      reasons: (json['TranscodeReasons'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          const [],
      completionPercentage: (json['CompletionPercentage'] as num?)?.toDouble(),
    );
  }
}

class PlaybackEntry {
  final String itemId;
  final String? parentItemId;
  final ContentType contentType;
  final ContentIdentity identity;
  final UserInfo user;
  final ClientInfo client;
  final TimingInfo timing;
  final StreamInfo streams;
  final TranscodingInfo? transcoding;
  final bool isCompleted;
  final bool isPaused;

  const PlaybackEntry({
    required this.itemId,
    this.parentItemId,
    required this.contentType,
    required this.identity,
    required this.user,
    required this.client,
    required this.timing,
    required this.streams,
    this.transcoding,
    required this.isCompleted,
    required this.isPaused,
  });

  factory PlaybackEntry.fromJson(String baseUrl, Map<String, dynamic> json) {
    final identityJson = json['identity'] as Map<String, dynamic>?;
    final userJson = json['user'] as Map<String, dynamic>?;
    final clientJson = json['client'] as Map<String, dynamic>?;
    final timingJson = json['timing'] as Map<String, dynamic>?;
    final streamsJson = json['streams'] as Map<String, dynamic>?;
    final transcodingJson = json['transcoding'] as Map<String, dynamic>?;

    return PlaybackEntry(
      itemId: json['item_id'] as String? ?? '',
      parentItemId: json['parent_item_id'] as String?,
      contentType: ContentType.fromApiKind(json['content_kind'] as String?),
      identity: identityJson != null
          ? ContentIdentity.fromJson(baseUrl, identityJson)
          : ContentIdentity(title: ''),
      user: userJson != null
          ? UserInfo.fromJson(baseUrl, userJson)
          : UserInfo(userId: '', userName: ''),
      client: clientJson != null
          ? ClientInfo.fromJson(clientJson)
          : ClientInfo(deviceName: '', clientName: ''),
      timing: timingJson != null
          ? TimingInfo.fromJson(timingJson)
          : const TimingInfo(),
      streams: streamsJson != null
          ? StreamInfo.fromJson(streamsJson)
          : const StreamInfo(),
      transcoding: transcodingJson != null
          ? TranscodingInfo.fromJson(transcodingJson)
          : null,
      isCompleted: json['is_completed'] as bool? ?? false,
      isPaused: json['is_paused'] as bool? ?? false,
    );
  }

  factory PlaybackEntry.fromSessionJson(
    String baseUrl,
    Map<String, dynamic> json,
  ) {
    final nowPlayingItem = json['NowPlayingItem'] as Map<String, dynamic>?;
    final playState = json['PlayState'] as Map<String, dynamic>?;
    final transcodingInfo = json['TranscodingInfo'] as Map<String, dynamic>?;
    final contentType =
        ContentType.fromApiKind(nowPlayingItem?['Type'] as String?);

    return PlaybackEntry(
      itemId: nowPlayingItem?['Id']?.toString() ?? '',
      parentItemId: nowPlayingItem?['ParentId']?.toString(),
      contentType: contentType,
      identity: ContentIdentity.fromSessionJson(baseUrl, json),
      user: UserInfo.fromSessionJson(baseUrl, json),
      client: ClientInfo.fromSessionJson(json),
      timing: TimingInfo.fromSessionJson(json),
      streams: StreamInfo.fromSessionJson(json),
      transcoding: transcodingInfo != null
          ? TranscodingInfo.fromSessionJson(transcodingInfo)
          : null,
      isCompleted: false,
      isPaused: playState?['IsPaused'] as bool? ?? false,
    );
  }
}
