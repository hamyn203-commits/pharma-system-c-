import 'package:al_neda_admin_flutter/core/theme/app_theme.dart';
import 'package:flutter/material.dart';

@immutable
class StatCardData {
  const StatCardData({
    required this.title,
    required this.value,
    required this.suffix,
    required this.icon,
    required this.accentColor,
  });

  final String title;
  final String value;
  final String suffix;
  final IconData icon;
  final Color accentColor;
}

class StatCard extends StatefulWidget {
  const StatCard({required this.data, super.key, this.onTap});

  final StatCardData data;
  final VoidCallback? onTap;

  @override
  State<StatCard> createState() => _StatCardState();
}

class _StatCardState extends State<StatCard> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

    return MouseRegion(
      onEnter: (_) => setState(() => _hovered = true),
      onExit: (_) => setState(() => _hovered = false),
      child: GestureDetector(
        onTap: widget.onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 160),
          curve: Curves.easeOutCubic,
          padding: const EdgeInsets.all(AppSpacing.xl),
          decoration: BoxDecoration(
            color: _hovered ? colors.surfaceHover : colors.surface,
            border: Border.all(
              color: _hovered
                  ? widget.data.accentColor.withValues(alpha: 0.28)
                  : colors.microBorder,
            ),
            borderRadius: AppRadii.panel,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    width: 44,
                    height: 44,
                    decoration: BoxDecoration(
                      color: widget.data.accentColor.withValues(alpha: 0.15),
                      border: Border.all(
                        color: widget.data.accentColor.withValues(alpha: 0.22),
                      ),
                      borderRadius: AppRadii.card,
                    ),
                    child: Icon(
                      widget.data.icon,
                      color: widget.data.accentColor,
                      size: 23,
                    ),
                  ),
                  const Spacer(),
                  Container(
                    width: 38,
                    height: 3,
                    decoration: BoxDecoration(
                      color: widget.data.accentColor,
                      borderRadius: BorderRadius.circular(99),
                    ),
                  ),
                ],
              ),
              const Spacer(),
              Text(
                widget.data.title,
                style: textTheme.labelLarge?.copyWith(color: colors.textMuted),
              ),
              const SizedBox(height: AppSpacing.sm),
              Row(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(
                    widget.data.value,
                    style: textTheme.headlineMedium?.copyWith(
                      color: colors.textPrimary,
                      fontWeight: FontWeight.w900,
                      height: 1,
                    ),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Padding(
                    padding: const EdgeInsets.only(bottom: 2),
                    child: Text(
                      widget.data.suffix,
                      style: textTheme.labelMedium?.copyWith(
                        color: widget.data.accentColor,
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
