output "ec2_public_ip" {
  value = aws_eip.app.public_ip
}

output "ec2_instance_id" {
  value = aws_instance.app.id
}

output "rds_endpoint" {
  value = aws_db_instance.main.endpoint
}

output "rds_address" {
  value = aws_db_instance.main.address
}

output "ecr_repository_url" {
  value = aws_ecr_repository.api.repository_url
}

output "ecr_repository_web_url" {
  value = aws_ecr_repository.web.repository_url
}

output "github_actions_role_arn" {
  value = aws_iam_role.github_actions.arn
}

# Deriva de var.project_name/var.environment (ssm.tf) — a pipeline de CI/CD lê
# este output em vez de hardcodar o prefixo, para respeitar o que estiver
# configurado em variables.tf/terraform.tfvars.
output "ssm_path_prefix" {
  value = local.ssm_path_prefix
}

output "vpc_id" {
  value = aws_vpc.main.id
}

output "public_subnet_id" {
  value = aws_subnet.public.id
}
