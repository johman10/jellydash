import 'package:flutter/material.dart';
import 'package:jellydash/theme/jellydash_theme.dart';

const posterHeight = 75.0;
const posterWidth = 50.0;

class EntityPoster extends StatelessWidget {
  final String imageUrl;
  final bool isPaused;

  const EntityPoster({
    super.key,
    required this.imageUrl,
    required this.isPaused,
  });

  Widget get posterFallback {
    return Container(
      width: posterWidth,
      height: posterHeight,
      color: JellydashColors.posterFallbackBackground,
      child: const Icon(
        Icons.movie,
        size: 48,
        color: JellydashColors.posterFallbackIcon,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(4),
      child: Stack(
        children: [
          imageUrl.isNotEmpty
              ? Image.network(
                  imageUrl,
                  width: posterWidth,
                  height: posterHeight,
                  fit: BoxFit.cover,
                  loadingBuilder: (context, child, loadingProgress) {
                    if (loadingProgress == null) return child;
                    return posterFallback;
                  },
                  errorBuilder: (context, error, stackTrace) => posterFallback,
                )
              : posterFallback,
          if (isPaused)
            Container(
              width: posterWidth,
              height: posterHeight,
              color: JellydashColors.pausedOverlayBackground,
              alignment: Alignment.center,
              child: Semantics(
                label: 'Playback paused',
                child: const Icon(
                  Icons.pause_circle_filled,
                  color: JellydashColors.pausedOverlayIcon,
                  size: 32,
                ),
              ),
            ),
        ],
      ),
    );
  }
}
