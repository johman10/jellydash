import 'package:flutter/material.dart';
import 'package:jellydash/services/api_exceptions.dart';

class ErrorCard extends StatelessWidget {
  final Exception error;

  const ErrorCard({super.key, required this.error});

  List<Widget> get children {
    if (error.runtimeType == NotFoundException) {
      final notFoundException = error as NotFoundException;
      if (notFoundException.service == NotFoundService.jellydash) {
        return [
          const Text(
            'Error: Jellydash plugin not found (404). Please ensure the Jellydash plugin is installed on your Jellyfin server.',
          ),
          const Text(
            'You can download the Jellydash plugin from: https://github.com/johman10/jellydash',
          ),
          const Text(
            'After installing the plugin, restart your Jellyfin server and try again.',
          ),
          const Text(
            'If you don\'t want to install the plugin, you can disable it through the settings in Jellydash.',
          )
        ];
      }

      if (notFoundException.service == NotFoundService.jellyfin) {
        return [
          const Text(
            'Error: Endpoint not found (404). Please check your Jellyefin server base URL and reverse proxy settings if applicable.',
          ),
        ];
      }
    }

    if (error.runtimeType == UnauthorizedException) {
      return [
        const Text(
          'Error: Unauthorized (401). Please check your API key.',
        ),
      ];
    }

    if (error.runtimeType == NetworkException) {
      final networkException = error as NetworkException;
      if (networkException.kind == NetworkFailureKind.timeout) {
        return [
          const Text(
            'Error: Network timeout. Please check your server status and network connection.',
          ),
        ];
      }

      if (networkException.kind == NetworkFailureKind.connection) {
        return [
          const Text(
            'Error: Could not connect to the server. Please check your server status and network connection.',
          ),
        ];
      }
    }

    return [
      const Text(
        'An unexpected error occurred.',
      ),
    ];
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(vertical: 8, horizontal: 0),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          spacing: 8,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: children,
        ),
      ),
    );
  }
}
