locals {
  ssm_path_prefix = "/${var.project_name}/${var.environment}"

  db_connection_string = "Server=${aws_db_instance.main.address},1433;Database=MechanicLtdaDb;User Id=${var.db_master_username};Password=${var.db_master_password};TrustServerCertificate=True;"
}

resource "aws_ssm_parameter" "jwt_secret_key" {
  name  = "${local.ssm_path_prefix}/jwt-secret-key"
  type  = "SecureString"
  value = var.jwt_secret_key
}

resource "aws_ssm_parameter" "encryption_key" {
  name  = "${local.ssm_path_prefix}/encryption-key"
  type  = "SecureString"
  value = var.encryption_cpf_cnpj_key
}

resource "aws_ssm_parameter" "email_password" {
  name  = "${local.ssm_path_prefix}/email-password"
  type  = "SecureString"
  value = var.email_password
}

resource "aws_ssm_parameter" "db_connection_string" {
  name  = "${local.ssm_path_prefix}/db-connection-string"
  type  = "SecureString"
  value = local.db_connection_string
}

resource "aws_ssm_parameter" "app_base_url_aprovacao" {
  name  = "${local.ssm_path_prefix}/app-base-url-aprovacao"
  type  = "String"
  value = "http://${aws_eip.app.public_ip}:8080"
}
