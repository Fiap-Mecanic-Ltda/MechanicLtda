data "aws_ami" "al2023" {
  most_recent = true
  owners      = ["amazon"]

  filter {
    name   = "name"
    values = ["al2023-ami-*-x86_64"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

resource "aws_eip" "app" {
  domain = "vpc"

  tags = {
    Name = "${var.project_name}-${var.environment}-eip"
  }
}

resource "aws_instance" "app" {
  ami                    = data.aws_ami.al2023.id
  instance_type          = var.ec2_instance_type
  subnet_id              = aws_subnet.public.id
  vpc_security_group_ids = [aws_security_group.ec2.id]
  iam_instance_profile   = aws_iam_instance_profile.ec2_profile.name
  key_name               = var.ec2_key_pair_name

  associate_public_ip_address = true

  root_block_device {
    volume_size = 30
    volume_type = "gp3"
    encrypted   = true
  }

  user_data = templatefile("${path.module}/templates/user_data.sh.tpl", {
    region          = var.aws_region
    ecr_repo_url    = aws_ecr_repository.api.repository_url
    image_tag       = var.container_image_tag
    ssm_path_prefix = local.ssm_path_prefix
  })

  depends_on = [
    aws_db_instance.main,
    aws_ssm_parameter.db_connection_string,
    aws_ssm_parameter.jwt_secret_key,
    aws_ssm_parameter.encryption_key,
    aws_ssm_parameter.email_password,
    aws_ssm_parameter.app_base_url_aprovacao,
  ]

  tags = {
    Name = "${var.project_name}-${var.environment}-api"
  }
}

resource "aws_eip_association" "app" {
  instance_id   = aws_instance.app.id
  allocation_id = aws_eip.app.id
}
