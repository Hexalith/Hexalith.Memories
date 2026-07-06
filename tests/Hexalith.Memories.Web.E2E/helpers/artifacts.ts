import { mkdir, readdir, readFile, stat, writeFile } from 'node:fs/promises';
import path from 'node:path';

export const evidenceDirectory = path.resolve('test-results', 'evidence');

const restrictedPatterns: readonly RegExp[] = [
  /Bearer\s+[A-Za-z0-9._-]+/i,
  /\bredis:\/\/[^\s"')]+/i,
  /[A-Za-z]:\\(?:Users|Temp|Windows|ProgramData)\\[^\s"')]+/i,
  /\/home\/[^\s"')]+/i,
  /\bat\s+[^\n]+\([^\n]+:\d+:\d+\)/,
  /provider\s+internal/i,
  /secret-axis-evidence/i,
  /memory-secret/i,
  /raw\s+payload/i,
];

export function sanitizeText(value: string): string {
  return value
    .replace(/Bearer\s+[A-Za-z0-9._-]+/gi, '[REDACTED]')
    .replace(/\bredis:\/\/[^\s"')]+/gi, '[REDACTED]')
    .replace(/[A-Za-z]:\\[^\s"')]+/g, '[REDACTED]')
    .replace(/\/(?:home|root|tmp|var|mnt|opt|etc|Users)\/[^\s"')]+/g, '[REDACTED]')
    .replace(/secret-axis-evidence/gi, '[REDACTED]')
    .replace(/memory-secret/gi, '[REDACTED]')
    .replace(/raw\s+payload/gi, '[REDACTED]');
}

export function validateArtifactText(text: string, label: string): void {
  for (const pattern of restrictedPatterns) {
    if (pattern.test(text)) {
      throw new Error(`Restricted artifact content matched ${pattern} in ${label}`);
    }
  }
}

export async function writeEvidenceFile(fileName: string, value: unknown): Promise<string> {
  if (!/^[a-z0-9._-]+\.json$/u.test(fileName)) {
    throw new Error(`Evidence file name is not bounded: ${fileName}`);
  }

  await mkdir(evidenceDirectory, { recursive: true });
  const target = path.join(evidenceDirectory, fileName);
  const relativePath = path.relative(process.cwd(), target).replaceAll(path.sep, '/');
  const text = `${sanitizeText(JSON.stringify(value, null, 2))}\n`;
  validateArtifactText(text, relativePath);
  await writeFile(target, text, 'utf8');
  return relativePath;
}

export async function validateEvidenceDirectory(): Promise<string[]> {
  const files = await collectFiles(evidenceDirectory);
  const relativeFiles: string[] = [];

  for (const file of files) {
    const relative = path.relative(process.cwd(), file).replaceAll(path.sep, '/');
    relativeFiles.push(relative);

    if (path.isAbsolute(relative)) {
      throw new Error(`Artifact path should be relative: ${relative}`);
    }

    if (file.endsWith('.json') || file.endsWith('.md') || file.endsWith('.txt') || file.endsWith('.xml')) {
      const text = await readFile(file, 'utf8');
      validateArtifactText(text, relative);
    }
  }

  return relativeFiles.sort();
}

async function collectFiles(root: string): Promise<string[]> {
  let rootStatus;
  try {
    rootStatus = await stat(root);
  } catch {
    return [];
  }

  if (!rootStatus.isDirectory()) {
    return [];
  }

  const entries = await readdir(root, { withFileTypes: true });
  const files: string[] = [];
  for (const entry of entries) {
    const fullPath = path.join(root, entry.name);
    if (entry.isDirectory()) {
      files.push(...await collectFiles(fullPath));
    } else {
      files.push(fullPath);
    }
  }

  return files;
}
