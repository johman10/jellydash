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
  });
}
