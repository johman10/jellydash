import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/types/session.dart';
import 'package:jellydash/widgets/current_activity_card.dart';

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
  }) {
    return Session(
      userName: 'User',
      client: 'Web',
      deviceName: 'Device',
      name: name ?? 'Title',
      season: season,
      episode: episode,
      year: year,
      imageUrl: '',
      video: SessionVideo(),
      audio: SessionAudio(),
      subtitles: SessionSubtitle(),
      transcodingInfo:
          TranscodingInfo(video: SessionVideo(), audio: SessionAudio(), reasons: []),
      progress: const Duration(minutes: 1),
      duration: const Duration(minutes: 10),
    );
  }

  group('CurrentActivityCard season/episode vs year display', () {
    testWidgets('shows season and episode when both are > 0', (tester) async {
      final session = baseSession(season: 1, episode: 2, year: 2024);

      await tester.pumpWidget(wrap(CurrentActivityCard(session: session)));

      expect(find.text('S01 · E02'), findsOneWidget);
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
}
