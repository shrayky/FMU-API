/**
 * Оставляет в указанном каталоге только bundle, lib и favicon.ico (для папки публикации).
 * Использование: node scripts/prune-wwwroot.mjs <абсолютный_путь_к_wwwroot>
 */
import { readdir, rm } from 'node:fs/promises';
import { join } from 'node:path';

const targetDir = process.argv[2];
if (!targetDir) {
  console.error('Укажите путь к wwwroot: node scripts/prune-wwwroot.mjs <путь>');
  process.exit(1);
}

const keep = new Set(['bundle', 'lib', 'favicon.ico']);

const entries = await readdir(targetDir, { withFileTypes: true });
for (const entry of entries) {
  if (keep.has(entry.name)) {
    continue;
  }
  await rm(join(targetDir, entry.name), { recursive: true, force: true });
}
