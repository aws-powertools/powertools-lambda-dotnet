/**
 * Pure helpers for the GitHub tools — no Octokit client or env state, so they
 * are unit-testable in isolation (see helpers.test.ts). They live apart from
 * github.ts because the agent builds its tool list with
 * `Object.values(githubTools)`; any non-tool export from that module would be
 * swept in as a bogus tool.
 */

/**
 * Split an "owner/repo" string into Octokit's { owner, repo }.
 *
 * Validates strictly: the value comes from the skill parsing the run input, so
 * a pasted URL, an extra path segment, or a missing slash are realistic.
 * Without this guard those silently yield a wrong or `undefined` coordinate and
 * every tool fails deep inside Octokit with an opaque error. We throw a clear
 * message instead; each tool's `run` catch returns it to the model.
 */
export function splitRepo(repo: string): { owner: string; repo: string } {
	const parts = repo.split('/');
	if (parts.length !== 2 || !parts[0] || !parts[1]) {
		throw new Error(
			`Invalid repo "${repo}": expected "owner/repo" (exactly one slash, no empty segments).`,
		);
	}
	return { owner: parts[0], repo: parts[1] };
}

/**
 * Classify a semver bump from old → new version into 'major' | 'minor' |
 * 'patch' | 'unknown'. The skill's risk policy keys off this: a model can be
 * confidently wrong about which segment changed, so we compute it
 * deterministically and let the skill decide what each class may do.
 *
 * Leading 'v' and pre-release/build metadata are tolerated. Anything that does
 * not parse as two comparable x.y.z triples is 'unknown' (the policy treats
 * 'unknown' as NOT low-risk).
 */
export function semverBump(from: string, to: string): 'major' | 'minor' | 'patch' | 'unknown' {
	const parse = (s: string): readonly [number, number, number] | null => {
		const m = /^v?(\d+)\.(\d+)\.(\d+)/.exec(s.trim());
		if (!m) return null;
		return [Number(m[1]), Number(m[2]), Number(m[3])] as const;
	};
	const a = parse(from);
	const b = parse(to);
	if (!a || !b) return 'unknown';
	if (b[0] !== a[0]) return 'major';
	if (b[1] !== a[1]) return 'minor';
	if (b[2] !== a[2]) return 'patch';
	return 'patch'; // identical core version (only pre-release/build differs)
}
