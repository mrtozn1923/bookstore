vault {
  address = "http://127.0.0.1:8200"
}

auto_auth {
  method "approle" {
    config = {
      role_id_file_path                   = "/vault/agent/role-id"
      secret_id_file_path                 = "/vault/agent/secret-id"
      remove_secret_id_file_after_reading = false
    }
  }
}

template_config {
  # Lease yenilenemeyince yeni credential alır ve dosyayı yeniden yazar
  exit_on_retry_failure = true
}

template {
  source      = "/vault/agent/appsettings.vault.json.ctmpl"
  destination = "/vault/secrets/appsettings.vault.json"
  perms       = "0640"
}
