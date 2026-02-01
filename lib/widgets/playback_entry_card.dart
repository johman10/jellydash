import 'package:flutter/material.dart';
import 'package:jellydash/types/playback_entry.dart';
import 'package:jellydash/widgets/poster.dart';
import 'package:jellydash/widgets/playback_progress.dart';
import 'package:jellydash/widgets/transcoding_badges.dart';
import 'package:jellydash/widgets/user_avatar.dart';

class PlaybackEntryCard extends StatelessWidget {
  final PlaybackEntry entry;
  const PlaybackEntryCard({super.key, required this.entry});

  Duration get progress =>
      Duration(microseconds: (entry.timing.endPositionTicks ?? 0) ~/ 10);
  Duration get duration =>
      Duration(microseconds: (entry.timing.runtimeTicks ?? 0) ~/ 10);

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
    final season = entry.identity.seasonNumber ?? 0;
    final episode = entry.identity.episodeNumber ?? 0;
    if (season > 0 && episode > 0) {
      return 'S${season.toString()} · E${episode.toString()}';
    } else if (entry.identity.year != null) {
      return '${entry.identity.year}';
    } else {
      return '';
    }
  }

  Text timeLeft(BuildContext context, ThemeData theme) {
    var text = '';
    if (entry.isCompleted && entry.timing.endTime != null) {
      final endTime = entry.timing.endTime!.toLocal();
      final now = DateTime.now();
      final difference = now.difference(endTime);

      if (difference.inMinutes < 3) {
        text = 'Just now';
      } else if (difference.inMinutes < 60) {
        text = '${difference.inMinutes}m ago';
      } else if (difference.inHours < 24) {
        text = '${difference.inHours}h ago';
      } else {
        text = '${difference.inDays}d ago';
      }
    } else {
      final remaining = duration - progress;
      final clampedRemaining = remaining.isNegative ? Duration.zero : remaining;
      final endDateTime = DateTime.now().add(clampedRemaining);
      final endTimeOfDay = TimeOfDay.fromDateTime(endDateTime).format(context);

      final minutesLeft = clampedRemaining.inMinutes;
      text = '$minutesLeft min left';
      if (!entry.isPaused) {
        text = '$text ($endTimeOfDay)';
      }
    }

    return Text(
      text,
      style: theme.textTheme.bodyMedium?.copyWith(color: theme.hintColor),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final imageUrl = entry.identity.primaryImageUrl != null
        ? entry.identity.primaryImageUrl!
        : '';
    final transcoding = entry.transcoding;
    final bitrateText = formatBitrate(entry.transcoding?.bitrate ?? 0);

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
                EntityPoster(
                  imageUrl: imageUrl,
                  isPaused: entry.isPaused,
                ),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Title
                      Text(
                        entry.contentType == ContentType.episode
                            ? (entry.identity.seriesName ?? '')
                            : entry.identity.title,
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
                      timeLeft(context, theme)
                    ],
                  ),
                ),
              ],
            ),
            PlaybackProgress(
              progress: progress,
              duration: duration,
              transcodingProgressPercent:
                  transcoding?.completionPercentage ?? 0,
            ),
            Row(
              spacing: 16,
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                UserAvatar(
                  userName: entry.user.userName,
                  userImageUrl: entry.user.userImageUrl,
                ),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        entry.user.userName,
                        style: theme.textTheme.bodyMedium,
                        overflow: TextOverflow.ellipsis,
                      ),
                      Text(
                        '${entry.client.clientName} · ${entry.client.deviceName}',
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
                  isAudioDirectStream: transcoding?.isAudioDirect ?? false,
                  isVideoDirectStream: transcoding?.isVideoDirect ?? false,
                ),
              ],
            )
          ],
        ),
      ),
    );
  }
}
