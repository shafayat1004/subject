/**
 * Tasteful beige terminal UI for observe doctor (true-color with ANSI fallback).
 */

const supportsTrueColor =
  process.env.FORCE_COLOR !== '0' &&
  (process.env.COLORTERM === 'truecolor' || process.env.TERM?.includes('256'));

/** Warm parchment palette */
const palette = {
  cream: [245, 240, 232],
  parchment: [237, 229, 216],
  taupe: [139, 119, 101],
  espresso: [92, 72, 58],
  sand: [201, 184, 150],
  sage: [92, 124, 98],
  terracotta: [180, 98, 82],
  amber: [196, 145, 88],
  line: [210, 198, 178],
};

/**
 * @param {[number, number, number]} rgb
 * @param {string} text
 */
function fg(rgb, text) {
  if (!supportsTrueColor) return text;
  return `\x1b[38;2;${rgb[0]};${rgb[1]};${rgb[2]}m${text}\x1b[0m`;
}

/**
 * @param {[number, number, number]} rgb
 * @param {string} text
 */
function bg(rgb, text) {
  if (!supportsTrueColor) return text;
  return `\x1b[48;2;${rgb[0]};${rgb[1]};${rgb[2]}m${text}\x1b[0m`;
}

function dim(text) {
  return supportsTrueColor ? `\x1b[2m${text}\x1b[0m` : text;
}

function bold(text) {
  return supportsTrueColor ? `\x1b[1m${text}\x1b[0m` : text;
}

const W = 62;

function hr() {
  return fg(palette.line, '  ' + '─'.repeat(W - 2));
}

function padRight(s, n) {
  const plain = s.replace(/\x1b\[[0-9;]*m/g, '');
  if (plain.length >= n) return s;
  return s + ' '.repeat(n - plain.length);
}

/**
 * @param {{ platforms: Array<{ platform: string, label: string, ready: boolean, checks: Array<{ id: string, ok: boolean, detail: string, informational?: boolean }> }>, shared?: Array<{ id: string, ok: boolean, detail: string }>, baseUrl?: string }} report
 */
export function renderDoctorUi(report) {
  const lines = [];

  lines.push('');
  lines.push(fg(palette.taupe, '  ╭' + '─'.repeat(W - 2) + '╮'));
  lines.push(
    fg(palette.taupe, '  │') +
      fg(palette.espresso, bold(padRight('  AppTodo Observe · Doctor', W - 2))) +
      fg(palette.taupe, '│')
  );
  lines.push(fg(palette.taupe, '  │') + fg(palette.sand, padRight('  Dev observability health check', W - 2)) + fg(palette.taupe, '│'));
  if (report.baseUrl) {
    lines.push(
      fg(palette.taupe, '  │') +
        dim(fg(palette.cream, padRight(`  Web target · ${report.baseUrl}`, W - 2))) +
        fg(palette.taupe, '│')
    );
  }
  lines.push(fg(palette.taupe, '  ╰' + '─'.repeat(W - 2) + '╯'));
  lines.push('');

  if (report.shared?.length) {
    lines.push(fg(palette.espresso, bold('  Shared · Native infrastructure')));
    lines.push(hr());
    for (const check of report.shared) {
      lines.push(formatCheck(check));
    }
    lines.push('');
  }

  for (const section of report.platforms) {
    const badge = section.ready
      ? fg(palette.sage, '● ready')
      : fg(palette.terracotta, '○ setup needed');
    lines.push(
      '  ' + fg(palette.espresso, bold(section.label)) + '  ' + badge
    );
    lines.push(hr());
    if (section.checks.length === 0) {
      lines.push(fg(palette.sand, '  (no checks)'));
    } else {
      for (const check of section.checks) {
        lines.push(formatCheck(check));
      }
    }
    lines.push('');
  }

  const readyCount = report.platforms.filter((p) => p.ready).length;
  const total = report.platforms.length;
  const summaryColor = readyCount === total ? palette.sage : readyCount > 0 ? palette.amber : palette.terracotta;

  lines.push(fg(palette.line, '  ' + '═'.repeat(W - 2)));
  lines.push(
    '  ' +
      fg(palette.taupe, 'Summary  ') +
      fg(summaryColor, bold(`${readyCount}/${total} platforms ready`))
  );

  const blockers = report.platforms.flatMap((p) =>
    p.checks.filter((c) => !c.ok && !c.informational).map((c) => ({ platform: p.label, detail: c.detail }))
  );
  if (blockers.length) {
    lines.push('');
    lines.push(fg(palette.espresso, bold('  Next steps')));
    lines.push(hr());
    for (const b of blockers.slice(0, 8)) {
      lines.push(fg(palette.cream, `  › ${b.platform}: `) + dim(b.detail));
    }
    if (blockers.length > 8) {
      lines.push(dim(fg(palette.sand, `  … and ${blockers.length - 8} more`)));
    }
  }

  if (report.playbooks?.length) {
    lines.push('');
    lines.push(fg(palette.espresso, bold('  Terminal playbooks')));
    lines.push(dim(fg(palette.sand, '  Native dev needs several terminals — keep each one running')));
    lines.push('');

    for (const book of report.playbooks) {
      const tag = book.highlight ? fg(palette.amber, ' ◆') : '';
      lines.push('  ' + fg(palette.espresso, bold(book.title)) + tag);
      if (book.subtitle) {
        lines.push(dim(fg(palette.sand, '  ' + book.subtitle)));
      }
      lines.push(hr());
      for (const term of book.terminals) {
        lines.push(fg(palette.taupe, `  T${term.n}  `) + fg(palette.cream, term.label));
        if (term.cwd) {
          lines.push(dim(fg(palette.sand, `      cd ${term.cwd}`)));
        }
        for (const line of term.lines) {
          if (line.startsWith('#')) {
            lines.push(dim(fg(palette.sand, `      ${line}`)));
          } else {
            lines.push(fg(palette.parchment, `      ${line}`));
          }
        }
        lines.push('');
      }
    }
  }

  lines.push('');
  return lines.join('\n');
}

/**
 * @param {{ id: string, ok: boolean, detail: string, informational?: boolean }} check
 */
function formatCheck(check) {
  const icon = check.informational
    ? fg(palette.sand, '◦')
    : check.ok
      ? fg(palette.sage, '✓')
      : fg(palette.terracotta, '✗');
  const label = fg(check.ok || check.informational ? palette.cream : palette.parchment, padRight(check.id, 22));
  const detail = check.ok
    ? dim(fg(palette.sand, check.detail))
    : fg(palette.cream, check.detail);
  return `  ${icon}  ${label}${detail}`;
}
