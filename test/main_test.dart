import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/services/jellyfin_api_service.dart';
import 'package:jellydash/screens/dashboard_screen.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:flutter/services.dart';

// A mock JellyfinApiService for tests
class MockJellyfinApiService extends JellyfinApiService {
  MockJellyfinApiService() : super(baseUrl: '', apiKey: '');
  @override
  Future<List<dynamic>> fetchCurrentSessions() async => [];
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() async {
    // Mock SharedPreferences to avoid disk IO in tests
    SharedPreferences.setMockInitialValues({});
    // Optionally, mock platform channels if needed
    const MethodChannel('plugins.flutter.io/shared_preferences')
        .setMockMethodCallHandler((call) async {
      return true;
    });
  });

  testWidgets('Dashboard UI renders core elements',
      (WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: DashboardScreen(apiService: MockJellyfinApiService()),
      ),
    );

    // AppBar title
    expect(find.text('Jellydash'), findsOneWidget);

    // Placeholder cards
    expect(find.text('Current Activity'), findsOneWidget);
    expect(find.text('Recent Activity'), findsOneWidget);
    // Only one placeholder message is expected for recent activity
    expect(find.text('No data yet. Connect to Jellyfin API.'), findsOneWidget);
  });

  testWidgets('Drawer opens and closes', (WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: DashboardScreen(apiService: MockJellyfinApiService()),
      ),
    );

    // Drawer should be closed initially
    expect(find.text('Jellydash Menu'), findsNothing);

    // Open the drawer by tapping the menu icon
    final Finder menuButton = find.byTooltip('Open navigation menu');
    expect(menuButton, findsOneWidget);
    await tester.tap(menuButton);
    await tester.pumpAndSettle();

    expect(find.text('Jellydash Menu'), findsOneWidget);

    // Close the drawer
    Navigator.of(tester.element(find.byType(Drawer))).pop();
    await tester.pumpAndSettle();
    expect(find.text('Jellydash Menu'), findsNothing);
  });
}
