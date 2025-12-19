// ...existing code...
/// AppDrawer provides the navigation/settings drawer for the app.
import 'package:flutter/material.dart';

class AppDrawer extends StatelessWidget {
  const AppDrawer({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Drawer(
      child: ListView(
        padding: EdgeInsets.zero,
        children: <Widget>[
          const DrawerHeader(
            decoration: BoxDecoration(
              color: Colors.deepPurple,
            ),
            child: Text('Jellydash Menu', style: TextStyle(color: Colors.white, fontSize: 24)),
          ),
          const ListTile(
            leading: Icon(Icons.dashboard),
            title: Text('Dashboard'),
          ),
          ListTile(
            leading: const Icon(Icons.settings),
            title: const Text('Settings'),
            onTap: () async {
              Navigator.of(context).pop(); // Close drawer
              await Navigator.of(context).pushNamed('/settings');
            },
          ),
          // Add more navigation items here
        ],
      ),
    );
  }
}
