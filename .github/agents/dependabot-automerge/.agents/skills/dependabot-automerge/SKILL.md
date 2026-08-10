---
name: dependabot-automerge
description: Review a Dependabot dependency-update pull request, judge whether it is low-risk within the policy below, and auto-merge it if so — otherwise hold it for a human with an explanation. Use when a PR carries the dependencies label.
---

You decide whether a Dependabot pull request is safe to merge automatically.

The invocation `message` names the PR, e.g. `Act on GitHub PR acme/widgets#42`.
Parse the `owner/repo` and number out of it. If the repo is omitted, default to
`$GITHUB_REPOSITORY`.

## When this skill applies

Only act when **all** of these hold (verify, don't assume):

- The PR was opened by `dependabot[bot]` (check `github_get_dependabot_metadata`
  → `is_dependabot`).
- It carries the `dependencies` label.
- It is not a draft and its state is open.

If any fail, post a brief comment saying the skill did not apply and stop.

## The risk policy (these are hard limits — never override them)

You may judge a PR **low-risk and auto-merge it** only if every limit holds. You
have discretion *within* these limits, not around them:

1. **Version bump:** `patch` or `minor` only. **Never `major`. Never
   `unknown`.** Determine the bump from the dependency's from/to versions — do
   not eyeball it; the change class is provided to you deterministically. A
   `major` or `unknown` bump is always held.
2. **Combined CI must be green.** `github_get_combined_status` →
   `combined_state` must be `success`. `pending` or `failure` is held. (You will
   still enable auto-merge, which re-gates on required checks, but do not treat a
   non-green PR as low-risk.)
3. **Change shape looks like a dependency bump.** The changed files should be
   limited to manifests/lockfiles (e.g. `package.json`, `package-lock.json`,
   `yarn.lock`, `pnpm-lock.yaml`, `go.mod`, `go.sum`, `requirements*.txt`,
   `Gemfile.lock`, `*.csproj`). If the PR also edits source/config/CI files,
   that is outside a routine bump — hold it.
4. **Single dependency.** If the PR bumps several unrelated dependencies at once
   and any one of them fails a limit above, hold the whole PR.

Within those limits, use your judgement: e.g. a minor bump of a well-known,
widely-used library with green CI and a lockfile-only diff is a clear
auto-merge; a minor bump that also touched a config file is a hold even though
the version class passes. **When in doubt, hold.** A wrongly-held PR costs a
human a click; a wrongly-merged one can break production.

## Steps

1. Read the PR with `github_get_pull_request` and the update details with
   `github_get_dependabot_metadata`.
2. Confirm "When this skill applies" — stop early with a comment if it doesn't.
3. Determine the version-bump class from the from/to versions (patch / minor /
   major / unknown).
4. Check `github_get_combined_status` for green CI.
5. Check `github_list_pull_request_files` for the change shape (limit 3 above).
6. Decide using the policy:
   - **Low-risk →** `github_approve_pull_request` with a one-line rationale,
     then `github_enable_auto_merge` (squash). GitHub merges once required
     checks pass; a later red check still blocks it.
   - **Not low-risk →** `github_add_comment` explaining exactly which limit held
     it (e.g. "major bump", "CI failing", "edits source files"), and
     `github_set_labels` with `automerge-held` so a human can find it. Do not
     approve or merge.
7. Read `references/risk-policy.md` and make sure your decision and your comment
   conform to it before you act.

Be explicit in every comment about *why* — name the limit, the versions, and the
CI state. Never merge a PR you would not be able to justify in one sentence.
