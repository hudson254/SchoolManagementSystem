#!/bin/bash
set -e

echo "Generating SSL certificates..."

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Create SSL directory
mkdir -p /etc/ssl/sms
cd /etc/ssl/sms

# Generate CA key and certificate
echo -e "${YELLOW}Generating CA certificate...${NC}"
openssl genrsa -out ca.key 2048
openssl req -x509 -new -nodes -key ca.key -sha256 -days 3650 -out ca.crt \
    -subj "/C=KE/ST=Nairobi/L=Nairobi/O=School Management System/CN=SMS CA"

# Generate server key and CSR
echo -e "${YELLOW}Generating server certificate...${NC}"
openssl genrsa -out server.key 2048
openssl req -new -key server.key -out server.csr \
    -subj "/C=KE/ST=Nairobi/L=Nairobi/O=School Management System/CN=localhost"

# Create config file for SAN
cat > san.cnf <<EOF
[req]
distinguished_name = req_distinguished_name
req_extensions = v3_req
prompt = no

[req_distinguished_name]
C = KE
ST = Nairobi
L = Nairobi
O = School Management System
CN = localhost

[v3_req]
keyUsage = keyEncipherment, dataEncipherment
extendedKeyUsage = serverAuth
subjectAltName = @alt_names

[alt_names]
DNS.1 = localhost
DNS.2 = *.localhost
DNS.3 = school.local
IP.1 = 127.0.0.1
IP.2 = ::1
EOF

# Generate server certificate
openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key -CAcreateserial \
    -out server.crt -days 3650 -sha256 -extensions v3_req -extfile san.cnf

# Create PKCS12 bundle
openssl pkcs12 -export -out certificate.pfx -inkey server.key -in server.crt -certfile ca.crt \
    -password pass:${SSL_PASSWORD:-password}

# Clean up
rm -f server.csr san.cnf

# Set permissions
chmod 600 server.key certificate.pfx

echo -e "${GREEN}SSL certificates generated successfully!${NC}"
echo -e "Location: /etc/ssl/sms/"
echo -e "  - CA Certificate: ca.crt"
echo -e "  - Server Certificate: server.crt"
echo -e "  - Server Key: server.key"
echo -e "  - PKCS12 Bundle: certificate.pfx"
echo -e ""
echo -e "${YELLOW}Add ca.crt to your trusted certificates for development.${NC}"