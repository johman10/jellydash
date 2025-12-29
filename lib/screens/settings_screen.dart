import 'package:flutter/material.dart';
import 'package:jellydash/scaffolds/app_scaffold.dart';
import 'package:jellydash/theme/jellydash_theme.dart';
import '../services/app_settings_service.dart';

class SettingsScreen extends StatefulWidget {
  final AppSettings appSettings;

  const SettingsScreen({super.key, required this.appSettings});

  @override
  State<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  late final TextEditingController _pollingIntervalController;
  late final TextEditingController _baseUrlController;
  late final TextEditingController _apiKeyController;
  final _formKey = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    _baseUrlController =
        TextEditingController(text: widget.appSettings.jellyfinBaseUrl);
    _apiKeyController =
        TextEditingController(text: widget.appSettings.jellyfinApiKey);
    _pollingIntervalController = TextEditingController(
        text: widget.appSettings.pollingInterval.toString());
  }

  @override
  void dispose() {
    _baseUrlController.dispose();
    _apiKeyController.dispose();
    _pollingIntervalController.dispose();
    super.dispose();
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
                spacing: 16,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Settings',
                    style: JellydashTextStyles.sectionTitle,
                  ),
                  TextFormField(
                    controller: _baseUrlController,
                    decoration: const InputDecoration(
                      labelText: 'API Hostname',
                      hintText: 'i.e. http://localhost:8096',
                    ),
                    validator: (value) {
                      var parsedUri = Uri.tryParse(_baseUrlController.text);
                      if (value == null || value.isEmpty || (parsedUri != null && !parsedUri.isAbsolute)) {
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
                    validator: (value) {
                      if (value != null && value.isNotEmpty) {
                        if (value.length < 32) {
                          return 'The API key seems too short';
                        } else if (value.length > 32) {
                          return 'The API key seems too long';
                        }
                      }
                      return null;
                    },
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
                      var parsed = int.tryParse(value);
                      if (parsed == null || parsed <= 0) {
                        return 'Please enter a valid positive integer';
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
