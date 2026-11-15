import 'package:flutter/services.dart';
import 'package:material_ui/material_ui.dart';

import '../../../data/validation_problem.dart';
import '../../../domain/models/delivery_address.dart';
import '../../core/validators.dart';

/// Returns null on success, or the ValidationProblemDetails JSON of the API.
typedef AddressHandler = Future<Map<String, Object?>?> Function(
  DeliveryAddress address,
);

/// API property names; the keys of ValidationProblemDetails.errors.
enum AddressField { fullName, addressLine, city, postalCode, phone }

extension on AddressField {
  String get apiName => name[0].toUpperCase() + name.substring(1);
}

class DeliveryAddressScreen extends StatefulWidget {
  const DeliveryAddressScreen({super.key, required this.onSubmit});

  final AddressHandler onSubmit;

  @override
  State<DeliveryAddressScreen> createState() => _DeliveryAddressScreenState();
}

class _DeliveryAddressScreenState extends State<DeliveryAddressScreen> {
  final _formKey = GlobalKey<FormState>();
  final _controllers = {
    for (final field in AddressField.values) field: TextEditingController(),
  };
  final _focusNodes = {
    for (final field in AddressField.values) field: FocusNode(),
  };
  final _fieldKeys = {
    for (final field in AddressField.values)
      field: GlobalKey<FormFieldState<String>>(),
  };
  var _dirty = false;
  var _serverErrors = <String, String>{};
  String? _formError;
  var _submitting = false;

  @override
  void initState() {
    super.initState();
    for (final controller in _controllers.values) {
      controller.addListener(_updateDirty);
    }
  }

  void _updateDirty() {
    final dirty = _controllers.values.any((c) => c.text.isNotEmpty);
    if (dirty != _dirty) setState(() => _dirty = dirty);
  }

  Future<bool> _confirmDiscard() async {
    final discard = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Discard this address?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Keep editing'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Discard'),
          ),
        ],
      ),
    );
    return discard ?? false;
  }

  @override
  void dispose() {
    for (final controller in _controllers.values) {
      controller.dispose();
    }
    for (final node in _focusNodes.values) {
      node.dispose();
    }
    super.dispose();
  }

  void _focusFirstInvalid() {
    for (final field in AddressField.values) {
      if (_fieldKeys[field]!.currentState?.hasError ?? false) {
        _focusNodes[field]!.requestFocus();
        return;
      }
    }
  }

  Future<void> _submit() async {
    setState(() {
      _serverErrors = {};
      _formError = null;
    });
    if (!_formKey.currentState!.validate()) {
      _focusFirstInvalid();
      return;
    }
    setState(() => _submitting = true);
    String text(AddressField f) => _controllers[f]!.text.trim();
    final problem = await widget.onSubmit(
      DeliveryAddress(
        fullName: text(.fullName),
        addressLine: text(.addressLine),
        city: text(.city),
        postalCode: text(.postalCode),
        phone: text(.phone),
      ),
    );
    if (!mounted) return;
    setState(() => _submitting = false);
    if (problem == null) return;

    final validation = ValidationProblem.fromJson(problem);
    final errors = validation.fieldErrors(prefix: 'ShippingAddress.');
    final known = {for (final f in AddressField.values) f.apiName};
    // An error with no field on this form still has to reach the user
    final unmapped = [
      for (final MapEntry(:key, :value) in validation.errors.entries)
        if (!known.contains(key.replaceFirst('ShippingAddress.', ''))) ...value,
    ];
    setState(() {
      _serverErrors = {
        for (final MapEntry(:key, :value) in errors.entries)
          if (known.contains(key)) key: value,
      };
      _formError = unmapped.isEmpty ? null : unmapped.join('\n');
    });
    WidgetsBinding.instance.addPostFrameCallback((_) => _focusFirstInvalid());
  }

  Widget _field(
    AddressField field,
    String label,
    FormFieldValidator<String> validator, {
    TextInputType? keyboardType,
    List<TextInputFormatter>? inputFormatters,
    Iterable<String>? autofillHints,
    TextInputAction action = TextInputAction.next,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: TextFormField(
        key: _fieldKeys[field],
        controller: _controllers[field],
        focusNode: _focusNodes[field],
        decoration: InputDecoration(labelText: label),
        keyboardType: keyboardType,
        inputFormatters: inputFormatters,
        autofillHints: autofillHints,
        textInputAction: action,
        validator: validator,
        // The server's message wins until the user edits the field
        forceErrorText: _serverErrors[field.apiName],
        onChanged: (_) {
          if (_serverErrors.containsKey(field.apiName)) {
            setState(() => _serverErrors.remove(field.apiName));
          }
        },
        onFieldSubmitted: action == TextInputAction.done
            ? (_) => _submit()
            : null,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    // Back button and back gesture ask first when the user has typed something
    return PopScope<Object?>(
      canPop: !_dirty,
      onPopInvokedWithResult: (didPop, result) async {
        if (didPop) return;
        if (await _confirmDiscard() && context.mounted) {
          Navigator.of(context).pop();
        }
      },
      child: Scaffold(
        appBar: AppBar(title: const Text('Delivery address')),
        body: SafeArea(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Form(
              key: _formKey,
              autovalidateMode: AutovalidateMode.onUserInteractionIfError,
              child: AutofillGroup(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (_formError case final error?)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: Text(
                          error,
                          style: TextStyle(
                            color: Theme.of(context).colorScheme.error,
                          ),
                        ),
                      ),
                    _field(
                      .fullName,
                      'Full name',
                      Validators.required,
                      autofillHints: const [AutofillHints.name],
                    ),
                    _field(
                      .addressLine,
                      'Address',
                      Validators.required,
                      keyboardType: TextInputType.streetAddress,
                      autofillHints: const [AutofillHints.fullStreetAddress],
                    ),
                    _field(
                      .city,
                      'City',
                      Validators.required,
                      autofillHints: const [AutofillHints.addressCity],
                    ),
                    _field(
                      .postalCode,
                      'Postal code',
                      Validators.postalCode,
                      keyboardType: TextInputType.number,
                      inputFormatters: [
                        FilteringTextInputFormatter.digitsOnly,
                        LengthLimitingTextInputFormatter(5),
                      ],
                      autofillHints: const [AutofillHints.postalCode],
                    ),
                    _field(
                      .phone,
                      'Phone',
                      Validators.phone,
                      keyboardType: TextInputType.phone,
                      autofillHints: const [AutofillHints.telephoneNumber],
                      action: TextInputAction.done,
                    ),
                    const SizedBox(height: 12),
                    FilledButton(
                      onPressed: _submitting ? null : _submit,
                      child: const Text('Use this address'),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
