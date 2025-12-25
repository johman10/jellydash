import 'package:flutter/material.dart';
import 'package:jellydash/scaffolds/app_scaffold.dart';
import '../services/app_settings_service.dart';

class SettingsScreen extends StatefulWidget {
  const SettingsScreen({super.key});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  TextEditingController _pollingIntervalController =
      TextEditingController(text: '10');
  TextEditingController _baseUrlController = TextEditingController(text: '');
  TextEditingController _apiKeyController = TextEditingController(text: '');
  final _formKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    _loadSettings();
  }

  Future<void> _loadSettings() async {
    final service = AppSettingsService();
    final baseUrl = await service.loadJellyfinBaseUrl();
    final apiKey = await service.loadJellyfinApiKey();
    final pollingInterval = await service.loadPollingInterval();

    setState(() {
      _pollingIntervalController =
          TextEditingController(text: pollingInterval.toString());
      _baseUrlController = TextEditingController(text: baseUrl);
      _apiKeyController = TextEditingController(text: apiKey);
    });
  }

  Future<void> _saveSettings(
      String baseUrl, String apiKey, int pollingInterval) async {
    final service = AppSettingsService();
    await service.saveJellyfinBaseUrl(baseUrl);
    await service.saveJellyfinApiKey(apiKey);
    await service.savePollingInterval(pollingInterval);
  }

  handleSavePressed() async {
    var scaffoldMessenger = ScaffoldMessenger.of(context);
    if (_formKey.currentState!.validate()) {
      await _saveSettings(_baseUrlController.text, _apiKeyController.text,
          int.parse(_pollingIntervalController.text));

      scaffoldMessenger.showSnackBar(
        const SnackBar(content: Text('Settings saved')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      children: [
        Form(
          key: _formKey,
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(24.0),
              child: Column(
                // spacing: 16,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('Settings',
                      style:
                          TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  TextFormField(
                    controller: _baseUrlController,
                    decoration: const InputDecoration(
                      labelText: 'API Hostname',
                      hintText: 'http://localhost:8096',
                    ),
                    validator: (value) {
                      if (value == null || value.isEmpty) {
                        return 'Please enter the Jellyfin hostname';
                      }
                      return null;
                    },
                  ),
                  TextFormField(
                    controller: _apiKeyController,
                    decoration: const InputDecoration(
                      labelText: 'API Key',
                      hintText: 'Your Jellyfin API Key',
                    ),
                  ),
                  TextFormField(
                    controller: _pollingIntervalController,
                    decoration: const InputDecoration(
                        labelText: 'Polling Interval', hintText: 'In seconds'),
                    keyboardType: TextInputType.number,
                    validator: (value) {
                      if (value == null || value.isEmpty) {
                        return 'Please enter a polling interval';
                      }
                      return null;
                    },
                  ),
                  Align(
                    alignment: Alignment.centerRight,
                    child: ElevatedButton.icon(
                      icon: const Icon(Icons.save),
                      label: const Text('Save'),
                      onPressed: handleSavePressed,
                    ),
                  ),
                ],
              ),
            ),
          ),
        )
      ],
    );
  }
}
