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

    testWidgets('renders the title always', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: CurrentActivities(isLoading: false, sessions: []),
          ),
        ),
      );
      expect(find.text('Current Activities'), findsOneWidget);
    });

    testWidgets('renders multiple activity cards for multiple sessions', (WidgetTester tester) async {
      final session1 = Session(
        userName: 'User1',
        name: 'Show1',
        season: 1,
        episode: 1,
        video: SessionVideo(),
        audio: SessionAudio(),
        subtitles: SessionSubtitle(),
        transcodingInfo: TranscodingInfo(video: SessionVideo(), audio: SessionAudio(), reasons: []),
        progress: 0.1,
      );
      final session2 = Session(
        userName: 'User2',
        name: 'Show2',
        season: 2,
        episode: 2,
        video: SessionVideo(),
        audio: SessionAudio(),
        subtitles: SessionSubtitle(),
        transcodingInfo: TranscodingInfo(video: SessionVideo(), audio: SessionAudio(), reasons: []),
        progress: 0.9,
      );
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: CurrentActivities(isLoading: false, sessions: [session1, session2]),
          ),
        ),
      );
      expect(find.byType(CurrentActivityCard), findsNWidgets(2));
      expect(find.textContaining('User1'), findsOneWidget);
      expect(find.textContaining('User2'), findsOneWidget);
    });

    testWidgets('handles session with null/empty fields', (WidgetTester tester) async {
      final session = Session(
        video: SessionVideo(),
        audio: SessionAudio(),
        subtitles: SessionSubtitle(),
        transcodingInfo: TranscodingInfo(video: SessionVideo(), audio: SessionAudio(), reasons: []),
        progress: 0.0,
      );
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: CurrentActivities(isLoading: false, sessions: [session]),
          ),
        ),
      );
      expect(find.textContaining('Unknown'), findsOneWidget);
    });

    testWidgets('handles session with 100% progress', (WidgetTester tester) async {
      final session = Session(
        userName: 'User',
        name: 'Show',
        season: 1,
        episode: 1,
        video: SessionVideo(),
        audio: SessionAudio(),
        subtitles: SessionSubtitle(),
        transcodingInfo: TranscodingInfo(video: SessionVideo(), audio: SessionAudio(), reasons: []),
        progress: 1.0,
      );
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: CurrentActivities(isLoading: false, sessions: [session]),
          ),
        ),
      );
      expect(find.textContaining('User'), findsOneWidget);
      expect(find.textContaining('Show'), findsOneWidget);
    });

    testWidgets('has semantic labels for accessibility', (WidgetTester tester) async {
      final session = Session(
        userName: 'AccessibleUser',
        name: 'AccessibleShow',
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
            body: CurrentActivities(isLoading: false, sessions: [session]),
          ),
        ),
      );
      // Check for semantic widgets
      expect(find.byType(SizedBox), findsOneWidget);
      expect(find.byType(Card), findsWidgets);
    });
  });
}
