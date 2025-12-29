import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/types/session.dart';
import 'package:jellydash/widgets/current_activities.dart';

void main() {
  group('CurrentActivities layout', () {
    Widget wrapWithMaterial(Widget child) {
      return MaterialApp(
        home: Scaffold(
          body: Center(child: child),
        ),
      );
    }

    Session buildSession(String userName) {
      return Session(
        userName: userName,
        client: 'Web',
        deviceName: 'Device',
        name: 'Show',
        season: 1,
        episode: 1,
        video: SessionVideo(),
        audio: SessionAudio(),
        subtitles: SessionSubtitle(),
        transcodingInfo:
            TranscodingInfo(video: SessionVideo(), audio: SessionAudio(), reasons: []),
        progress: const Duration(minutes: 1),
        duration: const Duration(minutes: 10),
      );
    }

    testWidgets('shows message when no sessions', (WidgetTester tester) async {
      await tester.pumpWidget(wrapWithMaterial(
        const CurrentActivities(isLoading: false, sessions: []),
      ));

      expect(find.text('No current activities.'), findsOneWidget);
    });

    testWidgets('renders one column on narrow width', (WidgetTester tester) async {
      final sessions = List.generate(3, (i) => buildSession('User$i'));

      await tester.binding.setSurfaceSize(const Size(320, 640));
      await tester.pumpWidget(wrapWithMaterial(
        CurrentActivities(isLoading: false, sessions: sessions),
      ));

      // All cards should be stacked vertically; we just assert they all exist.
      for (var i = 0; i < sessions.length; i++) {
        expect(find.textContaining('User$i'), findsOneWidget);
      }
    });

    testWidgets('renders multiple sessions without overflow', (WidgetTester tester) async {
      final sessions = List.generate(6, (i) => buildSession('User$i'));

      await tester.binding.setSurfaceSize(const Size(1200, 800));
      await tester.pumpWidget(wrapWithMaterial(
        CurrentActivities(isLoading: false, sessions: sessions),
      ));

      for (var i = 0; i < sessions.length; i++) {
        expect(find.textContaining('User$i'), findsOneWidget);
      }
    });
  });
}
