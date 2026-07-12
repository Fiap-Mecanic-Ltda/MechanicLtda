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

output "vpc_id" {
  value = aws_vpc.main.id
}

output "public_subnet_id" {
  value = aws_subnet.public.id
}
