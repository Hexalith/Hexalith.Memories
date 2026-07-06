import { readdir, readFile, stat } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';

// Keep this list in lockstep with `helpers/artifacts.ts` restrictedPatterns/sanitizeText.
const textPatterns = [
  /Bearer\s+[A-Za-z0-9._-]+/i,
  /\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/,
  /\bredis:\/\/[^\s"')]+/i,
  /[A-Za-z]:\\[^\s"')]+/i,
  /\/(?:home|root|tmp|var|mnt|opt|etc|Users)\/[^\s"')]+/i,
  /\bat\s+[^\n]+\([^\n]+:\d+:\d+\)/,
  /\bat\s+[^\n(]+\([^\n]*\)\s+in\s+[^\n]+:line\s+\d+/i,
  /provider\s+internal/i,
  /secret-axis-evidence/i,
  /memory-secret/i,
  /raw\s+payload/i,
];

// AC4: the manual assistive-technology checklist must name every disposition field.
// A dropped or blank field is a fail-closed evidence gap, not a silent pass.
const manualChecklistRequiredFields = [
  'workflow',
  'viewport',
  'browser',
  'os',
  'method',
  'tester',
  'date',
  'result',
  'defects',
  'severity',
  'owner',
  'waiverState',
  'releaseDisposition',
];

const textExtensions = new Set(['.json', '.md', '.txt', '.xml']);
const requiredEvidenceFiles = [
  'test-results/evidence/artifact-summary.json',
  'test-results/evidence/axe-summary.json',
  'test-results/evidence/copied-text-summary.json',
  'test-results/evidence/evidence-cockpit.png',
  'test-results/evidence/manual-at-checklist.json',
  'test-results/evidence/media-layout-summary.json',
  'test-results/evidence/route-metadata.json',
  'test-results/evidence/trace-policy.json',
  '../../_bmad-output/implementation-artifacts/tests/test-summary-17-7-browser-at-gap-closure.md',
];
const allowedNonTextArtifacts = new Set(['test-results/evidence/evidence-cockpit.png']);
const evidenceRoot = path.resolve('test-results', 'evidence');
const bmadSummary = path.resolve(
  '..',
  '..',
  '_bmad-output',
  'implementation-artifacts',
  'tests',
  'test-summary-17-7-browser-at-gap-closure.md');

const files = [
  ...await collectFiles(evidenceRoot),
  ...await existingFile(bmadSummary),
];

if (files.length === 0) {
  throw new Error('No evidence artifacts found to validate.');
}

const relativeFiles = files.map((file) => path.relative(process.cwd(), file).replaceAll(path.sep, '/'));
for (const required of requiredEvidenceFiles) {
  if (!relativeFiles.includes(required)) {
    throw new Error(`Required evidence artifact is missing: ${required}`);
  }
}

for (const file of files) {
  const relative = path.relative(process.cwd(), file).replaceAll(path.sep, '/');
  if (path.isAbsolute(relative) || relative.startsWith('/')) {
    throw new Error(`Evidence path is not relative: ${relative}`);
  }

  if (!isBounded(relative)) {
    throw new Error(`Evidence path is outside bounded artifact roots: ${relative}`);
  }

  if (!textExtensions.has(path.extname(file))) {
    if (!allowedNonTextArtifacts.has(relative)) {
      throw new Error(`Non-text artifact requires an explicit redaction policy: ${relative}`);
    }

    continue;
  }

  const text = await readFile(file, 'utf8');
  for (const pattern of textPatterns) {
    if (pattern.test(text)) {
      throw new Error(`Restricted artifact content matched ${pattern} in ${relative}`);
    }
  }
}

await validateManualChecklistSchema();

console.log(`Validated ${files.length} bounded evidence artifacts.`);

async function validateManualChecklistSchema() {
  const checklistPath = path.resolve(evidenceRoot, 'manual-at-checklist.json');
  let raw;
  try {
    raw = await readFile(checklistPath, 'utf8');
  } catch {
    throw new Error('Required evidence artifact is missing: test-results/evidence/manual-at-checklist.json');
  }

  let checklist;
  try {
    checklist = JSON.parse(raw);
  } catch (error) {
    throw new Error(`Manual AT checklist is not valid JSON: ${error.message}`);
  }

  for (const field of manualChecklistRequiredFields) {
    if (!(field in checklist)) {
      throw new Error(`Manual AT checklist is missing required AC4 field: ${field}`);
    }

    const value = checklist[field];
    if (field === 'defects') {
      if (!Array.isArray(value)) {
        throw new Error('Manual AT checklist field "defects" must be an array.');
      }

      continue;
    }

    if (typeof value !== 'string' || value.trim().length === 0) {
      throw new Error(`Manual AT checklist field "${field}" must be a non-empty string.`);
    }
  }
}

function isBounded(relative) {
  return relative.startsWith('test-results/evidence/')
    || relative.startsWith('../../_bmad-output/implementation-artifacts/tests/test-summary-17-7-');
}

async function collectFiles(root) {
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
  const found = [];
  for (const entry of entries) {
    const fullPath = path.join(root, entry.name);
    if (entry.isDirectory()) {
      found.push(...await collectFiles(fullPath));
    } else {
      found.push(fullPath);
    }
  }

  return found;
}

async function existingFile(file) {
  try {
    const fileStatus = await stat(file);
    return fileStatus.isFile() ? [file] : [];
  } catch {
    return [];
  }
}
