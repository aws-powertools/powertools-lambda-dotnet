import { defineAgent } from '@flue/runtime';
import { local } from '@flue/runtime/node';
import * as githubTools from '../tools/github/github.ts';

// Skills and AGENTS.md are discovered by Flue at init time from the sandbox cwd:
// `<cwd>/.agents/skills/<name>/SKILL.md` and `<cwd>/AGENTS.md`. No imports, no
// instructions field — the agent's framing lives in AGENTS.md, and each
// label-driven procedure lives in its own skill.
//
// This is a GENERIC label-action agent: a label applied to a PR triggers a
// one-shot GitHub Actions run, and the active skill decides what to do. The
// shipped example skill is dependabot-automerge, but the agent itself is
// action-agnostic — add a skill keyed to a different label to add a behavior,
// without touching this file.
//
// There is no channel: this runs one-shot in GitHub Actions. The workflow is
// the trigger; the PR ref arrives as input; `flue run pr-label-actions` is the
// entry point. (Contrast triage-jira-k8s, which uses a webhook channel, and
// @flue/github's createGitHubChannel — the always-on path we did not use here.)
const cwd = process.env.SKILLS_DIR ?? process.cwd();

export default defineAgent(() => ({
	model: 'amazon-bedrock/us.anthropic.claude-sonnet-4-6',
	sandbox: local({ cwd }),
	tools: Object.values(githubTools),
}));
