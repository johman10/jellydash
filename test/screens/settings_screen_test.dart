import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/screens/settings_screen.dart';
import 'package:jellydash/services/app_settings_service.dart';
import 'package:go_router/go_router.dart';

void main() {
  Widget wrapSettings(AppSettings settings) {
    final router = GoRouter(
      routes: [
        GoRoute(
          path: '/',
          builder: (context, state) => SettingsScreen(appSettings: settings),
        ),
      ],
      initialLocation: '/',
    );
    return MaterialApp.router(
      routerDelegate: router.routerDelegate,
      routeInformationParser: router.routeInformationParser,
      routeInformationProvider: router.routeInformationProvider,
    );
  }

  AppSettings baseSettings({
    String jellyfinBaseUrl = 'http://localhost:8096',
    String jellyfinApiKey = '',
    int pollingInterval = 10,
  }) {
    return AppSettings(
      jellyfinBaseUrl: jellyfinBaseUrl,
      jellyfinApiKey: jellyfinApiKey,
      pollingInterval: pollingInterval,
    );
  }

  group('SettingsScreen', () {
    testWidgets('shows API Hostname field', (tester) async {
      final settings = baseSettings(jellyfinBaseUrl: 'http://localhost:8097');

      await tester.pumpWidget(wrapSettings(settings));

      expect(find.widgetWithText(TextFormField, 'API Hostname'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'i.e. http://localhost:8096'),
          findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'http://localhost:8097'),
          findsOneWidget);
    });

    testWidgets('shows API Key field', (tester) async {
      final settings = baseSettings(
        jellyfinBaseUrl: 'http://localhost:8096',
        jellyfinApiKey: '1234567890abcdef1234567890abcdef',
      );

      await tester.pumpWidget(wrapSettings(settings));

      expect(find.widgetWithText(TextFormField, 'API Key'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'Your Jellyfin API Key'),
          findsOneWidget);
      expect(
        find.widgetWithText(
          TextFormField,
          '1234567890abcdef1234567890abcdef',
        ),
        findsOneWidget,
      );
    });

    testWidgets('shows Polling Interval field', (tester) async {
      final settings = baseSettings(pollingInterval: 10);

      await tester.pumpWidget(wrapSettings(settings));

      expect(find.widgetWithText(TextFormField, 'Polling Interval'),
          findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'In seconds'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, '10'), findsOneWidget);
    });

    testWidgets('can fill and save form', (tester) async {
      final settings = baseSettings();

      await tester.pumpWidget(wrapSettings(settings));

      await tester.enterText(
        find.widgetWithText(TextFormField, 'API Hostname'),
        'http://test:8090',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'API Key'),
        '1234567890abcdef1234567890abcdef',
      );
      await tester.enterText(
        find.widgetWithText(TextFormField, 'Polling Interval'),
        '15',
      );

      await tester.pump();
      await tester.tap(find.byIcon(Icons.save));
      await tester.pump();

      expect(find.text('Settings saved'), findsOneWidget);
    });
  });
}
