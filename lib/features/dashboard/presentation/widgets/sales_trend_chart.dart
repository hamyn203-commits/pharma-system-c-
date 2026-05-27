import 'package:al_neda_admin_flutter/core/theme/app_theme.dart';
import 'package:al_neda_admin_flutter/features/dashboard/data/dashboard_api.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

class SalesTrendChart extends StatelessWidget {
  const SalesTrendChart({super.key, this.salesData});

  final List<SalesDataPointDto>? salesData;

  static const _defaultSpots = [
    FlSpot(0, 18),
    FlSpot(3, 28),
    FlSpot(6, 24),
    FlSpot(9, 38),
    FlSpot(12, 44),
    FlSpot(15, 40),
    FlSpot(18, 58),
    FlSpot(21, 54),
    FlSpot(24, 72),
    FlSpot(27, 68),
    FlSpot(30, 84),
  ];

  List<FlSpot> get _spots {
    if (salesData == null || salesData!.isEmpty) return _defaultSpots;
    final maxIdx = salesData!.length - 1;
    return salesData!.asMap().entries.map((e) {
      final i = e.key;
      final point = e.value;
      final pct = maxIdx == 0 ? 1.0 : i / maxIdx;
      return FlSpot(pct * 30, point.value);
    }).toList(growable: false);
  }

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

    return Container(
      height: 430,
      padding: const EdgeInsets.all(AppSpacing.xl),
      decoration: BoxDecoration(
        color: colors.surface,
        border: AppBorders.micro(colors),
        borderRadius: AppRadii.panel,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'اتجاه المبيعات خلال 30 يوم',
                      style: textTheme.titleLarge,
                    ),
                    const SizedBox(height: AppSpacing.xs),
                    Text(
                      'تحليل يومي مبسط لقيمة المبيعات من المخزن.',
                      style: textTheme.bodyMedium?.copyWith(
                        color: colors.textMuted,
                      ),
                    ),
                  ],
                ),
              ),
              _ChartLegendDot(color: colors.cyan, label: 'مبيعات'),
            ],
          ),
          const SizedBox(height: AppSpacing.xl),
          Expanded(
            child: LineChart(
              LineChartData(
                minX: 0,
                maxX: 30,
                minY: 0,
                maxY: 100,
                lineTouchData: LineTouchData(
                  handleBuiltInTouches: true,
                  touchTooltipData: LineTouchTooltipData(
                    getTooltipColor: (_) => colors.surfaceElevated,
                    tooltipBorder: BorderSide(color: colors.microBorder),
                    getTooltipItems: (spots) {
                      return spots.map((spot) {
                        return LineTooltipItem(
                          '${spot.y.toStringAsFixed(0)} ألف ج.م',
                          textTheme.labelLarge!.copyWith(
                            color: colors.textPrimary,
                          ),
                        );
                      }).toList();
                    },
                  ),
                ),
                gridData: FlGridData(
                  show: true,
                  drawVerticalLine: false,
                  horizontalInterval: 25,
                  getDrawingHorizontalLine: (_) =>
                      FlLine(color: colors.microBorder, strokeWidth: 1),
                ),
                borderData: FlBorderData(show: false),
                titlesData: FlTitlesData(
                  topTitles: const AxisTitles(
                    sideTitles: SideTitles(showTitles: false),
                  ),
                  rightTitles: const AxisTitles(
                    sideTitles: SideTitles(showTitles: false),
                  ),
                  leftTitles: AxisTitles(
                    sideTitles: SideTitles(
                      showTitles: true,
                      reservedSize: 44,
                      interval: 25,
                      getTitlesWidget: (value, meta) {
                        if (value == 0) return const SizedBox.shrink();
                        return Text(
                          '${value.toInt()}k',
                          style: textTheme.labelSmall?.copyWith(
                            color: colors.textMuted,
                          ),
                        );
                      },
                    ),
                  ),
                  bottomTitles: AxisTitles(
                    sideTitles: SideTitles(
                      showTitles: true,
                      interval: 10,
                      reservedSize: 28,
                      getTitlesWidget: (value, meta) {
                        final day = value.toInt();
                        if (day == 0) return const SizedBox.shrink();
                        return Padding(
                          padding: const EdgeInsets.only(top: AppSpacing.sm),
                          child: Text(
                            'يوم $day',
                            style: textTheme.labelSmall?.copyWith(
                              color: colors.textMuted,
                            ),
                          ),
                        );
                      },
                    ),
                  ),
                ),
                lineBarsData: [
                  LineChartBarData(
                    spots: _spots,
                    isCurved: true,
                    curveSmoothness: 0.35,
                    preventCurveOverShooting: true,
                    barWidth: 3,
                    color: colors.cyan,
                    isStrokeCapRound: true,
                    dotData: FlDotData(
                      show: true,
                      getDotPainter: (spot, percent, barData, index) {
                        final showDot = index == _spots.length - 1;
                        return FlDotCirclePainter(
                          radius: showDot ? 4 : 0,
                          color: colors.cyanBright,
                          strokeWidth: showDot ? 3 : 0,
                          strokeColor: colors.surface,
                        );
                      },
                    ),
                    belowBarData: BarAreaData(
                      show: true,
                      gradient: LinearGradient(
                        begin: Alignment.topCenter,
                        end: Alignment.bottomCenter,
                        colors: [
                          colors.cyan.withValues(alpha: 0.22),
                          colors.cyan.withValues(alpha: 0.04),
                          colors.surface.withValues(alpha: 0),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
              duration: const Duration(milliseconds: 450),
              curve: Curves.easeOutCubic,
            ),
          ),
        ],
      ),
    );
  }
}

class _ChartLegendDot extends StatelessWidget {
  const _ChartLegendDot({required this.color, required this.label});

  final Color color;
  final String label;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).extension<AppThemeColors>()!;
    final textTheme = Theme.of(context).textTheme;

    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.sm,
      ),
      decoration: BoxDecoration(
        color: colors.surfaceElevated,
        border: AppBorders.micro(colors),
        borderRadius: AppRadii.card,
      ),
      child: Row(
        children: [
          Container(
            width: 9,
            height: 9,
            decoration: BoxDecoration(
              color: color,
              borderRadius: BorderRadius.circular(99),
            ),
          ),
          const SizedBox(width: AppSpacing.sm),
          Text(
            label,
            style: textTheme.labelMedium?.copyWith(color: colors.textSecondary),
          ),
        ],
      ),
    );
  }
}
