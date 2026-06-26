import assert from 'node:assert/strict';
import { test } from 'node:test';
import { semverBump, splitRepo } from './helpers.ts';

// Run with: npm test  (node --test, no extra deps — see package.json).

test('splitRepo accepts a well-formed owner/repo', () => {
	assert.deepEqual(splitRepo('octocat/hello-world'), {
		owner: 'octocat',
		repo: 'hello-world',
	});
});

test('splitRepo rejects malformed input with a clear error', () => {
	for (const bad of ['owner/repo/extra', 'https://github.com/owner/repo', 'noslash', 'owner/', '/repo', '']) {
		assert.throws(() => splitRepo(bad), /Invalid repo/, `expected "${bad}" rejected`);
	}
});

test('semverBump classifies major/minor/patch', () => {
	assert.equal(semverBump('4.17.20', '5.0.0'), 'major');
	assert.equal(semverBump('4.17.20', '4.18.0'), 'minor');
	assert.equal(semverBump('4.17.20', '4.17.21'), 'patch');
});

test('semverBump tolerates a leading v and pre-release metadata', () => {
	assert.equal(semverBump('v1.2.3', 'v1.2.4'), 'patch');
	assert.equal(semverBump('1.2.3', '2.0.0-rc.1'), 'major');
});

test('semverBump returns unknown for unparseable versions', () => {
	// Dependabot sometimes bumps coarse versions (e.g. GitHub Actions "4" -> "5")
	// that are not full x.y.z — the policy treats unknown as NOT low-risk.
	assert.equal(semverBump('4', '5'), 'unknown');
	assert.equal(semverBump('1.2.3', 'latest'), 'unknown');
});
