# dependabot-automerge agent (vendored)

This is a vendored copy of the `github-pr-label-actions` Flue agent
([upstream](https://github.com/hjgraca/agentic-sdlc/tree/main/examples/github-pr-label-actions)),
committed in-tree so this repo depends on **no external source repository** at
run time. Tracking issue: #1228.

It is invoked one-shot by
[`.github/workflows/dependabot_automerge.yml`](../../workflows/dependabot_automerge.yml):
on a Dependabot PR labelled `dependencies`, the agent judges whether the change
is low-risk within the policy in
[`.agents/skills/dependabot-automerge/SKILL.md`](.agents/skills/dependabot-automerge/SKILL.md)
(patch/minor only, green CI, manifest/lockfile-only diff). If so it approves and
enables GitHub auto-merge (which still waits for required checks); otherwise it
holds the PR and comments why. It works only through the GitHub API and never
checks out or runs PR code.

## Layout

```
AGENTS.md                                    # agent framing
.agents/skills/dependabot-automerge/         # the policy (edit here to tune risk limits)
src/agents/pr-label-actions.ts               # model + sandbox + tools (pure wiring)
src/tools/github/                            # outbound GitHub tools (@octokit/rest) + tests
```

## Tuning the risk policy

Edit `.agents/skills/dependabot-automerge/SKILL.md` and its
`references/risk-policy.md` — no rebuild. The model judges within the limits
defined there; it cannot widen them.

## Updating this copy

This is a vendored snapshot. To pull upstream changes, re-copy the example from
agentic-sdlc (excluding `node_modules`, `dist`, `.flue`, `.env`) and review the
diff. Local edits to the policy above can be re-applied on top.

## Local check

```bash
npm ci
npm test                              # unit tests for the pure helpers
./node_modules/.bin/flue build --target node
```
