resource "aws_db_instance" "main" {
  identifier     = "${var.project_name}-${var.environment}"
  engine         = "sqlserver-ex"
  engine_version = var.db_engine_version
  license_model  = "license-included"

  instance_class    = var.db_instance_class
  allocated_storage = var.db_allocated_storage
  storage_type      = "gp3"

  # Criptografia em repouso via chave gerenciada padrão da AWS (aws/rds) -
  # suportada pela instance class atual (db.t3.micro). Sem kms_key_id
  # explícito porque não há requisito de rotação/BYOK além do padrão da AWS.
  storage_encrypted = true

  username = var.db_master_username
  password = var.db_master_password

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.rds.id]

  multi_az            = false
  publicly_accessible = false

  backup_retention_period    = var.db_backup_retention_period
  auto_minor_version_upgrade = true
  copy_tags_to_snapshot      = true

  # Sem proteção contra exclusão nem snapshot final: projeto de estudo.
  deletion_protection = false
  skip_final_snapshot = true

  tags = {
    Name = "${var.project_name}-${var.environment}-sqlserver"
  }
}
