#!/bin/bash
# Makes Oneon's own code signing certificate, once. Signing every build with it keeps the app's identity the same
# from one build to the next, so macOS keeps the Accessibility permission across updates (ad-hoc signed builds lose
# it every time). It is self-signed: it does not replace Apple notarization, and macOS still shows "Open Anyway"
# the first time.
#
#   installer/macos/make-signing-cert.sh            # adds "Oneon Self-Signed" to your login keychain (local builds)
#
# It also writes oneon-signing.p12 and oneon-signing.p12.base64 to ~/Desktop/oneon-signing/. For release builds,
# add two repository secrets (GitHub → Settings → Secrets and variables → Actions):
#   MACOS_SIGNING_P12       the content of oneon-signing.p12.base64
#   MACOS_SIGNING_PASSWORD  the password this script prints
# Then delete that folder. Keep the private key secret: anyone holding it can sign an app macOS treats as Oneon.
set -euo pipefail

name="Oneon Self-Signed"
out="$HOME/Desktop/oneon-signing"
if security find-certificate -c "$name" >/dev/null 2>&1; then
  echo "\"$name\" is already in your keychain. Nothing was changed."
  exit 0
fi
mkdir -p "$out"
chmod 700 "$out"
password="$(uuidgen)"

cat > "$out/cert.cnf" <<EOF
[req]
distinguished_name = dn
prompt = no
x509_extensions = ext
[dn]
CN = $name
[ext]
basicConstraints = critical,CA:false
keyUsage = critical,digitalSignature
extendedKeyUsage = critical,codeSigning
EOF

openssl req -x509 -newkey rsa:2048 -nodes -days 3650 -config "$out/cert.cnf" \
  -keyout "$out/key.pem" -out "$out/cert.pem" 2>/dev/null
# -legacy: macOS's security tool cannot read the newer PKCS#12 encryption of OpenSSL 3.
legacy=""
if openssl version | grep -q "^OpenSSL 3"; then legacy="-legacy"; fi
openssl pkcs12 -export $legacy -inkey "$out/key.pem" -in "$out/cert.pem" -name "$name" \
  -out "$out/oneon-signing.p12" -passout "pass:$password"
base64 < "$out/oneon-signing.p12" > "$out/oneon-signing.p12.base64"
rm -f "$out/key.pem" "$out/cert.cnf"

security import "$out/oneon-signing.p12" -k "$HOME/Library/Keychains/login.keychain-db" -P "$password" -T /usr/bin/codesign

echo
echo "Done: \"$name\" is in your login keychain."
echo "For release builds, add these repository secrets:"
echo "  MACOS_SIGNING_P12       = content of $out/oneon-signing.p12.base64"
echo "  MACOS_SIGNING_PASSWORD  = $password"
echo "Then delete $out."
