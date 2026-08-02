import { constants as fsConstants } from "node:fs";
import { access, mkdtemp, readFile, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { createRequire } from "node:module";
import { fileURLToPath, pathToFileURL } from "node:url";
import { isDeepStrictEqual } from "node:util";

import { cosmiconfig } from "cosmiconfig";

const expectedPlugins = [
  "@semantic-release/commit-analyzer",
  "@semantic-release/release-notes-generator",
  "@semantic-release/exec",
  "@semantic-release/github",
];

const expectedConfiguration = {
  branches: ["main"],
  tagFormat: "v${version}",
  plugins: [
    "@semantic-release/commit-analyzer",
    "@semantic-release/release-notes-generator",
    [
      "@semantic-release/exec",
      {
        verifyReleaseCmd: "pwsh -NoLogo -NoProfile -File ./tools/verify-container-registry.ps1",
        prepareCmd:
          "pwsh -NoLogo -NoProfile -File ./tools/pack-release.ps1 -Version ${nextRelease.version} -OutputDirectory ./artifacts/packages/release",
        publishCmd: "pwsh -NoLogo -NoProfile -File ./tools/publish-release.ps1 -Version ${nextRelease.version}",
      },
    ],
    [
      "@semantic-release/github",
      {
        assets: [
          "artifacts/packages/release/*.nupkg",
          "artifacts/deployment/hexalith-memories-production.yaml",
        ],
      },
    ],
  ],
};

const lifecycleKeys = [
  "addChannel",
  "analyzeCommits",
  "fail",
  "generateNotes",
  "prepare",
  "publish",
  "success",
  "verifyConditions",
  "verifyRelease",
];

const alternateReleaseConfigPaths = [
  ".releaserc",
  ".releaserc.yaml",
  ".releaserc.yml",
  ".releaserc.js",
  ".releaserc.ts",
  ".releaserc.cjs",
  ".releaserc.mjs",
  ".config/releaserc",
  ".config/releaserc.json",
  ".config/releaserc.yaml",
  ".config/releaserc.yml",
  ".config/releaserc.js",
  ".config/releaserc.ts",
  ".config/releaserc.cjs",
  ".config/releaserc.mjs",
  "release.config.js",
  "release.config.ts",
  "release.config.cjs",
  "release.config.mjs",
];

const cosmiconfigMetaPaths = [
  "package.yaml",
  ".config/config.json",
  ".config/config.yaml",
  ".config/config.yml",
  ".config/config.js",
  ".config/config.ts",
  ".config/config.cjs",
  ".config/config.mjs",
];

const scriptPath = fileURLToPath(import.meta.url);
const repositoryRoot = resolve(dirname(scriptPath), "..");

function ensure(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

async function pathExists(path) {
  try {
    await access(path, fsConstants.F_OK);
    return true;
  } catch (error) {
    if (error?.code === "ENOENT") {
      return false;
    }

    throw error;
  }
}

async function readJson(path) {
  return JSON.parse(await readFile(path, "utf8"));
}

function getPluginName(plugin) {
  if (typeof plugin === "string") {
    return plugin;
  }

  if (Array.isArray(plugin) && (plugin.length === 1 || plugin.length === 2) && typeof plugin[0] === "string") {
    return plugin[0];
  }

  throw new Error("Every semantic-release plugin must be a package name or [package name, options] tuple.");
}

function ensureExactList(actual, expected, description) {
  ensure(
    JSON.stringify(actual) === JSON.stringify(expected),
    `${description} must be exactly ${JSON.stringify(expected)}; found ${JSON.stringify(actual)}.`,
  );
}

function ensureExactValue(actual, expected, description) {
  ensure(
    isDeepStrictEqual(actual, expected),
    `${description} must be exactly ${JSON.stringify(expected)}; found ${JSON.stringify(actual)}.`,
  );
}

async function validateActiveConfiguration(root) {
  const packagePath = join(root, "package.json");
  const canonicalConfigPath = join(root, ".releaserc.json");
  const packageManifest = await readJson(packagePath);

  ensure(!Object.hasOwn(packageManifest, "release"), "package.json must not define a shadow 'release' configuration.");
  ensure(!Object.hasOwn(packageManifest, "cosmiconfig"), "package.json must not redefine cosmiconfig search precedence.");

  for (const relativePath of [...alternateReleaseConfigPaths, ...cosmiconfigMetaPaths]) {
    ensure(!(await pathExists(join(root, relativePath))), `Shadow configuration '${relativePath}' must not exist.`);
  }

  ensure(await pathExists(canonicalConfigPath), "The canonical .releaserc.json configuration is missing.");
  const rawConfiguration = await readJson(canonicalConfigPath);
  ensure(!Object.hasOwn(rawConfiguration, "$import"), ".releaserc.json must not import shadow configuration.");
  ensure(!Object.hasOwn(rawConfiguration, "extends"), ".releaserc.json must not extend shadow configuration.");

  for (const lifecycleKey of lifecycleKeys) {
    ensure(
      !Object.hasOwn(rawConfiguration, lifecycleKey),
      `.releaserc.json must not define top-level '${lifecycleKey}' lifecycle configuration that shadows plugins.`,
    );
  }

  ensureExactValue(rawConfiguration.branches, expectedConfiguration.branches, "The semantic-release branches");
  ensure(rawConfiguration.tagFormat === expectedConfiguration.tagFormat, "The semantic-release tagFormat must remain 'v${version}'.");
  ensure(Array.isArray(rawConfiguration.plugins), ".releaserc.json must define an explicit plugins array.");
  const configuredPlugins = rawConfiguration.plugins.map(getPluginName);
  ensureExactList(configuredPlugins, expectedPlugins, "The active semantic-release plugin allowlist");
  ensure(
    !configuredPlugins.includes("@semantic-release/npm"),
    "The npm publication plugin must not be part of the active release lifecycle.",
  );
  ensureExactValue(rawConfiguration.plugins[2], expectedConfiguration.plugins[2], "The @semantic-release/exec command contract");
  ensureExactValue(rawConfiguration.plugins[3], expectedConfiguration.plugins[3], "The @semantic-release/github asset contract");
  ensureExactValue(rawConfiguration, expectedConfiguration, "The complete semantic-release configuration");

  // Load the canonical file through the same configuration library semantic-release uses. The
  // search surface is restricted because every competing default search location was rejected
  // above, which makes this deterministic and prevents executable shadow configuration.
  const explorer = cosmiconfig("release", {
    searchPlaces: [".releaserc.json"],
    stopDir: root,
  });
  const loaded = await explorer.load(canonicalConfigPath);
  ensure(loaded?.filepath === canonicalConfigPath, `Expected cosmiconfig to load '${canonicalConfigPath}'.`);
  ensureExactValue(loaded.config, expectedConfiguration, "The loaded semantic-release configuration");
  ensureExactList(loaded.config.plugins.map(getPluginName), expectedPlugins, "The loaded semantic-release plugin allowlist");

  return { configuration: loaded.config, packageManifest };
}

async function readContainingPackage(entryPath) {
  let current = dirname(entryPath);
  while (current !== dirname(current)) {
    const manifestPath = join(current, "package.json");
    if (await pathExists(manifestPath)) {
      return { manifest: await readJson(manifestPath), manifestPath };
    }

    current = dirname(current);
  }

  throw new Error(`Could not locate the package manifest containing '${entryPath}'.`);
}

async function loadInstalledPluginGraph(root, configuration, packageManifest) {
  ensure(
    packageManifest.devDependencies?.["semantic-release"] === "25.0.8",
    "semantic-release must remain pinned exactly to 25.0.8 while the verifier uses its internal plugin loader.",
  );
  ensure(
    packageManifest.devDependencies?.cosmiconfig === "9.0.2",
    "cosmiconfig must remain a direct dependency pinned exactly to 9.0.2.",
  );
  ensure(
    packageManifest.scripts?.["verify:semantic-release-config"] ===
      "node ./tools/verify-semantic-release-config.mjs && node ./tools/verify-semantic-release-config.mjs --self-test",
    "package.json must expose the canonical combined semantic-release verification command.",
  );

  const repositoryRequire = createRequire(join(root, "package.json"));
  const semanticReleaseEntry = repositoryRequire.resolve("semantic-release");
  const semanticReleaseRoot = dirname(semanticReleaseEntry);
  const semanticReleaseManifest = await readJson(join(semanticReleaseRoot, "package.json"));
  ensure(
    semanticReleaseManifest.name === "semantic-release" && semanticReleaseManifest.version === "25.0.8",
    `Installed semantic-release must be 25.0.8; found '${semanticReleaseManifest.name}@${semanticReleaseManifest.version}'.`,
  );

  const semanticReleaseRequire = createRequire(join(semanticReleaseRoot, "package.json"));
  const npmAliasEntry = semanticReleaseRequire.resolve("@semantic-release/npm");
  const npmAliasPackage = await readContainingPackage(npmAliasEntry);
  ensure(
    npmAliasPackage.manifest.name === "@semantic-release/error" && npmAliasPackage.manifest.version === "4.0.0",
    "semantic-release's @semantic-release/npm dependency must resolve to the official @semantic-release/error@4.0.0 alias.",
  );

  const pluginLoaderUrl = pathToFileURL(join(semanticReleaseRoot, "lib", "plugins", "index.js"));
  const { default: loadPlugins } = await import(pluginLoaderUrl.href);
  const logger = Object.freeze({ error() {}, log() {}, success() {} });
  const loadedLifecycle = await loadPlugins(
    {
      cwd: root,
      env: Object.freeze({}),
      logger,
      options: {
        ...configuration,
        repositoryUrl: "https://offline.invalid/Hexalith.Memories.git",
      },
    },
    {},
  );

  ensureExactList(Object.keys(loadedLifecycle).sort(), [...lifecycleKeys].sort(), "The installed plugin lifecycle");
}

async function verifyRepository(root) {
  const { configuration, packageManifest } = await validateActiveConfiguration(root);
  await loadInstalledPluginGraph(root, configuration, packageManifest);
}

async function expectStaticFailure(root, description, mutate, expectedMessage) {
  const fixture = await mkdtemp(join(tmpdir(), "semantic-release-config-verifier-"));
  try {
    const packageManifest = { name: "offline-verifier-fixture", private: true };
    const configuration = JSON.parse(await readFile(join(root, ".releaserc.json"), "utf8"));
    await mutate({ configuration, fixture, packageManifest });
    await writeFile(join(fixture, "package.json"), `${JSON.stringify(packageManifest, null, 2)}\n`, "utf8");
    await writeFile(join(fixture, ".releaserc.json"), `${JSON.stringify(configuration, null, 2)}\n`, "utf8");

    let failure;
    try {
      await validateActiveConfiguration(fixture);
    } catch (error) {
      failure = error;
    }

    ensure(failure instanceof Error, `Negative self-test '${description}' unexpectedly passed.`);
    ensure(
      failure.message.includes(expectedMessage),
      `Negative self-test '${description}' failed for the wrong reason: ${failure.message}`,
    );
  } finally {
    await rm(fixture, { force: true, recursive: true });
  }
}

async function runSelfTests(root) {
  await expectStaticFailure(
    root,
    "package release property",
    async ({ packageManifest }) => {
      packageManifest.release = { plugins: expectedPlugins };
    },
    "shadow 'release'",
  );
  await expectStaticFailure(
    root,
    "alternate configuration file",
    async ({ fixture }) => {
      await writeFile(join(fixture, ".releaserc.yml"), "plugins: []\n", "utf8");
    },
    "Shadow configuration '.releaserc.yml'",
  );
  await expectStaticFailure(
    root,
    "extends configuration",
    async ({ configuration }) => {
      configuration.extends = "./shadow-config.mjs";
    },
    "must not extend shadow configuration",
  );
  await expectStaticFailure(
    root,
    "top-level lifecycle shadow",
    async ({ configuration }) => {
      configuration.publish = "@semantic-release/npm";
    },
    "top-level 'publish'",
  );
  await expectStaticFailure(
    root,
    "implicit default plugins",
    async ({ configuration }) => {
      delete configuration.plugins;
    },
    "explicit plugins array",
  );
  await expectStaticFailure(
    root,
    "npm publication plugin",
    async ({ configuration }) => {
      configuration.plugins.push("@semantic-release/npm");
    },
    "active semantic-release plugin allowlist",
  );
  await expectStaticFailure(
    root,
    "release branch drift",
    async ({ configuration }) => {
      configuration.branches = ["next"];
    },
    "semantic-release branches",
  );
  await expectStaticFailure(
    root,
    "release tag format drift",
    async ({ configuration }) => {
      configuration.tagFormat = "release-${version}";
    },
    "tagFormat",
  );
  await expectStaticFailure(
    root,
    "exec command drift",
    async ({ configuration }) => {
      configuration.plugins[2][1].publishCmd = "pwsh ./tools/publish-release.ps1";
    },
    "@semantic-release/exec command contract",
  );
  await expectStaticFailure(
    root,
    "GitHub asset drift",
    async ({ configuration }) => {
      configuration.plugins[3][1].assets.pop();
    },
    "@semantic-release/github asset contract",
  );
}

async function main() {
  const arguments_ = process.argv.slice(2);
  if (arguments_.length === 0) {
    await verifyRepository(repositoryRoot);
    console.log("Verified semantic-release 25.0.8 and the explicit four-plugin lifecycle offline.");
    return;
  }

  if (arguments_.length === 1 && arguments_[0] === "--self-test") {
    await runSelfTests(repositoryRoot);
    console.log("Verified semantic-release configuration fail-closed negative cases.");
    return;
  }

  throw new Error(`Unsupported arguments: ${arguments_.join(" ")}`);
}

const invokedPath = process.argv[1] ? pathToFileURL(resolve(process.argv[1])).href : undefined;
if (invokedPath === import.meta.url) {
  main().catch((error) => {
    console.error(`Semantic-release configuration verification failed: ${error.message}`);
    process.exitCode = 1;
  });
}

export { runSelfTests, validateActiveConfiguration, verifyRepository };
