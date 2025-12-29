import 'package:flutter/material.dart';
import 'package:jellydash/theme/jellydash_theme.dart';
import '../types/session.dart';

const posterHeight = 75.0;
const posterWidth = 50.0;

class TranscodingBadges extends StatelessWidget {
  final bool isAudioDirectStream;
  final bool isVideoDirectStream;

  const TranscodingBadges({
    super.key,
    required this.isAudioDirectStream,
    required this.isVideoDirectStream,
  });

  @override
  Widget build(BuildContext context) {
    if (isAudioDirectStream && isVideoDirectStream) {
      return const SizedBox.shrink();
    }

    return Column(
      spacing: 2,
      children: [
        !isVideoDirectStream
            ? _buildBadge(context, 'V', 'Video transcoding')
            : const SizedBox.shrink(),
        !isAudioDirectStream
            ? _buildBadge(context, 'A', 'Audio transcoding')
            : const SizedBox.shrink(),
      ],
    );
  }

  Widget _buildBadge(
    BuildContext context,
    String label,
    String semanticsLabel,
  ) {
    return Container(
      height: 20,
      width: 20,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(4),
        color: JellydashColors.transcodingBadgeBackground(context),
      ),
      alignment: Alignment.center,
      child: Semantics(
        label: semanticsLabel,
        child: Text(label),
      ),
    );
  }
}

class UserAvatar extends StatelessWidget {
  final String userName;
  final String? userImageUrl;

  const UserAvatar({
    super.key,
    required this.userName,
    this.userImageUrl,
  });

  Color _colorFromString(String input) {
    int hash = 0;
    for (int i = 0; i < input.length; i++) {
      hash = input.codeUnitAt(i) + ((hash << 5) - hash);
    }
    int r = (hash & 0xFF);
    int g = ((hash >> 8) & 0xFF);
    int b = ((hash >> 16) & 0xFF);
    r = 100 + (r % 156);
    g = 100 + (g % 156);
    b = 100 + (b % 156);
    return Color.fromARGB(255, r, g, b);
  }

  Widget get userImageFallback {
    return Text(
      userName.substring(0, 1).toUpperCase(),
      style: JellydashTextStyles.userAvatarFallback,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: 'User: $userName',
      child: Container(
        width: posterWidth,
        height: 35,
        decoration: BoxDecoration(
          color: _colorFromString(userName),
          shape: BoxShape.circle,
        ),
        alignment: Alignment.center,
        child: userImageUrl != null && userImageUrl!.isNotEmpty
            ? Image.network(
                userImageUrl!,
                width: 25,
                height: 25,
                fit: BoxFit.cover,
                loadingBuilder: (context, child, loadingProgress) {
                  if (loadingProgress == null) return child;
                  return userImageFallback;
                },
                errorBuilder: (context, error, stackTrace) => userImageFallback,
              )
            : userImageFallback,
      ),
    );
  }
}

class SessionProgressSlider extends StatelessWidget {
  final Duration progress;
  final Duration duration;
  final double transcodingProgressPercent;

  const SessionProgressSlider({
    super.key,
    required this.progress,
    required this.duration,
    required this.transcodingProgressPercent,
  });

  @override
  Widget build(BuildContext context) {
    final totalMs = duration.inMilliseconds;
    final progressMs = progress.inMilliseconds;
    final sliderValue = (totalMs <= 0 || progressMs <= 0)
        ? 0.0
        : (progressMs / totalMs).clamp(0.0, 1.0);

    final progressPercent = (sliderValue * 100).round().clamp(0, 100);

    return Row(children: [
      Expanded(
        child: Semantics(
          label: 'Playback progress',
          value: '$progressPercent%',
          readOnly: true,
          child: IgnorePointer(
            child: SliderTheme(
              data: SliderTheme.of(context).copyWith(
                trackHeight: 4,
                thumbShape: SliderComponentShape.noThumb,
                overlayShape: SliderComponentShape.noOverlay,
              ),
              child: Slider(
                padding: EdgeInsets.zero,
                mouseCursor: SystemMouseCursors.basic,
                value: sliderValue,
                secondaryTrackValue:
                    (transcodingProgressPercent / 100.0).clamp(0.0, 1.0),
                onChanged: (double _) {},
                activeColor: JellydashColors.sliderActive,
                secondaryActiveColor: JellydashColors.sliderSecondaryActive,
              ),
            ),
          ),
        ),
      ),
    ]);
  }
}

class SessionPoster extends StatelessWidget {
  final String imageUrl;
  final bool isPaused;

  const SessionPoster({
    super.key,
    required this.imageUrl,
    required this.isPaused,
  });

  Widget get posterFallback {
    return Container(
      width: posterWidth,
      height: posterHeight,
      color: JellydashColors.posterFallbackBackground,
      child: const Icon(
        Icons.movie,
        size: 48,
        color: JellydashColors.posterFallbackIcon,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(4),
      child: Stack(
        children: [
          imageUrl.isNotEmpty
              ? Image.network(
                  imageUrl,
                  width: posterWidth,
                  height: posterHeight,
                  fit: BoxFit.cover,
                  loadingBuilder: (context, child, loadingProgress) {
                    if (loadingProgress == null) return child;
                    return posterFallback;
                  },
                  errorBuilder: (context, error, stackTrace) =>
                      posterFallback,
                )
              : posterFallback,
          if (isPaused)
            Container(
              width: posterWidth,
              height: posterHeight,
              color: JellydashColors.pausedOverlayBackground,
              alignment: Alignment.center,
              child: Semantics(
                label: 'Playback paused',
                child: const Icon(
                  Icons.pause_circle_filled,
                  color: JellydashColors.pausedOverlayIcon,
                  size: 32,
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class CurrentActivityCard extends StatelessWidget {
  final Session session;
  const CurrentActivityCard({super.key, required this.session});

  String? formatBitrate(int bps) {
    if (bps <= 0) return null;
    if (bps >= 1000000) {
      final mbps = bps / 1000000;
      return '${mbps.toStringAsFixed(1)}Mbps';
    }
    if (bps >= 1000) {
      final kbps = bps / 1000;
      return '${kbps.toStringAsFixed(0)}kbps';
    }
    return '$bps bps';
  }

  String get subtitle {
    final season = session.season ?? 0;
    final episode = session.episode ?? 0;
    if (season > 0 && episode > 0) {
      return 'S${season.toString()} · E${episode.toString()}';
    } else if (session.year != null) {
      return '${session.year}';
    } else {
      return '';
    }
  }

  String get displayUserName {
    final name = session.userName?.trim();
    if (name != null && name.isNotEmpty) {
      return name;
    }
    return '—';
  }

  String? get displayClientDevice {
    final client = session.client?.trim();
    final device = session.deviceName?.trim();
    final hasClient = client != null && client.isNotEmpty;
    final hasDevice = device != null && device.isNotEmpty;

    if (hasClient && hasDevice) {
      return '$client · $device';
    }
    if (hasClient) {
      return client;
    }
    if (hasDevice) {
      return device;
    }
    return null;
  }

  String get displayTitle {
    final name = session.name?.trim();
    if (name != null && name.isNotEmpty) {
      return name;
    }
    return '—';
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final imageUrl = session.imageUrl ?? '';
    final progress = session.progress;
    final duration = session.duration;
    final remaining = duration - progress;
    final clampedRemaining = remaining.isNegative ? Duration.zero : remaining;
    final minutesLeft = clampedRemaining.inMinutes;
    final endDateTime = DateTime.now().add(clampedRemaining);
    final endTimeOfDay = TimeOfDay.fromDateTime(endDateTime);
    final transcoding = session.transcodingInfo;
    final bitrateText = formatBitrate(session.bitrate ?? 0);

    return Card(
      margin: const EdgeInsets.symmetric(vertical: 8, horizontal: 0),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          spacing: 8,
          children: [
            Row(
              spacing: 16,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Poster
                SessionPoster(
                  imageUrl: imageUrl,
                  isPaused: session.isPaused,
                ),
                // Main content
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Title
                      Text(
                        displayTitle,
                        style: theme.textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.bold),
                        overflow: TextOverflow.ellipsis,
                        maxLines: 1,
                      ),
                        subtitle.isNotEmpty
                          ? Text(
                              subtitle,
                              style: theme.textTheme.bodyMedium
                                  ?.copyWith(color: theme.hintColor),
                            )
                          : const SizedBox.shrink(),
                      Text(
                        '$minutesLeft min left (${endTimeOfDay.format(context)})',
                        style: theme.textTheme.bodyMedium
                            ?.copyWith(color: theme.hintColor),
                      )
                    ],
                  ),
                ),
              ],
            ),
            SessionProgressSlider(
              progress: progress,
              duration: duration,
              transcodingProgressPercent: transcoding.progress,
            ),
            Row(
              spacing: 16,
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                UserAvatar(
                  userName: displayUserName,
                  userImageUrl: session.userImageUrl,
                ),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        displayUserName,
                        style: theme.textTheme.bodyMedium,
                        overflow: TextOverflow.ellipsis,
                      ),
                      if (displayClientDevice != null)
                        Text(
                          displayClientDevice!,
                          style: theme.textTheme.bodyMedium
                              ?.copyWith(color: theme.hintColor),
                          overflow: TextOverflow.ellipsis,
                        ),
                      bitrateText != null
                          ? Text(
                              bitrateText,
                              style: theme.textTheme.bodyMedium
                                  ?.copyWith(color: theme.hintColor),
                              overflow: TextOverflow.ellipsis,
                            )
                          : const SizedBox.shrink(),
                    ],
                  ),
                ),
                TranscodingBadges(
                  isAudioDirectStream: transcoding.audio.isDirectStream,
                  isVideoDirectStream: transcoding.video.isDirectStream,
                ),
              ],
            )
          ],
        ),
      ),
    );
  }
}
