/// Build-time configuration, one file per environment:
/// flutter build appbundle --dart-define-from-file=config/prod.json
/// These values end up inside the app binary: never put a secret here.
const apiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'http://127.0.0.1:5080',
);

/// dev, staging or prod; anything but prod shows a corner banner.
const appEnvironment = String.fromEnvironment('APP_ENV', defaultValue: 'dev');
