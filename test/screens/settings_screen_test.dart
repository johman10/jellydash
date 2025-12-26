import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/screens/settings_screen.dart';
import 'package:jellydash/services/app_settings_service.dart';

void main() {
  group('SettingsScreen', () {
    testWidgets('shows API Hostname field', (WidgetTester tester) async {
      final dummySettings = AppSettings(
        jellyfinBaseUrl: 'http://localhost:8097',
        jellyfinApiKey: '',
        pollingInterval: 10,
      );
      await tester.pumpWidget(
          MaterialApp(home: SettingsScreen(appSettings: dummySettings)));
      expect(
          find.widgetWithText(TextFormField, 'API Hostname'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'i.e. http://localhost:8096'),
          findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'http://localhost:8097'),
          findsOneWidget);
    });

    testWidgets('shows API Key field', (WidgetTester tester) async {
      final dummySettings = AppSettings(
        jellyfinBaseUrl: 'http://localhost:8096',
        jellyfinApiKey: '1234567890abcdef1234567890abcdef',
        pollingInterval: 10,
      );
      await tester.pumpWidget(
          MaterialApp(home: SettingsScreen(appSettings: dummySettings)));
      expect(find.widgetWithText(TextFormField, 'API Key'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'Your Jellyfin API Key'),
          findsOneWidget);
      expect(
          find.widgetWithText(
              TextFormField, '1234567890abcdef1234567890abcdef'),
          findsOneWidget);
    });

    testWidgets('shows Polling Interval field', (WidgetTester tester) async {
      final dummySettings = AppSettings(
        jellyfinBaseUrl: 'http://localhost:8096',
        jellyfinApiKey: '',
        pollingInterval: 10,
      );
      await tester.pumpWidget(
          MaterialApp(home: SettingsScreen(appSettings: dummySettings)));
      expect(find.widgetWithText(TextFormField, 'Polling Interval'),
          findsOneWidget);
      expect(find.widgetWithText(TextFormField, 'In seconds'), findsOneWidget);
      expect(find.widgetWithText(TextFormField, '10'), findsOneWidget);
    });

    testWidgets('can fill and save form', (WidgetTester tester) async {
      final dummySettings = AppSettings(
        jellyfinBaseUrl: 'http://localhost:8096',
        jellyfinApiKey: '',
        pollingInterval: 10,
      );
      await tester.pumpWidget(
          MaterialApp(home: SettingsScreen(appSettings: dummySettings)));
      // Enter values
      await tester.enterText(find.widgetWithText(TextFormField, 'API Hostname'),
          'http://test:8090');
      await tester.enterText(find.widgetWithText(TextFormField, 'API Key'),
          '1234567890abcdef1234567890abcdef');
      await tester.enterText(
          find.widgetWithText(TextFormField, 'Polling Interval'), '15');
      // Tap save
      await tester.pump();
      await tester.tap(find.byIcon(Icons.save));
      await tester.pump();
      // Should show a SnackBar
      expect(find.text('Settings saved'), findsOneWidget);
    });
  });
}
