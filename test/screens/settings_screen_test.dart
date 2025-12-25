import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/screens/settings_screen.dart';

void main() {
  group('SettingsScreen', () {
      testWidgets('shows API Hostname field', (WidgetTester tester) async {
        await tester.pumpWidget(const MaterialApp(home: SettingsScreen()));
        expect(find.widgetWithText(TextFormField, 'API Hostname'), findsOneWidget);
        expect(find.widgetWithText(TextFormField, 'http://localhost:8096'), findsOneWidget);
      });

      testWidgets('shows API Key field', (WidgetTester tester) async {
        await tester.pumpWidget(const MaterialApp(home: SettingsScreen()));
        expect(find.widgetWithText(TextFormField, 'API Key'), findsOneWidget);
        expect(find.widgetWithText(TextFormField, 'Your Jellyfin API Key'), findsOneWidget);
      });

      testWidgets('shows Polling Interval field', (WidgetTester tester) async {
        await tester.pumpWidget(const MaterialApp(home: SettingsScreen()));
        expect(find.widgetWithText(TextFormField, 'Polling Interval'), findsOneWidget);
        expect(find.widgetWithText(TextFormField, 'In seconds'), findsOneWidget);
      });

      testWidgets('can fill and save form', (WidgetTester tester) async {
        await tester.pumpWidget(const MaterialApp(home: SettingsScreen()));
        // Enter values
        await tester.enterText(find.widgetWithText(TextFormField, 'API Hostname'), 'http://test:8096');
        await tester.enterText(find.widgetWithText(TextFormField, 'API Key'), 'testkey');
        await tester.enterText(find.widgetWithText(TextFormField, 'Polling Interval'), '15');
        // Tap save
        await tester.tap(find.widgetWithText(ElevatedButton, 'Save'));
        await tester.pump();
        // Should show a SnackBar
        expect(find.text('Settings saved'), findsOneWidget);
      });
  });
}
