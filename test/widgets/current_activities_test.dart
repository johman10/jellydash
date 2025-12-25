import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/widgets/current_activities.dart';
import 'package:jellydash/widgets/current_activity_card.dart';
import 'package:jellydash/types/session.dart' show Session, SessionVideo, SessionAudio, SessionSubtitle, TranscodingInfo;

void main() {
  group('CurrentActivities', () {
    testWidgets('renders loading indicator when loading', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: CurrentActivities(isLoading: true, sessions: []),
          ),
        ),
      );
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });

    testWidgets('renders no activities text when not loading and sessions empty', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: CurrentActivities(isLoading: false, sessions: []),
          ),
        ),
      );
      expect(find.text('No current activities.'), findsOneWidget);
    });

    testWidgets('renders activity cards when sessions are present', (WidgetTester tester) async {
      final session = Session(
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
            body: CurrentActivities(isLoading: false, sessions: [session]),
          ),
        ),
      );
      // Should find at least one CurrentActivityCard
      expect(find.byType(CurrentActivities), findsOneWidget);
      expect(find.byType(CurrentActivityCard), findsWidgets);
    });
  });
}
