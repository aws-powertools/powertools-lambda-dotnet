import { defineConfig } from '@flue/cli/config';

/**
 * Build-time config only (target, root, output). Provider/model
 * registration is a runtime concern and lives in code that reads
 * process.env — never hardcode API keys here.
 *
 * `target: "node"` is set so `flue run` / `flue dev` work without the flag.
 * Switch to "cloudflare" if you deploy there instead.
 */
export default defineConfig({ target: 'node' });
