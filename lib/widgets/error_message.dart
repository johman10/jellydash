import 'package:flutter/material.dart';
import 'package:jellydash/services/api_exceptions.dart';

class ErrorMessage extends StatelessWidget {
  final Exception error;
  final VoidCallback? onRetry;
  final VoidCallback? onGoToSettings;
  final bool usePluginApi;

  const ErrorMessage({
    super.key,
    required this.error,
    this.onRetry,
    this.onGoToSettings,
    this.usePluginApi = true,
  });

  @override
  Widget build(BuildContext context) {
    final presentation = presentApiError(error, usePluginApi: usePluginApi);

    return Card(
      margin: const EdgeInsets.symmetric(vertical: 8, horizontal: 0),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          spacing: 8,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              presentation.title,
              style: Theme.of(context).textTheme.titleMedium,
            ),
            ...presentation.details.map((d) => Text(d)),
            if (onRetry != null || onGoToSettings != null)
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                spacing: 8,
                children: [
                  if (onGoToSettings != null)
                    ElevatedButton.icon(
                      icon: const Icon(Icons.settings),
                      label: const Text('Settings'),
                      onPressed: onGoToSettings,
                    ),
                  if (onRetry != null)
                    ElevatedButton.icon(
                      icon: const Icon(Icons.refresh),
                      label: const Text('Retry'),
                      onPressed: onRetry,
                    ),
                ],
              ),
          ],
        ),
      ),
    );
  }
}
