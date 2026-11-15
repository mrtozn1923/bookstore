/// The field errors of an ASP.NET Core ValidationProblemDetails response:
/// {"title": "...", "status": 400, "errors": {"PostalCode": ["..."]}}
class ValidationProblem {
  const ValidationProblem({
    required this.title,
    required this.errors,
    this.detail,
  });

  final String title;
  final String? detail;
  final Map<String, List<String>> errors;

  factory ValidationProblem.fromJson(Map<String, Object?> json) {
    final errors = <String, List<String>>{};
    if (json case {'errors': Map<String, Object?> raw}) {
      for (final MapEntry(:key, :value) in raw.entries) {
        if (value case List<Object?> messages) {
          errors[key] = [for (final m in messages) '$m'];
        }
      }
    }
    return ValidationProblem(
      title: json['title'] as String? ?? 'The request is not valid.',
      detail: json['detail'] as String?,
      errors: errors,
    );
  }

  /// The first message for each field whose key starts with [prefix],
  /// with the prefix removed ("ShippingAddress.City" becomes "City").
  Map<String, String> fieldErrors({String prefix = ''}) => {
    for (final MapEntry(:key, :value) in errors.entries)
      if (key.startsWith(prefix) && value.isNotEmpty)
        key.substring(prefix.length): value.first,
  };
}
