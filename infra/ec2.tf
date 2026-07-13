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

  # user_data só instala o k3s e o timer de refresh das credenciais do ECR —
  # o deploy da aplicação (ConfigMap/Secret/Deployments) é feito pela pipeline
  # de CI/CD via SSM Run Command, não no boot da instância.
  user_data = templatefile("${path.module}/templates/user_data.sh.tpl", {
    region            = var.aws_region
    ecr_registry_host = split("/", aws_ecr_repository.api.repository_url)[0]
  })

  tags = {
    Name = "${var.project_name}-${var.environment}-api"
  }
}

resource "aws_eip_association" "app" {
  instance_id   = aws_instance.app.id
  allocation_id = aws_eip.app.id
}
