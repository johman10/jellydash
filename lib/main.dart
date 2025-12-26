import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:go_router/go_router.dart';
import 'package:jellydash/screens/dashboard_screen.dart';
import 'package:jellydash/services/jellyfin_api_service.dart';
import 'screens/settings_screen.dart';
import 'services/app_settings_service.dart';

final appSettingsService = AppSettingsService();

void main() async {
  await dotenv.load(
    fileName: ".env",
  );

  WidgetsFlutterBinding.ensureInitialized();
  await appSettingsService.init();
  runApp(const JellydashApp());
}

final GoRouter _router = GoRouter(
  routes: <RouteBase>[
    GoRoute(
      name: 'dashboard',
      path: '/',
      builder: (BuildContext context, GoRouterState state) {
        return FutureBuilder<AppSettings>(
          future: appSettingsService.loadSettings(),
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const Center(child: CircularProgressIndicator());
            } else if (snapshot.hasError) {
              return Center(child: Text('Error: ${snapshot.error}'));
            } else if (snapshot.hasData) {
              final appSettings = snapshot.data!;
              return DashboardScreen(
                apiService: JellyfinApiService(
                  baseUrl: appSettings.jellyfinBaseUrl,
                  apiKey: appSettings.jellyfinApiKey,
                ),
                pollingInterval: appSettings.pollingInterval,
              );
            } else {
              return const Center(child: Text('No settings found.'));
            }
          },
        );
      },
    ),
    GoRoute(
      name: 'settings',
      path: '/settings',
      builder: (BuildContext context, GoRouterState state) {
        return FutureBuilder<AppSettings>(
          future: appSettingsService.loadSettings(),
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const Center(child: CircularProgressIndicator());
            } else if (snapshot.hasError) {
              return Center(child: Text('Error: ${snapshot.error}'));
            } else if (snapshot.hasData) {
              final appSettings = snapshot.data!;
              return SettingsScreen(
                appSettings: appSettings,
              );
            } else {
              return const Center(child: Text('No settings found.'));
            }
          },
        );
      },
    ),
  ],
);

class JellydashApp extends StatelessWidget {
  const JellydashApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'Jellydash',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.deepPurple),
        useMaterial3: true,
      ),
      darkTheme: ThemeData.dark(),
      routerConfig: _router,
    );
  }
}
