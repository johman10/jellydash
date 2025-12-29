import 'package:flutter/material.dart';
import 'package:jellydash/theme/jellydash_theme.dart';

class RecentActivityCard extends StatelessWidget {
  const RecentActivityCard({super.key});

  @override
  Widget build(BuildContext context) {
    return const SizedBox(
      width: double.infinity,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Recent Activities',
            style: JellydashTextStyles.sectionTitle,
          ),
        ],
      ),
    );
  }
}
