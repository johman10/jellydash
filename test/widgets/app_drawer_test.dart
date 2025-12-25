import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/widgets/app_drawer.dart';

void main() {
  group('AppDrawer', () {
    testWidgets('renders without error', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            drawer: AppDrawer(),
          ),
        ),
      );
      // Open the drawer
      ScaffoldState state = tester.firstState(find.byType(Scaffold));
      state.openDrawer();
      await tester.pumpAndSettle();
      expect(find.byType(AppDrawer), findsOneWidget);
    });

    testWidgets('contains navigation items', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            drawer: AppDrawer(),
          ),
        ),
      );
      ScaffoldState state = tester.firstState(find.byType(Scaffold));
      state.openDrawer();
      await tester.pumpAndSettle();
      // Check for common navigation items
      expect(find.textContaining('Dashboard', findRichText: true), findsWidgets);
      expect(find.textContaining('Settings', findRichText: true), findsWidgets);
    });
  });
}
