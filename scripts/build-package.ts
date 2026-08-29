#!/usr/bin/env bun
/**
 * build-package.ts — Build a local .nupkg for ai-sheepdog.
 *
 * Usage:
 *   bun scripts/build-package.ts [--rid <rid>] [--configuration <config>]
 *
 * Defaults: current RID, Release configuration.
 * Output: .nupkg/ai-sheepdog.<version>.nupkg
 */

import { $ } from "bun";

const args = process.argv.slice(2);

let rid = "";
let configuration = "Release";

for (let i = 0; i < args.length; i++) {
  if (args[i] === "--rid" && args[i + 1]) {
    rid = args[++i];
  } else if (args[i] === "--configuration" && args[i + 1]) {
    configuration = args[++i];
  }
}

// Read version from VERSION file
const version = (await Bun.file("VERSION").text()).trim();
console.log(`Building ai-sheepdog ${version} (${configuration}${rid ? `, RID=${rid}` : ""})...`);

// Ensure output directory exists
await $`mkdir -p .nupkg-local`;

// Build the pack command
const packArgs = [
  "dotnet", "pack",
  "src/AiSheepdog/AiSheepdog.csproj",
  "-c", configuration,
  "-o", ".nupkg-local",
  "--nologo",
];

if (rid) {
  packArgs.push("-p", `RuntimeIdentifiers=${rid}`);
}

const proc = Bun.spawn(packArgs, { stdout: "inherit", stderr: "inherit" });
const exitCode = await proc.exited;

if (exitCode !== 0) {
  console.error(`Pack failed with exit code ${exitCode}`);
  process.exit(exitCode);
}

console.log(`\nPackage written to .nupkg-local/ai-sheepdog.${version}.nupkg`);
console.log(`\nInstall locally:`);
console.log(`  dotnet tool install -g ai-sheepdog --add-source .nupkg-local`);
