import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/types/playback_entry.dart';
import 'package:jellydash/widgets/playback_entry_card.dart';
import 'package:jellydash/theme/jellydash_theme.dart';

void main() {
  Widget wrap(Widget child) {
    return MaterialApp(
      home: Scaffold(
        body: Center(child: child),
      ),
    );
  }

  int ticksFromDuration(Duration duration) => duration.inMicroseconds * 10;

  PlaybackEntry baseEntry({
    String? title,
    String? seriesName,
    ContentType contentType = ContentType.other,
    int? season,
    int? episode,
    int? year,
    Duration? progress,
    Duration? duration,
    int? bitrate,
    bool isVideoDirect = true,
    bool isAudioDirect = true,
    String? userName,
    String? userImageUrl,
    String? imageUrl,
    bool isPaused = false,
  }) {
    final effectiveDuration = duration ?? const Duration(minutes: 10);
    final effectiveProgress = progress ?? const Duration(minutes: 1);

    return PlaybackEntry(
      itemId: 'item1',
      parentItemId: null,
      contentType: contentType,
      identity: ContentIdentity(
        primaryImageUrl: imageUrl,
        title: title ?? 'Title',
        seriesName: seriesName,
        seasonNumber: season,
        episodeNumber: episode,
        year: year,
      ),
      user: UserInfo(
        userId: 'user1',
        userName: userName ?? 'User',
        userImageUrl: userImageUrl,
      ),
      client: const ClientInfo(deviceName: 'Device', clientName: 'Web'),
      timing: TimingInfo(
        runtimeTicks: ticksFromDuration(effectiveDuration),
        endPositionTicks: ticksFromDuration(effectiveProgress),
      ),
      streams: const StreamInfo(),
      transcoding: bitrate == null
          ? null
          : TranscodingInfo(
              isVideoDirect: isVideoDirect,
              isAudioDirect: isAudioDirect,
              bitrate: bitrate,
              reasons: const [],
            ),
      isCompleted: false,
      isPaused: isPaused,
    );
  }

  group('CurrentActivityCard season/episode vs year display', () {
    testWidgets('shows season and episode when both are > 0', (tester) async {
      final session = baseEntry(season: 1, episode: 2, year: 2024);

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.text('S1 · E2'), findsOneWidget);
      expect(find.text('2024'), findsNothing);
    });

    testWidgets('shows year when no season/episode but year is set',
        (tester) async {
      final session = baseEntry(season: 0, episode: 0, year: 2024);

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.text('2024'), findsOneWidget);
      expect(find.textContaining('S0'), findsNothing);
    });

    testWidgets('shows neither season/episode nor year when not provided',
        (tester) async {
      final session = baseEntry(season: 0, episode: 0, year: null);

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.textContaining('S0'), findsNothing);
      expect(find.text('null'), findsNothing);
    });
  });

  group('CurrentActivityCard title display', () {
    testWidgets('prefers seriesName over title when episode', (tester) async {
      final entry = baseEntry(
        seriesName: 'Series Title',
        title: 'Episode Title',
        contentType: ContentType.episode,
        season: 1,
        episode: 1,
      );

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: entry)));

      expect(find.text('Series Title'), findsOneWidget);
      expect(find.text('Episode Title'), findsNothing);
    });

    testWidgets('prefers title when not episode', (tester) async {
      final entry = baseEntry(
        seriesName: null,
        title: 'Movie Title',
        contentType: ContentType.movie,
        year: 2024,
      );

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: entry)));

      expect(find.text('Movie Title'), findsOneWidget);
    });
  });

  group('CurrentActivityCard remaining time display', () {
    testWidgets('shows correct minutes left for normal progress',
        (tester) async {
      final session = baseEntry(
        progress: const Duration(minutes: 3),
        duration: const Duration(minutes: 10),
      );

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.textContaining('7 min left'), findsOneWidget);
    });

    testWidgets(
        'clamps remaining minutes to zero when progress exceeds duration',
        (tester) async {
      final session = baseEntry(
        progress: const Duration(minutes: 15),
        duration: const Duration(minutes: 10),
      );

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.textContaining('0 min left'), findsOneWidget);
    });
  });

  group('CurrentActivityCard bitrate display', () {
    testWidgets('does not show bitrate when value is null or non-positive',
        (tester) async {
      final session = baseEntry(bitrate: 0);

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.textContaining('bps'), findsNothing);
      expect(find.textContaining('kbps'), findsNothing);
      expect(find.textContaining('Mbps'), findsNothing);
    });

    testWidgets('shows formatted Mbps bitrate when high value is provided',
        (tester) async {
      final session = baseEntry(bitrate: 2500000);

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.text('2.5Mbps'), findsOneWidget);
    });

    testWidgets('shows formatted kbps bitrate for 1000 bps', (tester) async {
      final session = baseEntry(bitrate: 1000);

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.text('1kbps'), findsOneWidget);
    });
  });

  group('CurrentActivityCard transcoding badges', () {
    testWidgets('shows V and A when video and audio are transcoding',
        (tester) async {
      final session = baseEntry(
        bitrate: 1000,
        isVideoDirect: false,
        isAudioDirect: false,
      );

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.text('V'), findsOneWidget);
      expect(find.text('A'), findsOneWidget);
    });

    testWidgets('hides badges when both streams are direct', (tester) async {
      final session = baseEntry(
        bitrate: 1000,
        isVideoDirect: true,
        isAudioDirect: true,
      );

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.text('V'), findsNothing);
      expect(find.text('A'), findsNothing);
    });
  });

  group('CurrentActivityCard avatars and poster fallback', () {
    testWidgets('uses user initial when no user image URL', (tester) async {
      final session = baseEntry(
        userName: 'Alice',
        userImageUrl: null,
        bitrate: 1000,
      );

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.text('A'), findsOneWidget);
    });

    testWidgets('uses PosterFallback when no imageUrl', (tester) async {
      final session = baseEntry(imageUrl: null);

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      final iconFinder = find.byIcon(Icons.movie);
      expect(iconFinder, findsOneWidget);

      final icon = tester.widget<Icon>(iconFinder);
      expect(icon.size, 48);
      expect(icon.color, JellydashColors.posterFallbackIcon);
    });
  });

  group('CurrentActivityCard paused overlay', () {
    testWidgets('shows pause icon overlay when session is paused',
        (tester) async {
      final session = baseEntry(isPaused: true);

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.byIcon(Icons.pause_circle_filled), findsOneWidget);
    });

    testWidgets('does not show pause icon overlay when session is not paused',
        (tester) async {
      final session = baseEntry(isPaused: false);

      await tester.pumpWidget(wrap(PlaybackEntryCard(entry: session)));

      expect(find.byIcon(Icons.pause_circle_filled), findsNothing);
    });
  });
}
