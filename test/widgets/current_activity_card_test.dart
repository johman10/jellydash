import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/widgets/current_activity_card.dart';
import 'package:jellydash/types/session.dart';

void main() {
  group('CurrentActivityCard', () {
    testWidgets('renders session details', (WidgetTester tester) async {
      final session = Session(
        userName: 'Alice',
        name: 'Movie',
        season: 1,
        episode: 2,
        video: SessionVideo(),
        audio: SessionAudio(),
        subtitles: SessionSubtitle(),
        transcodingInfo: TranscodingInfo(
          video: SessionVideo(),
          audio: SessionAudio(),
          reasons: [],
        ),
        progress: 0.5,
      );
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: CurrentActivityCard(session: session),
          ),
        ),
      );
      expect(find.textContaining('Alice'), findsOneWidget);
      expect(find.textContaining('Movie'), findsOneWidget);
      expect(find.textContaining('1x2'), findsOneWidget);
    });

    testWidgets('handles null values gracefully', (WidgetTester tester) async {
      final session = Session(
        video: SessionVideo(),
        audio: SessionAudio(),
        subtitles: SessionSubtitle(),
        transcodingInfo: TranscodingInfo(
          video: SessionVideo(),
          audio: SessionAudio(),
          reasons: [],
        ),
        progress: 0.0,
      );
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: CurrentActivityCard(session: session),
          ),
        ),
      );
      expect(find.textContaining('Unknown'), findsOneWidget);
    });

    testWidgets('renders with all fields null/empty', (WidgetTester tester) async {
      final session = Session(
        userName: null,
        name: null,
        season: null,
        episode: null,
        video: SessionVideo(),
        audio: SessionAudio(),
        subtitles: SessionSubtitle(),
        transcodingInfo: TranscodingInfo(video: SessionVideo(), audio: SessionAudio(), reasons: []),
        progress: 0.0,
      );
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: CurrentActivityCard(session: session),
          ),
        ),
      );
      expect(find.textContaining('Unknown'), findsOneWidget);
    });

    testWidgets('renders with special characters and long names', (WidgetTester tester) async {
      final session = Session(
        userName: 'A!@#%&*()_+|~',
        name: 'VeryLongMovieNameThatExceedsNormalLength1234567890',
        season: 10,
        episode: 99,
        video: SessionVideo(),
        audio: SessionAudio(),
        subtitles: SessionSubtitle(),
        transcodingInfo: TranscodingInfo(video: SessionVideo(), audio: SessionAudio(), reasons: []),
        progress: 0.8,
      );
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: CurrentActivityCard(session: session),
          ),
        ),
      );
      expect(find.textContaining('A!@#%&*()_+|~'), findsOneWidget);
      expect(find.textContaining('VeryLongMovieNameThatExceedsNormalLength1234567890'), findsOneWidget);
      expect(find.textContaining('10x99'), findsOneWidget);
    });

    testWidgets('renders with zero/negative season and episode', (WidgetTester tester) async {
      final session = Session(
        userName: 'ZeroUser',
        name: 'ZeroShow',
        season: 0,
        episode: -1,
        video: SessionVideo(),
        audio: SessionAudio(),
        subtitles: SessionSubtitle(),
        transcodingInfo: TranscodingInfo(video: SessionVideo(), audio: SessionAudio(), reasons: []),
        progress: 0.2,
      );
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: CurrentActivityCard(session: session),
          ),
        ),
      );
      expect(find.textContaining('0x-1'), findsOneWidget);
    });

    testWidgets('card is present and readable (accessibility)', (WidgetTester tester) async {
      final session = Session(
        userName: 'Accessible',
        name: 'Show',
        season: 1,
        episode: 1,
        video: SessionVideo(),
        audio: SessionAudio(),
        subtitles: SessionSubtitle(),
        transcodingInfo: TranscodingInfo(video: SessionVideo(), audio: SessionAudio(), reasons: []),
        progress: 0.5,
      );
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: CurrentActivityCard(session: session),
          ),
        ),
      );
      expect(find.byType(Card), findsOneWidget);
      expect(find.textContaining('Accessible'), findsOneWidget);
    });
  });
}
