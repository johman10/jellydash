import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:jellydash/theme/jellydash_theme.dart';

class AppDrawer extends StatelessWidget {
  const AppDrawer({super.key});

  void _navigateIfNotCurrent(BuildContext context, String routeName) {
    final String currentLocation = GoRouterState.of(context).uri.toString();

    if (Scaffold.of(context).isDrawerOpen) {
      Navigator.pop(context);
    }
    if (currentLocation != routeName) {
      context.go(routeName);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Drawer(
      child: ListView(
        padding: EdgeInsets.zero,
        children: <Widget>[
          const DrawerHeader(
            decoration: BoxDecoration(
              color: JellydashColors.drawerHeaderBackground,
            ),
            child: Text(
              'Jellydash Menu',
              style: JellydashTextStyles.appDrawerHeader,
            ),
          ),
          ListTile(
            leading: const Icon(Icons.dashboard),
            title: const Text('Dashboard'),
            onTap: () {
              _navigateIfNotCurrent(context, '/');
            },
          ),
          ListTile(
            leading: const Icon(Icons.settings),
            title: const Text('Settings'),
            onTap: () {
              _navigateIfNotCurrent(context, '/settings');
            },
          ),
        ],
      ),
    );
  }
}
