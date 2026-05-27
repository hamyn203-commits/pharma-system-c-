abstract final class AppConfig {
  static const dotnetApiBaseUrl = String.fromEnvironment(
    'ALNEDA_API_BASE_URL',
    defaultValue: 'http://localhost:5000',
  );

  static const analyticsApiBaseUrl = String.fromEnvironment(
    'ALNEDA_ANALYTICS_API_BASE_URL',
    defaultValue: 'http://localhost:8010',
  );

  static const authTokenStorageKey = 'alneda.jwt';
}
