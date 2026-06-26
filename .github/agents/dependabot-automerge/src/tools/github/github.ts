import { defineTool } from '@flue/runtime';
import { Octokit } from '@octokit/rest';
import * as v from 'valibot';
import { splitRepo } from './helpers.ts';

/**
 * GitHub tools for a label-driven PR-action agent. Every call is a typed
 * Octokit request over the REST/GraphQL API — the agent reads PR *metadata* and
 * takes PR *actions* (approve, comment, enable auto-merge). It does not check
 * out or run the PR's code. That matters: the workflow runs on
 * `pull_request_target` with a write token, so running untrusted PR contents
 * would be a supply-chain risk. Reading metadata over the API is safe.
 *
 * Auth comes from the environment at runtime — never hardcode tokens:
 *   GITHUB_TOKEN    the Actions-provided token (pull-requests: write,
 *                   contents: write for merge) or a PAT for local runs
 *   GITHUB_API_URL  set by Actions; point it at https://<host>/api/v3 for
 *                   GitHub Enterprise. Octokit defaults to https://api.github.com.
 */
const octokit = new Octokit({
	auth: process.env.GITHUB_TOKEN,
	...(process.env.GITHUB_API_URL ? { baseUrl: process.env.GITHUB_API_URL } : {}),
});

export const getPullRequest = defineTool({
	name: 'github_get_pull_request',
	description:
		'Fetch a pull request (title, body, author, labels, base/head, mergeable state) by repo ("owner/repo") and PR number. Use to read the PR you are acting on.',
	input: v.object({ repo: v.string(), prNumber: v.number() }),
	run: async ({ input }) => {
		try {
			const { data } = await octokit.rest.pulls.get({
				...splitRepo(input.repo),
				pull_number: input.prNumber,
			});
			return JSON.stringify({
				number: data.number,
				title: data.title,
				body: data.body,
				user: data.user?.login,
				labels: data.labels.map((l) => l.name),
				state: data.state,
				draft: data.draft,
				mergeable: data.mergeable,
				mergeable_state: data.mergeable_state,
				base: data.base.ref,
				head: data.head.ref,
				changed_files: data.changed_files,
				additions: data.additions,
				deletions: data.deletions,
				html_url: data.html_url,
			});
		} catch (err) {
			return `GitHub get PR failed: ${String(err)}`;
		}
	},
});

export const listPullRequestFiles = defineTool({
	name: 'github_list_pull_request_files',
	description:
		'List the files changed in a pull request, with per-file additions/deletions. Use to judge the blast radius of a change (e.g. lockfile-only vs. source edits).',
	input: v.object({ repo: v.string(), prNumber: v.number() }),
	run: async ({ input }) => {
		try {
			const { data } = await octokit.rest.pulls.listFiles({
				...splitRepo(input.repo),
				pull_number: input.prNumber,
				per_page: 100,
			});
			return JSON.stringify(
				data.map((f) => ({
					filename: f.filename,
					status: f.status,
					additions: f.additions,
					deletions: f.deletions,
				})),
			);
		} catch (err) {
			return `GitHub list PR files failed: ${String(err)}`;
		}
	},
});

export const getCombinedStatus = defineTool({
	name: 'github_get_combined_status',
	description:
		"Get the combined CI status (success / pending / failure) and individual check runs for a PR's head commit. Use to confirm checks are green before merging.",
	input: v.object({ repo: v.string(), prNumber: v.number() }),
	run: async ({ input }) => {
		try {
			const { owner, repo } = splitRepo(input.repo);
			const { data: pr } = await octokit.rest.pulls.get({
				owner,
				repo,
				pull_number: input.prNumber,
			});
			const ref = pr.head.sha;
			const [status, checks] = await Promise.all([
				octokit.rest.repos.getCombinedStatusForRef({ owner, repo, ref }),
				octokit.rest.checks.listForRef({ owner, repo, ref }),
			]);
			return JSON.stringify({
				combined_state: status.data.state, // success | pending | failure
				statuses: status.data.statuses.map((s) => ({ context: s.context, state: s.state })),
				check_runs: checks.data.check_runs.map((c) => ({
					name: c.name,
					status: c.status, // queued | in_progress | completed
					conclusion: c.conclusion, // success | failure | neutral | ...
				})),
			});
		} catch (err) {
			return `GitHub get combined status failed: ${String(err)}`;
		}
	},
});

export const getDependabotMetadata = defineTool({
	name: 'github_get_dependabot_metadata',
	description:
		'For a Dependabot PR, extract the dependency name, previous and new versions, and author from the PR title. Returns null fields when the PR is not a recognizable Dependabot update. Use to feed the risk policy.',
	input: v.object({ repo: v.string(), prNumber: v.number() }),
	run: async ({ input }) => {
		try {
			const { data } = await octokit.rest.pulls.get({
				...splitRepo(input.repo),
				pull_number: input.prNumber,
			});
			const title = data.title ?? '';
			// Dependabot titles look like:
			//   "Bump lodash from 4.17.20 to 4.17.21"
			//   "chore(deps): bump actions/checkout from 4 to 5"
			const m = /bump\s+(\S+)\s+from\s+(\S+)\s+to\s+(\S+)/i.exec(title);
			return JSON.stringify({
				is_dependabot: data.user?.login === 'dependabot[bot]',
				author: data.user?.login,
				dependency: m?.[1] ?? null,
				from_version: m?.[2] ?? null,
				to_version: m?.[3] ?? null,
				title,
			});
		} catch (err) {
			return `GitHub get dependabot metadata failed: ${String(err)}`;
		}
	},
});

export const approvePullRequest = defineTool({
	name: 'github_approve_pull_request',
	description:
		'Submit an APPROVE review on a PR with a short comment. Use only after the risk policy has confirmed the change is low-risk.',
	input: v.object({ repo: v.string(), prNumber: v.number(), body: v.string() }),
	run: async ({ input }) => {
		try {
			await octokit.rest.pulls.createReview({
				...splitRepo(input.repo),
				pull_number: input.prNumber,
				event: 'APPROVE',
				body: input.body,
			});
			return `Approved ${input.repo}#${input.prNumber}`;
		} catch (err) {
			return `GitHub approve failed: ${String(err)}`;
		}
	},
});

export const enableAutoMerge = defineTool({
	name: 'github_enable_auto_merge',
	description:
		"Enable GitHub auto-merge on a PR (squash by default). GitHub then merges the PR ONLY after all required status checks pass — a red check blocks it. This is the safe merge path; prefer it over an immediate merge. Requires auto-merge to be turned on in the repo's settings.",
	input: v.object({
		repo: v.string(),
		prNumber: v.number(),
		mergeMethod: v.optional(v.picklist(['squash', 'merge', 'rebase'])),
	}),
	run: async ({ input }) => {
		try {
			const { owner, repo } = splitRepo(input.repo);
			// Auto-merge is only exposed via GraphQL. Resolve the PR node id, then
			// call enablePullRequestAutoMerge.
			const { repository } = await octokit.graphql<{
				repository: { pullRequest: { id: string } };
			}>(
				`query($owner:String!,$repo:String!,$num:Int!){
					repository(owner:$owner,name:$repo){ pullRequest(number:$num){ id } }
				}`,
				{ owner, repo, num: input.prNumber },
			);
			const method = (input.mergeMethod ?? 'squash').toUpperCase();
			await octokit.graphql(
				`mutation($id:ID!,$method:PullRequestMergeMethod!){
					enablePullRequestAutoMerge(input:{pullRequestId:$id, mergeMethod:$method}){ clientMutationId }
				}`,
				{ id: repository.pullRequest.id, method },
			);
			return `Auto-merge (${method}) enabled on ${input.repo}#${input.prNumber}; GitHub will merge once required checks pass.`;
		} catch (err) {
			return `GitHub enable auto-merge failed: ${String(err)}`;
		}
	},
});

export const addComment = defineTool({
	name: 'github_add_comment',
	description:
		'Add a comment to a PR. Use to explain a decision — especially when NOT auto-merging, so a human knows why it was held for review. Body is GitHub-flavored Markdown.',
	input: v.object({ repo: v.string(), prNumber: v.number(), body: v.string() }),
	run: async ({ input }) => {
		try {
			const { data } = await octokit.rest.issues.createComment({
				...splitRepo(input.repo),
				issue_number: input.prNumber,
				body: input.body,
			});
			return `Comment ${data.id} added to ${input.repo}#${input.prNumber}`;
		} catch (err) {
			return `GitHub add comment failed: ${String(err)}`;
		}
	},
});

export const setLabels = defineTool({
	name: 'github_set_labels',
	description:
		'Add labels to a PR (e.g. "automerge-held" when a change is not low-risk). Existing labels are kept.',
	input: v.object({ repo: v.string(), prNumber: v.number(), labels: v.array(v.string()) }),
	run: async ({ input }) => {
		try {
			await octokit.rest.issues.addLabels({
				...splitRepo(input.repo),
				issue_number: input.prNumber,
				labels: input.labels,
			});
			return `Labels ${input.labels.join(', ')} added to ${input.repo}#${input.prNumber}`;
		} catch (err) {
			return `GitHub set labels failed: ${String(err)}`;
		}
	},
});
