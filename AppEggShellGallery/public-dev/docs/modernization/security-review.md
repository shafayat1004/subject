# Security Review

A focused pass over the framework's auth, SQL, crypto, and transport surfaces was conducted as part
of the architectural review documented in the [Architecture](./architecture/index.md) review (section 11). The findings below are
**candidate leads to verify, not proven exploits.** Access-control defaults in particular often have
subtle intended paths; confirm the exact branches and intended semantics before making any change.

The framework has real strengths here: codec evolution validation, optimistic concurrency, typed
access predicates, and centralized (not per-handler) access control. The work is mostly verifying
defaults and hardening one SQL identifier surface.

---

## Candidate findings

| # | Area | Finding | Severity | Direction |
|---|------|---------|----------|-----------|
| 1 | AuthZ | Possible **default-allow** when a subject or view has no session handling configured (`maybeSessionHandling = None` mapping to `Grant`) in the host `AccessControl` access path. | **Critical (verify)** | Confirm the exact branch in `LibLifeCycleHost/src/Web/Api/V1/`. If real, flip the default to `Deny` and require explicit `ApiAccess` for external exposure. |
| 2 | SQL | Schema/lifecycle/ecosystem **names interpolated** into SQL via `sprintf` (e.g., `SqlServerSetup.fs`, `SqlServerDataProtectionXmlRepository.fs`, `SqlServerTransferBlobHandler.fs`). Data values are parameterized; identifiers are not. | **High** | These names are framework/admin-controlled today, so impact is bounded. Add identifier whitelisting (`^[A-Za-z0-9_]+$` regex check) or `QUOTENAME`-style escaping to be safe. |
| 3 | Transport | CORS uses `SetIsOriginAllowedToAllowWildcardSubdomains()` **combined with** `AllowCredentials()` (`Host/K8S/Api/Startup.fs`). | Medium | Pin explicit subdomains, or drop credentials for wildcard origins. These two settings together are potentially exploitable for CSRF. |
| 4 | Serialization | Reflection + `MakeGenericMethod` on projection types in `JsonEncoding.fs`. | Medium | Fine as long as projection types come only from compiled lifecycle definitions (they do). Add a guard if that path ever changes to accept user-supplied types. |
| 5 | Session | Cookies correctly set `HttpOnly` / `Secure` (non-dev) / `SameSite=Lax`, but the expiry is **2 years**. | Low | Shorten the expiry and revalidate sessions on privilege changes. |
| 6 | Crypto | `SssXmlEncryptor.fs` (currently untracked) uses AES-GCM correctly: CSPRNG nonce, 16-byte tag, verified on decrypt. | None | Self-labeled "foundational example." Harden error handling before wiring to production. |

---

## Recommended priority

1. **Verify finding #1 first.** A default-allow on unauthenticated grain access would be a critical
   vulnerability. Read the exact code path in `AccessControl` / `GenericHttpHandler` before any
   change; the intended path may be clear once inspected.
2. **Fix finding #2 next.** SQL identifier interpolation is a concrete, low-effort fix (regex
   whitelist on framework-controlled names) and it closes a real injection surface even if current
   exploitation requires admin access.
3. **Fix finding #3.** CORS / `AllowCredentials` with wildcard subdomains is a configuration issue
   with a clear fix: enumerate the allowed subdomains explicitly.
4. Findings #4, #5, and #6 are hardening items with no known active risk.

---

## Relevant source locations

| Finding | Source |
|---------|--------|
| #1 | `LibLifeCycleHost/src/Web/Api/V1/GenericHttpHandler.fs`, access-control path around `maybeSessionHandling` |
| #2 | `LibLifeCycleHost/src/Storage/SqlServer/SqlServerSetup.fs`, `SqlServerDataProtectionXmlRepository.fs`, `SqlServerTransferBlobHandler.fs` |
| #3 | `LibLifeCycleHost/src/Host/K8S/Api/Startup.fs`, CORS configuration |
| #4 | `LibLifeCycleHost/src/Web/Api/V1/JsonEncoding.fs`, `generateAutoEncoder` / `generateAutoDecoder` reflection paths |
| #5 | Cookie configuration in the host startup; search for `ExpireTimeSpan` or `IsPersistent` |
| #6 | `SssXmlEncryptor.fs` (location may vary; was untracked at time of review) |

---

## Status

Not started. Security fixes are independent of other goals and goals A through H; they can be
verified and applied at any time without waiting for the modernization work to complete. See
[Goals & Roadmap: Security hardening](./modernization/goals-and-roadmap.md#security-hardening).
