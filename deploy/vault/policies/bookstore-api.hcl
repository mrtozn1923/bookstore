# Bookstore.Api yalnız kendi KV secret'larını ve kendi DB rolünü okuyabilir
path "secret/data/bookstore/api" {
  capabilities = ["read"]
}

path "database/creds/bookstore-api" {
  capabilities = ["read"]
}
