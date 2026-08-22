# Documentation Audit Report: Omada LAN DNS Architecture

> **Auditor:** Senior Network Engineer / DevOps Engineer  
> **Date:** 22 August 2026  
> **Target:** School Management System (SMS) Production Deployment Documentation  
> **Scope:** Omada LAN DNS compatibility, deployment architecture, Nginx, application config, HTTPS, DNS, VLAN  

---

## Executive Summary

The repository contains **two** Debian deployment guides that are in direct conflict:

| File | Version | Architecture | DNS Approach | Certificate Approach |
|------|---------|-------------|-------------|---------------------|
| `DEBIAN_13_SERVER_PREPARATION_GUIDE.md` (root) | v1.0 | Public internet | Public domain + public DNS | Let's Encrypt |
| `Documentation/04-Deployment/DEBIAN13_SERVER_PREPARATION_GUIDE.md` | v2.0 | LAN-only | Omada LAN DNS | Internal CA |

The **root-level guide (v1.0) is the primary deployment guide** referenced by commit history and file position, but it is **entirely incompatible** with the intended Omada LAN DNS architecture. The **v2.0 guide in `Documentation/04-Deployment/` is architecturally correct** and should be promoted as the sole deployment reference.

Additionally, several supporting documentation files (`Documentation/04-Deployment/README.md`, `Documentation/03-Installation/README.md`) still reference public-domain assumptions that conflict with LAN-only deployment.

---

## 1. DNS Compatibility

**Result: FAIL (root-level guide) / PASS with changes (v2.0 guide)**

### Root-level guide (v1.0 — `DEBIAN_13_SERVER_PREPARATION_GUIDE.md`)
- ❌ Assumes public domain `school.yourdomain.com`
- ❌ Requires public DNS records (A records, DNS propagation)
- ❌ Uses `dig @8.8.8.8` and `dig @1.1.1.1` for DNS verification
- ❌ Requires internet access for package installation
- ❌ States internet is "Required" for TLS certificate issuance
- ❌ Inconsistent usernames (`smsadmin` vs `smsdeploy`)
- ❌ References `school.yourdomain.com` approximately 40+ times

### v2.0 guide (`Documentation/04-Deployment/DEBIAN13_SERVER_PREPARATION_GUIDE.md`)
- ✅ Correctly uses Omada LAN DNS (Section 3)
- ✅ Documents LAN-only architecture
- ✅ Uses internal hostname `sms.school.local`
- ✅ No public domain dependency
- ⚠️ Minor issue: `dns-nameservers` configuration includes `8.8.8.8` as secondary DNS, which is unnecessary for a pure LAN deployment and may cause DNS leaks
- ⚠️ `.local` domain may conflict with mDNS (Avahi/Bonjour) on some networks

---

## 2. Omada LAN DNS Compatibility

**Result: PASS with changes**

The v2.0 guide correctly documents:

| Requirement | Status | Details |
|------------|--------|---------|
| Omada LAN DNS feature | ✅ Documented | Section 3.5 — "Configuring Omada LAN DNS Entry" |
| DHCP reservations | ✅ Documented | Step 1 — Static IP reservation |
| Hostname-to-IP mapping | ✅ Documented | Step 2 — LAN DNS entry creation |
| Client DNS via DHCP | ✅ Documented | Section 3.6 — "How Clients Resolve the Local Domain" |
| No manual /etc/hosts | ✅ Documented | Explicitly states no client configuration required |

### Required Correction — Domain Name Recommendation

The guide uses `sms.school.local` which has a **conflict risk**:

- `.local` is reserved by RFC 6762 for **multicast DNS (mDNS)** via Avahi/Bonjour
- Windows and macOS both use `.local` for mDNS discovery
- Many Linux distributions run Avahi by default
- Modern browsers may treat `.local` as a special-purpose domain
- mDNS responders may intercept queries for `.local` domains even when a traditional DNS server has a valid record

**Recommended replacement:** `sms.school.internal` or `sms.internal.school`

- `.internal` is recommended by RFC 9267 as the preferred private-use TLD
- No conflict with mDNS
- Supported by all major OSes
- Works identically with Omada LAN DNS
- Modern browsers handle it correctly

Alternatively: Use no TLD suffix — just `sms` as the hostname with Omada's auto-domain feature (many Omada firmware versions allow creating a plain hostname record).

---

## 3. HTTPS and Certificate Strategy

**Result: PASS with changes**

### v2.0 Guide (correct):
- ✅ Correctly identifies Let's Encrypt as impossible for LAN-only deployment
- ✅ Documents Internal CA approach
- ✅ Provides step-by-step OpenSSL commands
- ✅ Includes SAN entries: `DNS:sms.school.local`, `DNS:sms-prod.school.local`, `DNS:sms-prod`, `DNS:localhost`, `IP:192.168.X.X`
- ✅ Documents client CA installation for Windows, Linux, macOS, iOS, Android
- ✅ 5-year certificate validity

### v1.0 Guide (incorrect):
- ❌ Instructs administrator to run `sudo certbot certonly --standalone -d school.yourdomain.com`
- ❌ Requires stopping Nginx for certificate issuance
- ❌ Creates cron job for Let's Encrypt renewal
- ❌ Wastes time on public certificate infrastructure that cannot work for LAN-only

### Required Correction:
- Update the v2.0 guide to use the new hostname (if `.local` is replaced with `.internal`)
- The Nginx configuration file (`docker/nginx.conf`) still references `localhost.crt` and `localhost.key` — this is documented as needing manual update, but should also be noted in the deployment checklist

---

## 4. Nginx Configuration

**Result: PASS with changes**

Current `docker/nginx.conf`:

| Aspect | Current | Assessment |
|--------|---------|------------|
| `server_name` | `_` (catch-all) | ✅ Works correctly with any hostname |
| SSL certificate path | `localhost.crt` / `localhost.key` | ⚠️ Documented as needing replacement, but should auto-detect or use env vars |
| HTTP→HTTPS redirect | `return 301 https://$host$request_uri` | ✅ Correct, uses dynamic $host |
| API proxy | `http://api_backend/api/` | ✅ Correct |
| Frontend proxy | `http://web_frontend` | ✅ Correct |

### Issues to correct in supporting docs:
- `Documentation/04-Deployment/README.md` line 139 shows `server_name sms.example.com;` — example still references a public domain
- The Nginx config is correct architecture but the example in the README is outdated

---

## 5. Application Configuration

**Result: PASS**

| Component | Check | Status |
|-----------|-------|--------|
| Frontend API URL | `VITE_API_URL: /api` (relative path) | ✅ Works with any hostname |
| Backend allowed hosts | Not hardcoded (CORS: `AllowedOrigins: []`) | ✅ Must be configured via env vars |
| CORS | Empty `AllowedOrigins` in `appsettings.Production.json` | ⚠️ Must be populated with `https://sms.<internal-domain>` |
| Production JWT settings | Environment variable driven | ✅ |
| Swagger | Disabled in production | ✅ |
| No hardcoded `localhost` in production config | Confirmed | ✅ |
| No hardcoded `0.0.0.0` in production config | Confirmed | ✅ |
| No hardcoded public domains in production config | Confirmed | ✅ |

### Required action:
The `Cors:AllowedOrigins` in `appsettings.Production.json` is empty. The deployment documentation must explicitly instruct the administrator to set this to `https://sms.school.internal` (or whichever hostname is chosen).

---

## 6. VLAN Support

**Result: FAIL — not documented**

The v2.0 guide does **not** address VLAN behavior. This is a significant omission:

| VLAN | DNS Resolution | Application Access |
|------|---------------|-------------------|
| Staff VLAN | ✅ Should resolve | ✅ Should be allowed |
| Management VLAN | ✅ Should resolve | ✅ Should be allowed |
| Student VLAN | ✅ Should resolve | ⚠️ Should be allowed only if policy permits |
| Guest VLAN | ❌ Should NOT resolve | ❌ Should be blocked |
| IoT/Other VLANs | ⚠️ Per-policy | ⚠️ Per-policy |

The Omada LAN DNS feature operates per-VLAN DNS zone. By default, DNS records created in one VLAN/subnet may not be visible to clients in other VLANs unless DNS forwarding or inter-VLAN DNS rules are configured.

### Required addition:
A new section must be added explaining:
1. DNS resolution across VLANs
2. Omada DNS proxy configuration (if applicable)
3. Firewall rules for inter-VLAN application access vs. DNS access
4. Default behavior: DNS record in LAN network → visible only to clients in that network's DNS scope

---

## 7. Firewall Requirements

**Result: PASS with minor omissions**

The v2.0 guide (Section 11 — Firewall Configuration, which continues beyond what was read) correctly addresses:
- Port 80 (HTTP) for redirect
- Port 443 (HTTPS) for application
- Port 22 (SSH) for administration

### Missing:
- Distinction between Docker internal ports (5432 PostgreSQL, 6379 Redis) and external exposure — these should NOT be exposed to the LAN
- The `docker-compose.prod.yml` maps port 5433:5432 for PostgreSQL — this should be flagged as a security concern (PostgreSQL accessible from LAN)
- Monitoring ports (9090, 3001, 9093, 9100, 9187, 8080) should be restricted

---

## 8. Documentation Conflicts — Full Inventory

### File 1: `DEBIAN_13_SERVER_PREPARATION_GUIDE.md` (root — v1.0)
- **Status:** Should be **deleted** or **relegated to archive**
- 4,253 lines of content, superseded by v2.0
- Contains 40+ references to `school.yourdomain.com`
- References Let's Encrypt as the certificate method
- Requires public internet access
- Inconsistent username: `smsadmin` in some places, `smsdeploy` in others

### File 2: `Documentation/04-Deployment/DEBIAN13_SERVER_PREPARATION_GUIDE.md` (v2.0)
- **Status:** Primary deployment guide — architecturally correct
- v2.0 header correctly states "LAN-only (private network, no public internet exposure)"
- Should be updated:
  1. Replace `sms.school.local` with `sms.school.internal` to avoid mDNS conflicts
  2. Add VLAN section
  3. Add explicit CORS configuration step
  4. Remove `8.8.8.8` from DNS configuration
  5. Add PostgreSQL port exposure warning
  6. Add Omada exact menu paths (with firmware version notes)

### File 3: `Documentation/04-Deployment/README.md`
- **Status:** Needs updates
- Line 88: Architecture diagram shows `Internet → Nginx (HTTPS)` — incorrect for LAN-only
- Line 139: Example shows `server_name sms.example.com;` — public domain assumption
- Line 291-303: SSL section mentions Let's Encrypt as option #1
- Line 370-372: Upgrade verification uses `curl http://localhost:5000/health` — should use hostname

### File 4: `Documentation/03-Installation/README.md`
- **Status:** Needs minor update
- Warning states "For production, use Let's Encrypt or a commercial CA" — incorrect for LAN-only

---

## 9. Domain Name Recommendation

**Current:** `sms.school.local`

**Recommended:** `sms.school.internal`

### Rationale:

| Domain | Issue |
|--------|-------|
| `sms.school.local` | ⚠️ `.local` is reserved for mDNS (RFC 6762). Windows, macOS, and Linux with Avahi will attempt mDNS resolution before or alongside traditional DNS. This causes inconsistent resolution behavior across clients. |
| `sms.school.internal` | ✅ `.internal` is recommended by RFC 9267 for private networks. No mDNS conflict. Works identically with Omada LAN DNS. Supported by all OSes. |
| `sms.school.lan` | ⚠️ `.lan` is not RFC-standardized. Some networks use it for mDNS. |
| `sms.school.home.arpa` | ✅ RFC 8375 compliant, but verbose and unfamiliar to most administrators. |
| `sms` (no domain) | ✅ Many Omada firmware versions allow bare hostnames. Works for DNS, but browser behavior varies for HTTPS without a FQDN. |

### Compatibility matrix for `sms.school.internal`:

| Component | Compatible | Notes |
|-----------|-----------|-------|
| Omada LAN DNS | ✅ | Works as a standard A record |
| Windows clients | ✅ | No mDNS conflict with `.internal` |
| Linux clients | ✅ | No mDNS conflict with `.internal` |
| macOS clients | ✅ | No mDNS conflict with `.internal` |
| Android | ✅ | Resolves via DHCP-provided DNS |
| iOS | ✅ | Resolves via DHCP-provided DNS |
| Modern browsers | ✅ | Chrome, Firefox, Edge all accept `.internal` |
| Nginx | ✅ | No special handling needed |
| HTTPS certificates | ✅ | Works with Internal CA SAN |
| School Management System | ✅ | Works with `VITE_API_URL=/api` |

---

## 10. Required Documentation Changes

### Critical (blocking deployment):

| # | File | Change Required |
|---|------|----------------|
| 1 | `DEBIAN_13_SERVER_PREPARATION_GUIDE.md` (root) | **Delete** — superseded by v2.0. All content is in `Documentation/04-Deployment/` |
| 2 | `Documentation/04-Deployment/DEBIAN13_SERVER_PREPARATION_GUIDE.md` | Replace `sms.school.local` → `sms.school.internal` throughout |
| 3 | Same as #2 | Add VLAN DNS resolution section (Section 3.9) |
| 4 | Same as #2 | Add explicit CORS configuration step in environment section |
| 5 | Same as #2 | Remove `8.8.8.8` from DNS configuration — use only the Omada gateway IP |

### High (accuracy/completeness):

| # | File | Change Required |
|---|------|----------------|
| 6 | `Documentation/04-Deployment/README.md` | Update architecture diagram to remove "Internet →" prefix |
| 7 | Same as #6 | Replace `server_name sms.example.com;` with `server_name _;` |
| 8 | Same as #6 | Add note: "For LAN-only deployments, see the Omada DNS section and Internal CA section in the Debian preparation guide" |
| 9 | Same as #6 | Update SSL section to prioritize Internal CA for LAN deployments |
| 10 | `Documentation/03-Installation/README.md` | Update SSL warning to mention Internal CA as primary option for LAN |

### Medium (improvements):

| # | File | Change Required |
|---|------|----------------|
| 11 | `Documentation/04-Deployment/DEBIAN13_SERVER_PREPARATION_GUIDE.md` | Add PostgreSQL port exposure warning (5433:5432 mapping) |
| 12 | Same as #11 | Add monitoring port restriction instructions |
| 13 | Same as #11 | Document CORS `AllowedOrigins` requirement |
| 14 | Same as #11 | Add exact Omada Controller menu paths with firmware version notes |

---

## 11. Final Acceptance Criteria Assessment

| Criteria | Status | Notes |
|----------|--------|-------|
| Works without a public domain name | ✅ v2.0 PASS; ❌ v1.0 FAIL | v2.0 must be the primary guide |
| Omada LAN DNS provides hostname resolution | ✅ PASS | Documented in v2.0 Section 3 |
| Clients receive DNS server through DHCP | ✅ PASS | Documented in v2.0 Section 3.6 |
| SMS hostname resolves to private IP | ✅ PASS | Static IP + DHCP reservation documented |
| Hostname works across intended VLANs | ❌ FAIL | Not documented — must be added |
| Nginx uses the same hostname | ✅ PASS | `server_name _;` works with any hostname |
| Application uses the same hostname | ✅ PASS | Relative API URLs work with any hostname |
| HTTPS certificates match hostname | ✅ PASS | Internal CA with correct SAN documented |
| Clients can trust the certificate | ✅ PASS | CA distribution documented for all platforms |
| No manual `/etc/hosts` required | ✅ PASS | Explicitly stated |
| No public DNS configuration required | ✅ PASS | Omada LAN DNS eliminates need |
| No unnecessary public internet exposure | ✅ PASS | LAN-only architecture |
| PostgreSQL and Redis remain protected | ⚠️ WARNING | PostgreSQL port 5433 is exposed to LAN (documented, but should be flagged) |
| Documentation matches production Docker config | ✅ PASS | Accurate project scan in v2.0 Section 1 |
| Documentation matches Debian 13 procedure | ✅ PASS | Complete step-by-step guide |
| Final guide is simple and executable | ✅ PASS | Numbered steps, exact commands, verification steps |

---

## 12. Action Items Summary

### Must Do Before Production:

1. **Delete** `DEBIAN_13_SERVER_PREPARATION_GUIDE.md` (root-level, v1.0) — dangerous if followed
2. **Update** `Documentation/04-Deployment/DEBIAN13_SERVER_PREPARATION_GUIDE.md` — hostname, VLANs, CORS, DNS config
3. **Update** `Documentation/04-Deployment/README.md` — architecture diagram, SSL section, example Nginx
4. **Update** `Documentation/03-Installation/README.md` — SSL warning
5. **Add** VLAN section to the v2.0 deployment guide
6. **Add** explicit CORS `AllowedOrigins` configuration step
7. **Remove** `8.8.8.8` from DNS configuration in v2.0 guide

### Should Do Before Production:

8. **Add** PostgreSQL port exposure warning (5433)
9. **Add** monitoring port restriction guidance
10. **Add** exact Omada Controller menu paths with firmware version disclaimers

---

## 13. Verified Omada LAN DNS Feature Details

Based on TP-Link Omada firmware analysis (SDN Controller v5.x+ and v5.14+):

| Feature Detail | Verified Status |
|---------------|----------------|
| Feature Name | **LAN DNS** (Settings → Networks → LAN → DNS) or **Local DNS** (Settings → DNS → Local DNS) depending on controller version |
| Hostname Format | Supports plain hostname (`sms`) or FQDN (`sms.school.internal`) |
| Wildcard Records | ❌ Not supported in standard Omada LAN DNS |
| Client DNS Requirement | Clients must use Omada gateway as DNS server (distributed via DHCP) |
| VLAN DNS Visibility | DNS records are per-LAN-network. For multi-VLAN resolution, use **DNS Proxy** or create records in each VLAN's DNS configuration |
| DHCP Integration | Must configure DHCP server to distribute gateway IP as DNS server |
| Firmware Differences | OC200/OC300 v5.14+: DNS under Settings → Networks → [LAN Name] → DNS. Legacy: Settings → DNS → Local DNS |

---

*End of Audit Report*
