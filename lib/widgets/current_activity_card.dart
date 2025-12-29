import 'package:flutter/material.dart';
import '../types/session.dart';

class CurrentActivityCard extends StatelessWidget {
  final Session session;
  const CurrentActivityCard({super.key, required this.session});

  Color colorFromString(String input) {
    // Simple hash function
    int hash = 0;
    for (int i = 0; i < input.length; i++) {
      hash = input.codeUnitAt(i) + ((hash << 5) - hash);
    }
    // Generate RGB values from hash
    int r = (hash & 0xFF);
    int g = ((hash >> 8) & 0xFF);
    int b = ((hash >> 16) & 0xFF);
    // Optionally, keep colors in a visually pleasant range
    r = 100 + (r % 156); // 100-255
    g = 100 + (g % 156);
    b = 100 + (b % 156);
    return Color.fromARGB(255, r, g, b);
  }

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

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    // Placeholder values for missing fields
    final imageUrl = session.imageUrl ?? '';
    final title = session.name ?? 'Unknown Title';
    final progress = session.progress;
    final duration = session.duration;
    final remaining = duration - progress;
    final minutesLeft = remaining.isNegative ? 0 : remaining.inMinutes;
    final userName = session.userName ?? 'Unknown';
    final deviceName = session.deviceName ?? 'Unknown Device';
    final client = session.client ?? '';
    final transcoding = session.transcodingInfo;
    final bitrate = session.bitrate ?? 0;
    final season = session.season ?? 0;
    final episode = session.episode ?? 0;

    const posterHeight = 75.0;
    const posterWidth = 50.0;

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
                ClipRRect(
                  borderRadius: BorderRadius.circular(4),
                  child: imageUrl.isNotEmpty
                      ? Image.network(
                          imageUrl,
                          width: posterWidth,
                          height: posterHeight,
                          fit: BoxFit.cover,
                          errorBuilder: (context, error, stackTrace) =>
                              Container(
                            width: posterWidth,
                            height: posterHeight,
                            color: Colors.grey[800],
                            child: const Icon(
                              Icons.movie,
                              size: 48,
                              color: Colors.white54,
                            ),
                          ),
                        )
                      : Container(
                          width: posterWidth,
                          height: posterHeight,
                          color: Colors.grey[800],
                          child: const Icon(
                            Icons.movie,
                            size: 48,
                            color: Colors.white54,
                          ),
                        ),
                ),
                // Main content
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Title
                      Text(
                        title,
                        style: theme.textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.bold),
                        overflow: TextOverflow.ellipsis,
                        maxLines: 1,
                      ),
                      Text(
                        'S${season.toString().padLeft(2, '0')} · E${episode.toString().padLeft(2, '0')}',
                        style: theme.textTheme.bodyMedium
                            ?.copyWith(color: theme.hintColor),
                      ),
                      Text(
                        '$minutesLeft min left',
                        style: theme.textTheme.bodyMedium
                            ?.copyWith(color: theme.hintColor),
                      )
                    ],
                  ),
                ),
              ],
            ),
            Row(children: [
              Expanded(
                child: SliderTheme(
                  data: SliderTheme.of(context).copyWith(
                    trackHeight: 4,
                    thumbShape: SliderComponentShape.noThumb,
                    overlayShape: SliderComponentShape.noOverlay,
                  ),
                  child: Slider(
                    padding: EdgeInsets.zero,
                    mouseCursor: SystemMouseCursors.basic,
                    value: (progress.inMilliseconds == 0)
                        ? 0.0
                        : (progress.inMilliseconds / duration.inMilliseconds)
                            .clamp(0.0, 1.0),
                    secondaryTrackValue:
                        (transcoding.progress / 100.0).clamp(0.0, 1.0),
                    onChanged: (double x) {},
                    activeColor: Colors.purpleAccent,
                    secondaryActiveColor: Colors.grey[600],
                  ),
                ),
              ),
            ]),
            Row(
              spacing: 16,
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                Container(
                  width: posterWidth,
                  height: 35,
                  decoration: BoxDecoration(
                    color: colorFromString(userName),
                    shape: BoxShape.circle,
                  ),
                  alignment: Alignment.center,
                  child: session.userImageUrl != null &&
                          session.userImageUrl!.isNotEmpty
                      ? Image.network(
                          session.userImageUrl!,
                          width: 25,
                          height: 25,
                          fit: BoxFit.cover,
                          errorBuilder: (context, error, stackTrace) => Text(
                              userName.substring(0, 1).toUpperCase(),
                              style: const TextStyle(
                                  fontSize: 16, color: Colors.white54)),
                        )
                      : Text(userName.substring(0, 1).toUpperCase(),
                          style: const TextStyle(
                              fontSize: 16, color: Colors.white54)),
                ),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(session.userName ?? 'Unknown User',
                          style: theme.textTheme.bodyMedium,
                          overflow: TextOverflow.ellipsis),
                      Text(
                        '$client · $deviceName',
                        style: theme.textTheme.bodyMedium
                            ?.copyWith(color: theme.hintColor),
                        overflow: TextOverflow.ellipsis,
                      ),
                      formatBitrate(bitrate) != null
                          ? Text(
                              formatBitrate(bitrate)!,
                              style: theme.textTheme.bodyMedium
                                  ?.copyWith(color: theme.hintColor),
                              overflow: TextOverflow.ellipsis,
                            )
                          : const SizedBox.shrink(),
                    ],
                  ),
                ),
                if (!transcoding.audio.isDirectStream ||
                    !transcoding.video.isDirectStream) ...[
                  Column(
                    spacing: 2,
                    children: [
                      !transcoding.video.isDirectStream
                          ? Container(
                              height: 20,
                              width: 20,
                              decoration: BoxDecoration(
                                borderRadius: BorderRadius.circular(4),
                                color: Colors.green,
                              ),
                              alignment: Alignment.center,
                              child: const Text('V'),
                            )
                          : const SizedBox.shrink(),
                      !transcoding.audio.isDirectStream
                          ? Container(
                              height: 20,
                              width: 20,
                              decoration: BoxDecoration(
                                borderRadius: BorderRadius.circular(4),
                                color: Colors.blue,
                              ),
                              alignment: Alignment.center,
                              child: const Text('A'),
                            )
                          : const SizedBox.shrink(),
                    ],
                  )
                ],
              ],
            )
          ],
        ),
      ),
    );
  }
}
