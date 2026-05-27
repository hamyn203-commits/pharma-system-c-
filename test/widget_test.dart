import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';

import 'package:al_neda_admin_flutter/core/theme/app_theme.dart';
import 'package:al_neda_admin_flutter/features/dashboard/presentation/pages/dashboard_screen.dart';
import 'package:al_neda_admin_flutter/main.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUpAll(() {
    GoogleFonts.config.allowRuntimeFetching = false;
  });

  testWidgets('renders the Al-Neda login shell', (tester) async {
    tester.view.physicalSize = const Size(1440, 900);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(const AlNedaAdminApp());
    await tester.pump();

    expect(find.text('مخزن الندى'), findsOneWidget);
    expect(find.text('تسجيل الدخول'), findsOneWidget);
    expect(find.text('دخول النظام'), findsOneWidget);
  });

  testWidgets('renders the Al-Neda dashboard shell', (tester) async {
    tester.view.physicalSize = const Size(1440, 900);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.dark,
        home: const Directionality(
          textDirection: TextDirection.rtl,
          child: DashboardScreen(),
        ),
      ),
    );
    await tester.pump();

    expect(find.text('لوحة التحكم'), findsWidgets);
    expect(find.byIcon(Icons.dashboard_rounded), findsOneWidget);
  });
}
