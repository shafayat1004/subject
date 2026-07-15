/**
 * Timeout helpers for observe workflows — fail fast when end states are unreachable.
 */

/**
 * @template T
 * @param {() => Promise<T>} fn
 * @param {number} ms
 * @param {string} label
 */
export async function withTimeout(fn, ms, label = 'operation') {
  let timer;
  const timeout = new Promise((_, reject) => {
    timer = setTimeout(() => reject(new Error(`${label} timed out after ${ms}ms`)), ms);
  });
  try {
    return await Promise.race([fn(), timeout]);
  } finally {
    clearTimeout(timer);
  }
}

/**
 * Poll until predicate returns truthy or deadline passes.
 * @template T
 * @param {() => Promise<T | null | undefined | false>} predicate
 * @param {{ timeoutMs?: number, pollMs?: number, log?: (msg: string) => void, label?: string }} [options]
 * @returns {Promise<T>}
 */
export async function pollUntil(predicate, options = {}) {
  const timeoutMs = options.timeoutMs ?? 120_000;
  const pollMs = options.pollMs ?? 750;
  const log = options.log ?? (() => {});
  const label = options.label ?? 'condition';
  const deadline = Date.now() + timeoutMs;
  let lastLogAt = 0;

  while (Date.now() < deadline) {
    const result = await predicate();
    if (result) return result;

    const now = Date.now();
    if (now - lastLogAt >= 5000) {
      log(`waiting for ${label} (${Math.round((timeoutMs - (deadline - now)) / 1000)}s)...`);
      lastLogAt = now;
    }
    await new Promise((r) => setTimeout(r, pollMs));
  }

  throw new Error(`${label} not reached within ${timeoutMs}ms`);
}
