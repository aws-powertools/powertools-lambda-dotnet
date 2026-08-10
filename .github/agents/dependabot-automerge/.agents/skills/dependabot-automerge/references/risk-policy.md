# Dependabot auto-merge — decision checklist

Before approving + enabling auto-merge, confirm every line. Any "no" → hold the
PR and comment why.

## Eligibility
- [ ] Author is `dependabot[bot]`.
- [ ] PR carries the `dependencies` label.
- [ ] PR is open and not a draft.

## Risk limits (hard — never overridden by judgement)
- [ ] Version bump is `patch` or `minor` (NOT `major`, NOT `unknown`).
- [ ] Combined CI status is `success` (not `pending`, not `failure`).
- [ ] Changed files are manifests/lockfiles only — no source, config, or CI edits.
- [ ] If multiple dependencies are bumped, every one passes the limits above.

## Action taken
- [ ] **Auto-merge path:** approved with a one-line rationale, then auto-merge
      (squash) enabled. Comment states the dependency, from→to versions, bump
      class, and that CI is green.
- [ ] **Hold path:** a comment names the exact limit that held it; the
      `automerge-held` label is applied. No approval, no merge.

## Tuning this policy
This file plus the limits in `SKILL.md` are the only place to change behavior —
the agent code does not encode any of it. To make the policy stricter or looser
for your repo, edit here (e.g. restrict to `patch` only, or allow `minor` only
for `devDependencies`). The model judges within whatever limits you set; it
cannot widen them. When unsure, the policy is: **hold, don't merge.**
