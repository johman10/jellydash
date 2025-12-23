import 'package:flutter/material.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:go_router/go_router.dart';
import 'screens/dashboard_screen.dart';
import 'screens/settings_screen.dart';
import 'services/app_settings_service.dart';

void main() async {
  await dotenv.load(
    fileName: ".env",
  );


  WidgetsFlutterBinding.ensureInitialized();
  await AppSettingsService().init();
  runApp(const JellydashApp());
}

final GoRouter _router = GoRouter(
  routes: <RouteBase>[
    GoRoute(
      name: 'dashboard',
      path: '/',
      builder: (BuildContext context, GoRouterState state) {
        return const DashboardScreen();
      },
      routes: <RouteBase>[
        GoRoute(
          name: 'settings',
          path: 'settings',
          builder: (BuildContext context, GoRouterState state) {
            return const SettingsScreen();
          },
        ),
      ],
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
