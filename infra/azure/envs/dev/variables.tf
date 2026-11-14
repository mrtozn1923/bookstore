variable "sql_admin_password" {
  type        = string
  description = "SQL Server administrator password (post 9 removes it from the state)."
  sensitive   = true
}
