import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:go_router/go_router.dart';
import 'package:jellydash/screens/dashboard_screen.dart';
import 'package:jellydash/services/api_service_factory.dart';
import 'package:jellydash/theme/jellydash_theme.dart';
import 'screens/settings_screen.dart';
import 'services/app_settings_service.dart';

final appSettingsService = AppSettingsService();

class SettingsHolder extends InheritedWidget {
  final AppSettings settings;
  const SettingsHolder(
      {required this.settings, required super.child, super.key});

  static AppSettings of(BuildContext context) =>
      context.dependOnInheritedWidgetOfExactType<SettingsHolder>()!.settings;

  @override
  bool updateShouldNotify(covariant SettingsHolder oldWidget) =>
      settings != oldWidget.settings;
}

Future<void> main() async {
  GoRouter.optionURLReflectsImperativeAPIs = true;

  WidgetsFlutterBinding.ensureInitialized();

  await dotenv.load(fileName: "assets/env/.env", isOptional: true);
  await appSettingsService.init();
  final settings = await appSettingsService.loadSettings();

  runApp(JellydashApp(settings: settings));
}

// GoRouter is now created inside JellydashApp with settings

class JellydashApp extends StatelessWidget {
  final AppSettings settings;
  const JellydashApp({super.key, required this.settings});

  @override
  Widget build(BuildContext context) {
    final GoRouter router = GoRouter(
      routes: <RouteBase>[
        GoRoute(
          name: 'dashboard',
          path: '/',
          builder: (BuildContext context, GoRouterState state) {
            final appSettings = SettingsHolder.of(context);
            return DashboardScreen(
              apiService: ApiServiceFactory.create(
                baseUrl: appSettings.jellyfinBaseUrl,
                apiKey: appSettings.jellyfinApiKey,
                usePluginApi: appSettings.usePluginApi,
              ),
              pollingInterval: appSettings.pollingInterval,
            );
          },
        ),
        GoRoute(
          name: 'settings',
          path: '/settings',
          builder: (BuildContext context, GoRouterState state) {
            final appSettings = SettingsHolder.of(context);
            return SettingsScreen(appSettings: appSettings);
          },
        ),
      ],
    );
    return SettingsHolder(
      settings: settings,
      child: MaterialApp.router(
        title: 'Jellydash',
        theme: ThemeData(
          colorScheme: ColorScheme.fromSeed(seedColor: JellydashColors.primary),
          useMaterial3: true,
        ),
        darkTheme: ThemeData.dark(),
        routerConfig: router,
      ),
    );
  }
}
