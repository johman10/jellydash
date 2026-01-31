import 'package:flutter/material.dart';
import 'package:jellydash/theme/jellydash_theme.dart';

class PlaybackProgress extends StatelessWidget {
  final Duration progress;
  final Duration duration;
  final double transcodingProgressPercent;

  const PlaybackProgress({
    super.key,
    required this.progress,
    required this.duration,
    required this.transcodingProgressPercent,
  });

  @override
  Widget build(BuildContext context) {
    final totalMs = duration.inMilliseconds;
    final progressMs = progress.inMilliseconds;
    final sliderValue = (totalMs <= 0 || progressMs <= 0)
        ? 0.0
        : (progressMs / totalMs).clamp(0.0, 1.0);

    final progressPercent = (sliderValue * 100).round().clamp(0, 100);

    return Row(children: [
      Expanded(
        child: Semantics(
          label: 'Playback progress',
          value: '$progressPercent%',
          readOnly: true,
          child: IgnorePointer(
            child: SliderTheme(
              data: SliderTheme.of(context).copyWith(
                trackHeight: 4,
                thumbShape: SliderComponentShape.noThumb,
                overlayShape: SliderComponentShape.noOverlay,
              ),
              child: Slider(
                padding: EdgeInsets.zero,
                mouseCursor: SystemMouseCursors.basic,
                value: sliderValue,
                secondaryTrackValue:
                    (transcodingProgressPercent / 100.0).clamp(0.0, 1.0),
                onChanged: (double _) {},
                activeColor: JellydashColors.sliderActive,
                secondaryActiveColor: JellydashColors.sliderSecondaryActive,
              ),
            ),
          ),
        ),
      ),
    ]);
  }
}
