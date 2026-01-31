import 'package:flutter/material.dart';
import 'package:jellydash/theme/jellydash_theme.dart';

class TranscodingBadges extends StatelessWidget {
  final bool isAudioDirectStream;
  final bool isVideoDirectStream;

  const TranscodingBadges({
    super.key,
    required this.isAudioDirectStream,
    required this.isVideoDirectStream,
  });

  @override
  Widget build(BuildContext context) {
    if (isAudioDirectStream && isVideoDirectStream) {
      return const SizedBox.shrink();
    }

    return Column(
      spacing: 2,
      children: [
        !isVideoDirectStream
            ? _buildBadge(context, 'V', 'Video transcoding')
            : const SizedBox.shrink(),
        !isAudioDirectStream
            ? _buildBadge(context, 'A', 'Audio transcoding')
            : const SizedBox.shrink(),
      ],
    );
  }

  Widget _buildBadge(
    BuildContext context,
    String label,
    String semanticsLabel,
  ) {
    return Container(
      height: 20,
      width: 20,
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(4),
        color: JellydashColors.transcodingBadgeBackground(context),
      ),
      alignment: Alignment.center,
      child: Semantics(
        label: semanticsLabel,
        child: Text(label),
      ),
    );
  }
}
