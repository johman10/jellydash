import 'package:flutter/material.dart';
// settings_dialog.dart is now obsolete; content moved here.
import '../services/app_settings_service.dart';

/// Screen for Jellyfin settings, previously shown as a dialog.
class SettingsScreen extends StatelessWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    // Load current settings from AppSettingsService
    return FutureBuilder<Map<String, dynamic>>(
      future: _loadSettings(),
      builder: (context, snapshot) {
        if (!snapshot.hasData) {
          return Scaffold(
            appBar: AppBar(title: const Text('Jellyfin Settings')),
            body: const Center(child: CircularProgressIndicator()),
          );
        }
        final baseUrlController = TextEditingController(text: snapshot.data!['baseUrl']);
        final apiKeyController = TextEditingController(text: snapshot.data!['apiKey']);
        int pollingInterval = snapshot.data!['pollingInterval'] ?? 10;
      return Scaffold(
        appBar: AppBar(title: const Text('Jellyfin Settings')),
        body: Align(
          alignment: Alignment.topCenter,
          child: Card(
            margin: const EdgeInsets.all(16.0),
            elevation: 8,
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  TextField(
                    controller: baseUrlController,
                    decoration: const InputDecoration(
                      labelText: 'API Hostname',
                      hintText: 'http://localhost:8096',
                    ),
                  ),
                  TextField(
                    controller: apiKeyController,
                    decoration: const InputDecoration(
                      labelText: 'API Key',
                      hintText: 'Your Jellyfin API Key',
                    ),
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      const Text('Polling interval (sec):'),
                      const SizedBox(width: 8),
                      Expanded(
                        child: TextFormField(
                          initialValue: pollingInterval.toString(),
                          keyboardType: TextInputType.number,
                          decoration: const InputDecoration(
                            border: OutlineInputBorder(),
                          ),
                          onChanged: (value) async {
                            final int? newInterval = int.tryParse(value);
                            if (newInterval != null && newInterval > 0) {
                              await AppSettingsService().savePollingInterval(newInterval);
                              pollingInterval = newInterval;
                            }
                          },
                        ),
                      ),
                    ],
                  ),
                      const SizedBox(height: 24),
                      Align(
                        alignment: Alignment.centerRight,
                        child: ElevatedButton.icon(
                          icon: const Icon(Icons.save),
                          label: const Text('Save'),
                          onPressed: () async {
                            final navigator = Navigator.of(context);
                            await _saveSettings(baseUrlController.text, apiKeyController.text, pollingInterval);
                            // Notify dashboard to reload settings and polling
                            if (navigator.canPop()) {
                              navigator.pop(true); // Return true to indicate settings changed
                            }
                          },
                        ),
                      ),
                ],
              ),
            ),
          ),
        ),
      );
      },
    );
  }

  Future<Map<String, dynamic>> _loadSettings() async {
    final service = AppSettingsService();
    final baseUrl = await service.loadJellyfinBaseUrl();
    final apiKey = await service.loadJellyfinApiKey();
    final pollingInterval = await service.loadPollingInterval();
    return {
      'baseUrl': baseUrl,
      'apiKey': apiKey,
      'pollingInterval': pollingInterval,
    };
  }

  Future<void> _saveSettings(String baseUrl, String apiKey, int pollingInterval) async {
    final service = AppSettingsService();
    await service.saveJellyfinBaseUrl(baseUrl);
    await service.saveJellyfinApiKey(apiKey);
    await service.savePollingInterval(pollingInterval);
  }
}
