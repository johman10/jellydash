import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class AppScaffold extends StatelessWidget {
  final List<Widget> children;
  final String title;
  final List<Widget>? actions;
  final Widget? leading;
  final Future<void> Function()? onRefresh;
  const AppScaffold({
    super.key,
    required this.children,
    required this.title,
    this.actions,
    this.leading,
    this.onRefresh,
  });

  static const double maxContentWidth = 1100;

  Widget getScrollableContent(BoxConstraints constraints) {
    return SingleChildScrollView(
      physics: const AlwaysScrollableScrollPhysics(),
      child: Center(
        child: ConstrainedBox(
          constraints: BoxConstraints(
              maxWidth: maxContentWidth,
              minHeight: constraints.maxHeight - kToolbarHeight),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: children,
            ),
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final canPop = GoRouter.of(context).canPop();

    return Scaffold(
      appBar: AppBar(
        leading: canPop
            ? IconButton(
                icon: const Icon(Icons.arrow_back),
                tooltip: 'Back',
                onPressed: () {
                  GoRouter.of(context).pop();
                },
              )
            : leading,
        title: Text(title),
        actions: actions,
      ),
      extendBody: true,
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) {
            if (onRefresh != null) {
              return RefreshIndicator(
                onRefresh: onRefresh!,
                child: getScrollableContent(constraints),
              );
            }
            return getScrollableContent(constraints);
          },
        ),
      ),
    );
  }
}
