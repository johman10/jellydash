import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/types/session.dart';
import 'package:jellydash/widgets/current_activity_card.dart';
import 'package:jellydash/theme/jellydash_theme.dart';

void main() {
  Widget wrap(Widget child) {
    return MaterialApp(
      home: Scaffold(
        body: Center(child: child),
      ),
    );
  }

  Session baseSession({
    String? name,
    int? season,
    int? episode,
    int? year,
    Duration? progress,
    Duration? duration,
    int? bitrate,
    TranscodingInfo? transcodingInfo,
    String? userName,
    String? userImageUrl,
    String? imageUrl,
    bool isPaused = false,
  }) {
    return Session(
      userName: userName ?? 'User',
      client: 'Web',
      deviceName: 'Device',
      name: name ?? 'Title',
      season: season,
      episode: episode,
      year: year,
      imageUrl: imageUrl ?? '',
      video: SessionVideo(),
      audio: SessionAudio(),
      subtitles: SessionSubtitle(),
      transcodingInfo: transcodingInfo ??
          TranscodingInfo(video: SessionVideo(), audio: SessionAudio(), reasons: []),
      progress: progress ?? const Duration(minutes: 1),
      duration: duration ?? const Duration(minutes: 10),
      bitrate: bitrate,
      userImageUrl: userImageUrl,
      isPaused: isPaused,
    );
  }

  group('CurrentActivityCard season/episode vs year display', () {
    testWidgets('shows season and episode when both are > 0', (tester) async {
      final session = baseSession(season: 1, episode: 2, year: 2024);

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.text('S1 · E2'), findsOneWidget);
      expect(find.text('2024'), findsNothing);
    });

    testWidgets('shows year when no season/episode but year is set', (tester) async {
      final session = baseSession(season: 0, episode: 0, year: 2024);

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.text('2024'), findsOneWidget);
      expect(find.textContaining('S0'), findsNothing);
    });

    testWidgets('shows neither season/episode nor year when not provided', (tester) async {
      final session = baseSession(season: 0, episode: 0, year: null);

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.textContaining('S0'), findsNothing);
      expect(find.text('null'), findsNothing);
    });
  });

  group('CurrentActivityCard remaining time display', () {
    testWidgets('shows correct minutes left for normal progress', (tester) async {
      final session = baseSession(
        progress: const Duration(minutes: 3),
        duration: const Duration(minutes: 10),
      );

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.textContaining('7 min left'), findsOneWidget);
    });

    testWidgets('clamps remaining minutes to zero when progress exceeds duration',
        (tester) async {
      final session = baseSession(
        progress: const Duration(minutes: 15),
        duration: const Duration(minutes: 10),
      );

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.textContaining('0 min left'), findsOneWidget);
    });
  });

  group('CurrentActivityCard bitrate display', () {
    testWidgets('does not show bitrate when value is null or non-positive',
        (tester) async {
      final session = baseSession(bitrate: 0);

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.textContaining('bps'), findsNothing);
      expect(find.textContaining('kbps'), findsNothing);
      expect(find.textContaining('Mbps'), findsNothing);
    });

    testWidgets('shows formatted Mbps bitrate when high value is provided',
        (tester) async {
      final session = baseSession(bitrate: 2500000);

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.text('2.5Mbps'), findsOneWidget);
    });
  });

  group('CurrentActivityCard transcoding badges', () {
    testWidgets('shows V and A when video and audio are transcoding', (tester) async {
      final transcodingInfo = TranscodingInfo(
        video: SessionVideo(isDirectStream: false),
        audio: SessionAudio(isDirectStream: false),
        reasons: const [],
      );
      final session = baseSession(transcodingInfo: transcodingInfo);

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.text('V'), findsOneWidget);
      expect(find.text('A'), findsOneWidget);
    });

    testWidgets('hides badges when both streams are direct', (tester) async {
      final transcodingInfo = TranscodingInfo(
        video: SessionVideo(isDirectStream: true),
        audio: SessionAudio(isDirectStream: true),
        reasons: const [],
      );
      final session = baseSession(transcodingInfo: transcodingInfo);

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.text('V'), findsNothing);
      expect(find.text('A'), findsNothing);
    });
  });

  group('CurrentActivityCard avatars and poster fallback', () {
    testWidgets('uses user initial when no user image URL', (tester) async {
      final transcodingInfo = TranscodingInfo(
        video: SessionVideo(isDirectStream: true),
        audio: SessionAudio(isDirectStream: true),
        reasons: const [],
      );
      final session = baseSession(
        userName: 'Alice',
        userImageUrl: null,
        transcodingInfo: transcodingInfo,
      );

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.text('A'), findsOneWidget);
    });

    testWidgets('uses PosterFallback when no imageUrl', (tester) async {
      final session = baseSession(imageUrl: '');

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

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
      final session = baseSession(isPaused: true);

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.byIcon(Icons.pause_circle_filled), findsOneWidget);
    });

    testWidgets('does not show pause icon overlay when session is not paused',
        (tester) async {
      final session = baseSession(isPaused: false);

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.byIcon(Icons.pause_circle_filled), findsNothing);
    });
  });
}
