variable "aws_region" {
  type    = string
  default = "us-east-1"
}

variable "project_name" {
  type    = string
  default = "mechanicltda"
}

variable "environment" {
  type    = string
  default = "prod"
}

variable "github_repository" {
  type        = string
  description = "Repositório GitHub (owner/repo) autorizado a assumir a role de CI/CD via OIDC."
  default     = "Fiap-Mecanic-Ltda/MechanicLtda"
}

# Rede

variable "vpc_cidr" {
  type    = string
  default = "10.0.0.0/16"
}

variable "public_subnet_cidr" {
  type    = string
  default = "10.0.1.0/24"
}

variable "db_subnet_a_cidr" {
  type    = string
  default = "10.0.2.0/24"
}

variable "db_subnet_b_cidr" {
  type    = string
  default = "10.0.3.0/24"
}

variable "api_allowed_cidr" {
  type        = string
  description = "CIDR com acesso à API na porta 8080."
  default     = "0.0.0.0/0"
}

# EC2

variable "ec2_instance_type" {
  type        = string
  description = "Tipo da instância EC2 que roda a API."
  default     = "t3.micro"
}

variable "ec2_key_pair_name" {
  type        = string
  description = "Key pair EC2 usado no acesso SSH."
}

# RDS

variable "db_instance_class" {
  type    = string
  default = "db.t3.micro"
}

variable "db_engine_version" {
  type        = string
  description = "Versão do engine SQL Server Express (sqlserver-ex)."
}

variable "db_master_username" {
  type        = string
  description = "Usuário master do RDS. Não pode ser 'sa'."
  default     = "mechanicltda_admin"
}

variable "db_master_password" {
  type        = string
  description = "Senha do usuário master do RDS."
  sensitive   = true
}

variable "db_allocated_storage" {
  type        = number
  description = "Storage do RDS em GiB."
  default     = 20
}

variable "db_backup_retention_period" {
  type    = number
  default = 7
}

# ECR

variable "ecr_repository_name" {
  type    = string
  default = "mechanicltda-api"
}

variable "ecr_repository_web_name" {
  type    = string
  default = "mechanicltda-web"
}

# Segredos da aplicação

variable "jwt_secret_key" {
  type        = string
  description = "Chave secreta de assinatura dos tokens JWT (mínimo 32 caracteres)."
  sensitive   = true
}

variable "encryption_cpf_cnpj_key" {
  type        = string
  description = "Chave de criptografia dos campos CPF/CNPJ (mínimo 32 caracteres)."
  sensitive   = true
}

variable "email_password" {
  type        = string
  description = "Senha de app do Gmail para envio de e-mails via SMTP."
  sensitive   = true
}
