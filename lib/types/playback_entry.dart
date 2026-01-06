enum ContentKind { movie, episode, other }

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
    return ContentIdentity(
      primaryImageUrl: json['primary_image_path'] != null ? '$baseUrl${json['primary_image_path']}' : null,
      title: json['title'],
      genres: (json['genres'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? const [],
      year: json['year'],
      seasonNumber: json['season_number'],
      episodeNumber: json['episode_number'],
      seriesName: json['series_name'],
    );
  }

  factory ContentIdentity.fromSessionJson(String baseUrl,ContentKind contentKind, Map<String, dynamic> json) {
    final nowPlayingItem = json['NowPlayingItem'] as Map<String, dynamic>?;
    final itemId = nowPlayingItem?['Id']?.toString();
    final parentItemId = nowPlayingItem?['ParentId']?.toString();

    String? primaryImagePath;
    if (contentKind == ContentKind.episode && parentItemId != null && parentItemId.isNotEmpty) {
      primaryImagePath = '/Items/$parentItemId/Images/Primary';
    } else if (itemId != null && itemId.isNotEmpty) {
      primaryImagePath = '/Items/$itemId/Images/Primary';
    }

    return ContentIdentity(
      primaryImageUrl: primaryImagePath != null ? '$baseUrl$primaryImagePath' : null,
      title: nowPlayingItem?['Name'] ?? '',
      genres: (nowPlayingItem?['Genres'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? const [],
      year: nowPlayingItem?['ProductionYear'] as int?,
      seasonNumber: nowPlayingItem?['ParentIndexNumber'] as int?,
      episodeNumber: nowPlayingItem?['IndexNumber'] as int?,
      seriesName: nowPlayingItem?['SeriesName'] as String?,
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
    return UserInfo(
      userId: json['user_id'],
      userName: json['user_name'],
      userImageUrl: json['user_image_path'] != null ? '$baseUrl${json['user_image_path']}' : null,
    );
  }

  factory UserInfo.fromSessionJson(String baseUrl,Map<String, dynamic> json) {
    final userId = json['UserId']?.toString();
    return UserInfo(
      userId: userId ?? '',
      userName: json['UserName'] ?? '',
      userImageUrl: userId != null && (json['UserPrimaryImageTag']?.toString().isNotEmpty ?? false)
          ? '$baseUrl/Users/${json['UserId']}/Images/Primary?tag=${json['UserPrimaryImageTag']}'
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
      deviceName: json['device_name'],
      clientName: json['client_name'],
      deviceId: json['device_id'],
    );
  }

  factory ClientInfo.fromSessionJson(Map<String, dynamic> json) {
    return ClientInfo(
      deviceName: json['DeviceName'],
      clientName: json['Client'],
      deviceId: json['DeviceId'],
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
    return TimingInfo(
      startUtc: DateTime.parse(json['start_utc']),
      endUtc:
          json['end_utc'] != null ? DateTime.parse(json['end_utc']) : null,
      runtimeTicks: json['runtime_ticks'],
      startPositionTicks: json['start_position_ticks'],
      endPositionTicks: json['end_position_ticks'],
      startPercentage: json['start_percentage']?.toDouble(),
      endPercentage: json['end_percentage']?.toDouble(),
    );
  }

  factory TimingInfo.fromSessionJson(int? runTimeTicks, Map<String, dynamic> json) {
    final positionTicks = (json['PlayState']?['PositionTicks'] as num?)?.toInt();

    return TimingInfo(
      startUtc: null,
      endUtc: null,
      runtimeTicks: runTimeTicks,
      startPositionTicks: 0,
      endPositionTicks: positionTicks,
      startPercentage: 0.0,
      endPercentage: runTimeTicks != null &&
              positionTicks != null
          ? (positionTicks.toDouble() /
              runTimeTicks.toDouble() *
              100)
          : null,
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
        codec: json['codec'],
        container: json['container'],
        videoRange: json['video_range'],
        bitrate: json['bitrate'],
        bitDepth: json['bit_depth'],
        height: json['height'],
        width: json['width']);
  }

  factory VideoTrack.fromTranscodingInfoJson(Map<String, dynamic> json) {
    return VideoTrack(
      codec: json['VideoCodec'],
      container: json['Container'],
      bitrate: json['Bitrate'],
      height: json['Height'],
      width: json['Width'],
    );
  }

  factory VideoTrack.fromMediaStreamJson(
    Map<String, dynamic>? json, {
    String? container,
  }) {
    if (json == null) {
      return const VideoTrack();
    }

    return VideoTrack(
      codec: json['Codec'] as String?,
      container: container,
      videoRange: json['VideoRange'] as String?,
      bitrate: json['BitRate'] as int?,
      bitDepth: json['BitDepth'] as int?,
      height: json['Height'] as int?,
      width: json['Width'] as int?,
    );
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
      language: json['language'],
      codec: json['codec'],
      layout: json['layout'],
      bitrate: json['bitrate'],
      sampleRate: json['sample_rate'],
    );
  }

  factory AudioTrack.fromTranscodingInfoJson(Map<String, dynamic> json) {
    return AudioTrack(
      codec: json['AudioCodec'],
      bitrate: json['Bitrate'],
    );
  }

  factory AudioTrack.fromMediaStreamJson(Map<String, dynamic>? json) {
    if (json == null) {
      return const AudioTrack();
    }

    return AudioTrack(
      language: json['Language'] as String?,
      codec: json['Codec'] as String?,
      layout: json['ChannelLayout'] as String?,
      bitrate: json['BitRate'] as int?,
      sampleRate: json['SampleRate'] as int?,
    );
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
      isForced: json['is_forced'] ?? false,
      isHearingImpaired: json['is_hearing_impaired'] ?? false,
      codec: json['codec'],
      language: json['language'],
    );
  }

  factory SubtitleTrack.fromMediaStreamJson(Map<String, dynamic> json) {
    return SubtitleTrack(
      isForced: json['IsForced'] ?? false,
      isHearingImpaired: json['IsHearingImpaired'] ?? false,
      codec: json['Codec'],
      language: json['Language'],
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
    return StreamInfo(
      video: json['video'] != null
          ? VideoTrack.fromJson(json['video'])
          : null,
      audio: json['audio'] != null
          ? AudioTrack.fromJson(json['audio'])
          : null,
      subtitle: json['subtitle'] != null
          ? SubtitleTrack.fromJson(json['subtitle'])
          : null,
    );
  }

  factory StreamInfo.fromSessionJson(Map<String, dynamic> json) {
    final nowPlayingItem = json['NowPlayingItem'] as Map<String, dynamic>?;
    final mediaStreams = nowPlayingItem?['MediaStreams'] as List<dynamic>?;
    final audioStreamIndex = json['PlayState']?['AudioStreamIndex'];
    final subtitleStreamIndex = json['PlayState']?['SubtitleStreamIndex'];

    Map<String, dynamic>? videoStream;
    Map<String, dynamic>? audioStream;
    Map<String, dynamic>? subtitleStream;

    if (mediaStreams != null) {
      for (final stream in mediaStreams) {
        final streamMap = stream as Map<String, dynamic>;
        final type = streamMap['Type'];

        if (type == 'Video' && videoStream == null) {
          videoStream = streamMap;
        }

        if (type == 'Audio' && streamMap['Index'] == audioStreamIndex) {
          audioStream = streamMap;
        }

        if (type == 'Subtitle' && streamMap['Index'] == subtitleStreamIndex) {
          subtitleStream = streamMap;
        }
      }
    }

    return StreamInfo(
      video: VideoTrack.fromMediaStreamJson(
        videoStream,
        container: nowPlayingItem?['Container'] as String?,
      ),
      audio: AudioTrack.fromMediaStreamJson(audioStream),
      subtitle: subtitleStream != null
          ? SubtitleTrack.fromMediaStreamJson(subtitleStream)
          : null,
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

  const TranscodingInfo({
    required this.isVideoDirect,
    required this.isAudioDirect,
    this.hardwareAcceleration,
    this.bitrate,
    this.transcodedVideo,
    this.transcodedAudio,
    required this.reasons,
    this.completionPercentage
  });

  factory TranscodingInfo.fromJson(Map<String, dynamic> json) {
    return TranscodingInfo(
      isVideoDirect: json['is_video_direct'] ?? false,
      isAudioDirect: json['is_audio_direct'] ?? false,
      hardwareAcceleration: json['hardware_acceleration'],
      bitrate: json['bitrate'],
      transcodedVideo: json['transcoded_video'] != null
          ? VideoTrack.fromJson(json['transcoded_video'])
          : null,
      transcodedAudio: json['transcoded_audio'] != null
          ? AudioTrack.fromJson(json['transcoded_audio'])
          : null,
      reasons: (json['reasons'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          [],
      completionPercentage: json['completion_percentage']?.toDouble(),
    );
  }

  factory TranscodingInfo.fromSessionJson(Map<String, dynamic> json) {
    return TranscodingInfo(
      isVideoDirect: json['IsVideoDirect'] ?? false,
      isAudioDirect: json['IsAudioDirect'] ?? false,
      hardwareAcceleration: json['HardwareAccelerationType'],
      bitrate: json['Bitrate'],
      transcodedVideo: VideoTrack.fromTranscodingInfoJson(json),
      transcodedAudio: AudioTrack.fromTranscodingInfoJson(json),
      reasons: (json['TranscodeReasons'] as List<dynamic>?)
              ?.map((e) => e.toString())
              .toList() ??
          [],
      completionPercentage: json['CompletionPercentage']?.toDouble(),
    );
  }
}

class PlaybackEntry {
  final String itemId;
  final String? parentItemId;
  final ContentKind contentKind;
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
    required this.contentKind,
    required this.identity,
    required this.user,
    required this.client,
    required this.timing,
    required this.streams,
    this.transcoding,
    required this.isCompleted,
    required this.isPaused,
  });

  factory PlaybackEntry.fromJson(String baseUrl,Map<String, dynamic> json) {
    ContentKind contentKind;
    if (json['content_kind'] == 0) {
      contentKind = ContentKind.movie;
    } else if (json['content_kind'] == 1) {
      contentKind = ContentKind.episode;
    } else {
      contentKind = ContentKind.other;
    }

    return PlaybackEntry(
      itemId: json['item_id'],
      parentItemId: json['parent_item_id'],
      contentKind: contentKind,
      identity: ContentIdentity.fromJson(baseUrl, json['identity']),
      user: UserInfo.fromJson(baseUrl, json['user']),
      client: ClientInfo.fromJson(json['client']),
      timing: TimingInfo.fromJson(json['timing']),
      streams: StreamInfo.fromJson(json['streams']),
      transcoding: json['transcoding'] != null
          ? TranscodingInfo.fromJson(json['transcoding'])
          : null,
      isCompleted: json['is_completed'] ?? false,
      isPaused: json['is_paused'] ?? false,
    );
  }

  factory PlaybackEntry.fromSessionJson(String baseUrl, Map<String, dynamic> json) {
    final nowPlayingItem = json['NowPlayingItem'] as Map<String, dynamic>?;

    final contentKind = (nowPlayingItem?['Type'] == 'Movie')
        ? ContentKind.movie
        : (nowPlayingItem?['Type'] == 'Episode')
            ? ContentKind.episode
            : ContentKind.other;

    return PlaybackEntry(
      itemId: nowPlayingItem?['Id']?.toString() ?? '',
      parentItemId: nowPlayingItem?['ParentId']?.toString(),
      contentKind: contentKind,
      identity: ContentIdentity.fromSessionJson(baseUrl, contentKind, json),
      user: UserInfo.fromSessionJson(baseUrl, json),
      client: ClientInfo.fromSessionJson(json),
      timing: TimingInfo.fromSessionJson(nowPlayingItem?['RunTimeTicks'] as int?, json),
      streams: StreamInfo.fromSessionJson(json),
      transcoding: json['TranscodingInfo'] != null
          ? TranscodingInfo.fromSessionJson(json['TranscodingInfo'])
          : null,
      isCompleted: false,
      isPaused: json['PlayState']['IsPaused'] ?? false,
    );
  }
}
