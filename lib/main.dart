import 'dart:async';
import 'dart:io';
import 'dart:ui';

import 'package:al_neda_admin_flutter/core/theme/app_theme.dart';
import 'package:al_neda_admin_flutter/features/auth/presentation/pages/login_screen.dart';
import 'package:al_neda_admin_flutter/features/dashboard/presentation/pages/dashboard_screen.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:window_manager/window_manager.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  Bloc.observer = const AlNedaBlocObserver();

  // Cairo is loaded by google_fonts during development. For locked-down
  // production environments, bundle the font under assets/fonts and disable
  // runtime fetching in release builds.
  GoogleFonts.config.allowRuntimeFetching = !kReleaseMode;

  await initializeDateFormatting('ar_EG');
  await SystemChrome.setEnabledSystemUIMode(SystemUiMode.edgeToEdge);
  await _configureDesktopWindow();

  runApp(const AlNedaAdminApp());
}

Future<void> _configureDesktopWindow() async {
  if (kIsWeb || !(Platform.isWindows || Platform.isLinux || Platform.isMacOS)) {
    return;
  }

  await windowManager.ensureInitialized();

  const windowOptions = WindowOptions(
    size: Size(1440, 920),
    minimumSize: Size(1180, 760),
    center: true,
    title: 'Al-Neda Pharmacy Warehouse',
    backgroundColor: Color(0xFF0F172A),
    titleBarStyle: TitleBarStyle.normal,
  );

  unawaited(
    windowManager.waitUntilReadyToShow(windowOptions, () async {
      await windowManager.show();
      await windowManager.focus();
    }),
  );
}

final _router = GoRouter(
  initialLocation: '/login',
  debugLogDiagnostics: !kReleaseMode,
  routes: [
    GoRoute(
      path: '/login',
      name: 'login',
      builder: (context, state) => const LoginScreen(),
    ),
    ShellRoute(
      builder: (context, state, child) => child,
      routes: [
        GoRoute(
          path: '/dashboard',
          name: 'dashboard',
          builder: (context, state) => const DashboardScreen(),
          routes: [
            GoRoute(
              path: 'products',
              name: 'products',
              builder: (context, state) => const DashboardScreen(),
            ),
            GoRoute(
              path: 'pharmacies',
              name: 'pharmacies',
              builder: (context, state) => const DashboardScreen(),
            ),
            GoRoute(
              path: 'orders',
              name: 'orders',
              builder: (context, state) => const DashboardScreen(),
            ),
            GoRoute(
              path: 'reports',
              name: 'reports',
              builder: (context, state) => const DashboardScreen(),
            ),
            GoRoute(
              path: 'settings',
              name: 'settings',
              builder: (context, state) => const DashboardScreen(),
            ),
          ],
        ),
      ],
    ),
  ],
);

class AlNedaAdminApp extends StatelessWidget {
  const AlNedaAdminApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'Al-Neda Pharmacy Warehouse',
      debugShowCheckedModeBanner: false,
      routerConfig: _router,
      theme: AppTheme.dark,
      themeMode: ThemeMode.dark,
      locale: const Locale('ar', 'EG'),
      supportedLocales: const [Locale('ar', 'EG'), Locale('en', 'US')],
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      scrollBehavior: const EnterpriseDesktopScrollBehavior(),
      builder: (context, child) {
        return Directionality(
          textDirection: TextDirection.rtl,
          child: child ?? const SizedBox.shrink(),
        );
      },
    );
  }
}

/// Desktop-friendly scrolling for mouse, touchpad, stylus, and touch screens.
class EnterpriseDesktopScrollBehavior extends MaterialScrollBehavior {
  const EnterpriseDesktopScrollBehavior();

  @override
  Set<PointerDeviceKind> get dragDevices => const {
    PointerDeviceKind.touch,
    PointerDeviceKind.mouse,
    PointerDeviceKind.trackpad,
    PointerDeviceKind.stylus,
  };
}

/// Central Bloc observer. Keep it lightweight; structured logging can be wired
/// through `logger` once core diagnostics are added.
class AlNedaBlocObserver extends BlocObserver {
  const AlNedaBlocObserver();

  @override
  void onError(BlocBase<dynamic> bloc, Object error, StackTrace stackTrace) {
    FlutterError.reportError(
      FlutterErrorDetails(
        exception: error,
        stack: stackTrace,
        library: 'flutter_bloc',
        context: ErrorDescription('while processing ${bloc.runtimeType}'),
      ),
    );
    super.onError(bloc, error, stackTrace);
  }
}
