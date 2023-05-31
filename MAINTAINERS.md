<!--  markdownlint-disable MD043 -->
## Table of contents <!-- omit in toc -->

- [Overview](#overview)
- [Current Maintainers](#current-maintainers)
- [Emeritus](#emeritus)
- [Labels](#labels)
- [Maintainer Responsibilities](#maintainer-responsibilities)
  - [Uphold Code of Conduct](#uphold-code-of-conduct)
  - [Prioritize Security](#prioritize-security)
  - [Review Pull Requests](#review-pull-requests)
  - [Triage New Issues](#triage-new-issues)
  - [Triage Bug Reports](#triage-bug-reports)
  - [Triage RFCs](#triage-rfcs)
  - [Releasing a new version](#releasing-a-new-version)
    - [Changelog generation](#changelog-generation)
    - [Bumping the version](#bumping-the-version)
    - [Drafting release notes](#drafting-release-notes)
  - [Run end to end tests](#run-end-to-end-tests)
  - [Releasing a documentation hotfix](#releasing-a-documentation-hotfix)
  - [Maintain Overall Health of the Repo](#maintain-overall-health-of-the-repo)
  - [Manage Roadmap](#manage-roadmap)
  - [Add Continuous Integration Checks](#add-continuous-integration-checks)
  - [Negative Impact on the Project](#negative-impact-on-the-project)
  - [Becoming a maintainer](#becoming-a-maintainer)
- [Common scenarios](#common-scenarios)
  - [Contribution is stuck](#contribution-is-stuck)
  - [Insufficient feedback or information](#insufficient-feedback-or-information)
  - [Crediting contributions](#crediting-contributions)
  - [Is that a bug?](#is-that-a-bug)
  - [Mentoring contributions](#mentoring-contributions)
  - [Long running issues or PRs](#long-running-issues-or-prs)

## Overview

> **Please treat this content as a living document.**

This is document explains who the maintainers are (see below), what they do in this repo, and how they should be doing it. If you're interested in contributing, see [CONTRIBUTING](CONTRIBUTING.md).

## Current Maintainers

| Maintainer        | GitHub ID                                     | Affiliation |
| ----------------- | --------------------------------------------- | ----------- |
| Stephen Liedig    | [sliedig](https://github.com/sliedig)         | Amazon      |
| Amir Khairalomoum | [amirkaws](https://github.com/amirkaws)       | Amazon      |

## Emeritus

Previous active maintainers who contributed to this project.

| Maintainer        | GitHub ID                                       | Affiliation |
| ----------------- | ----------------------------------------------- | ----------- |
| Tiago Barbosa     | [t1agob](https://github.com/t1agob)             | Music Tribe |

## Labels

These are the most common labels used by maintainers to triage issues, pull requests (PR), and for project management:

| Label                  | Usage                                                            | Notes                                                           |
| ---------------------- | ---------------------------------------------------------------- | --------------------------------------------------------------- |
| triage                 | New issues that require maintainers review                       | Issue template                                                  |
| bug                    | Unexpected, reproducible and unintended software behavior        | PR/Release automation; Doc snippets are excluded;               |
| not-a-bug              | New and existing bug reports incorrectly submitted as bug        | Analytics                                                       |
| documentation          | Documentation improvements                                       | PR/Release automation; Doc additions, fixes, etc.;              |
| feature-request        | New or enhancements to existing features                         | Issue template                                                  |
| RFC                    | Technical design documents related to a feature request          | Issue template                                                  |
| bug-upstream           | Bug caused by upstream dependency                                |                                                                 |
| help wanted            | Tasks you want help from anyone to move forward                  | Bandwidth, complex topics, etc.                                 |
| need-customer-feedback | Tasks that need more feedback before proceeding                  | 80/20% rule, uncertain, etc.                                    |
| need-more-information  | Missing information before making any calls                      |                                                                 |
| need-documentation     | PR is missing or has incomplete documentation                    |                                                                 |
| need-issue             | PR is missing a related issue for tracking change                | Needs to be automated                                           |
| pending-release        | Merged changes that will be available soon                       | Release automation auto-closes/notifies it                      |
| revisit-in-3-months    | Blocked issues/PRs that need to be revisited                     | Often related to `need-customer-feedback`, prioritization, etc. |
| breaking-change        | Changes that will cause customer impact and need careful triage  |                                                                 |
| do-not-merge           | PRs that are blocked for varying reasons                         | Timeline is uncertain                                           |
| tests                  | PRs that add or change tests                                     | PR automation                                                   |
| `area/<utility>`       | PRs related to a Powertools utility, e.g. `logging`, `tracing`   | PR automation                                                   |
| feature                | New features or minor changes                                    | PR/Release automation                                           |
| dependencies           | Changes that touch dependencies, e.g. Dependabot, etc.           | PR/ automation                                                  |
| github-actions         | Changes in GitHub workflows                                      | PR automation                                                   |
| github-templates       | Changes in GitHub issue/PR templates                             | PR automation                                                   |
| internal               | Changes in governance and chores (linting setup, baseline, etc.) | PR automation                                                   |

## Maintainer Responsibilities

Maintainers are active and visible members of the community, and have [maintain-level permissions on a repository](https://docs.github.com/en/organizations/managing-access-to-your-organizations-repositories/repository-permission-levels-for-an-organization). Use those privileges to serve the community and evolve code as follows.

Be aware of recurring ambiguous situations and [document them](#common-scenarios) to help your fellow maintainers.

### Uphold Code of Conduct

Model the behaviour set forward by the [Code of Conduct](CODE_OF_CONDUCT.md) and raise any violations to other maintainers and admins. There could be unusual circumstances where inappropriate behaviour does not immediately fall within the [Code of Conduct](CODE_OF_CONDUCT.md).

These might be nuanced and should be handled with extra care - when in doubt, do not engage and reach out to other maintainers and admins.

### Prioritize Security

Security is your number one priority. Maintainer's Github keys must be password protected securely and any reported security vulnerabilities are addressed before features or bugs.

Note that this repository is monitored and supported 24/7 by Amazon Security, see [Reporting a Vulnerability](SECURITY.md) for details.

### Review Pull Requests

Review pull requests regularly, comment, suggest, reject, merge and close. Accept only high quality pull-requests. Provide code reviews and guidance on incoming pull requests.

PRs are [labeled](#labels) based on file changes and semantic title. Pay attention to whether labels reflect the current state of the PR and correct accordingly.

Use and enforce [semantic versioning](https://semver.org/) pull request titles, as these will be used for [CHANGELOG](CHANGELOG.md) and [Release notes](https://github.com/aws-powertools/lambda-dotnet/releases) - make sure they communicate their intent at the human level.

> TODO: This is an area we want to automate using the new GitHub GraphQL API.

For issues linked to a PR, make sure `pending-release` label is applied to them when merging. [Upon release](#releasing-a-new-version), these issues will be notified which release version contains their change.

See [Common scenarios](#common-scenarios) section for additional guidance.

### Triage New Issues

Manage [labels](#labels), review issues regularly, and create new labels as needed by the project. Remove `triage` label when you're able to confirm the validity of a request, a bug can be reproduced, etc. Give priority to the original author for implementation, unless it is a sensitive task that is best handled by maintainers.

> TODO: This is an area we want to automate using the new GitHub GraphQL API.

Make sure issues are assigned to our [board of activities](https://github.com/orgs/awslabs/projects/51/) and have the right [status](https://awslabs.github.io/aws-lambda-powertools-dotnet/latest/roadmap/#roadmap-status-definition).

Use our [labels](#labels) to signal good first issues to new community members, and to set expectation that this might need additional feedback from the author, other customers, experienced community members and/or maintainers.

Be aware of [casual contributors](https://opensource.com/article/17/10/managing-casual-contributors) and recurring contributors. Provide the experience and attention you wish you had if you were starting in open source.

See [Common scenarios](#common-scenarios) section for additional guidance.

### Triage Bug Reports

Be familiar with [our definition of bug](#is-that-a-bug). If it's not a bug, you can close it or adjust its title and labels - always communicate the reason accordingly.

For bugs caused by upstream dependencies, replace `bug` with `bug-upstream` label. Ask the author whether they'd like to raise the issue upstream or if they prefer us to do so.

Assess the impact and make the call on whether we need an emergency release. Contact other [maintainers](#current-maintainers) when in doubt.

See [Common scenarios](#common-scenarios) section for additional guidance.

### Releasing a new version

Releases mare managed by maintainers and follow a rigorous internal release process.

Make sure you are using the `develop` branch and it is up to date with the origin.

### Maintain Overall Health of the Repo

> TODO: Coordinate renaming `develop` to `main`

Keep the `develop` branch at production quality at all times. Backport features as needed. Cut release branches and tags to enable future patches.

### Manage Roadmap

See [Roadmap section](https://awslabs.github.io/aws-lambda-powertools-dotnet/roadmap/)

Ensure the repo highlights features that should be elevated to the project roadmap. Be clear about the feature’s status, priority, target version, and whether or not it should be elevated to the roadmap.

### Add Continuous Integration Checks

Add integration checks that validate pull requests and pushes to ease the burden on Pull Request reviewers. Continuously revisit areas of improvement to reduce operational burden in all parties involved.

### Negative Impact on the Project

Actions that negatively impact the project will be handled by the admins, in coordination with other maintainers, in balance with the urgency of the issue. Examples would be [Code of Conduct](CODE_OF_CONDUCT.md) violations, deliberate harmful or malicious actions, spam, monopolization, and security risks.

## Common scenarios

These are recurring ambiguous situations that new and existing maintainers may encounter. They serve as guidance. It is up to each maintainer to follow, adjust, or handle in a different manner as long as [our conduct is consistent](#uphold-code-of-conduct)

### Contribution is stuck

A contribution can get stuck often due to lack of bandwidth and language barrier. For bandwidth issues, check whether the author needs help. Make sure you get their permission before pushing code into their existing PR - do not create a new PR unless strictly necessary.

For language barrier and others, offer a 1:1 chat to get them unblocked. Often times, English might not be their primary language, and writing in public might put them off, or come across not the way they intended to be.

In other cases, you may have constrained capacity. Use `help wanted` label when you want to signal other maintainers and external contributors that you could use a hand to move it forward.

### Insufficient feedback or information

When in doubt, use `need-more-information` or `need-customer-feedback` labels to signal more context and feedback are necessary before proceeding. You can also use `revisit-in-3-months` label when you expect it might take a while to gather enough information before you can decide.

### Crediting contributions

We credit all contributions as part of each [release note](https://github.com/aws-powertools/lambda-dotnet/releases) as an automated process. If you find contributors are missing from the release note you're producing, please add them manually.

### Is that a bug?

A bug produces incorrect or unexpected results at runtime that differ from its intended behaviour. Bugs must be reproducible. They directly affect customers experience at runtime despite following its recommended usage.

Documentation snippets, use of internal components, or unadvertised functionalities are not considered bugs.

### Mentoring contributions

Always favour mentoring issue authors to contribute, unless they're not interested or the implementation is sensitive (_e.g., complexity, time to release, etc._).

Make use of `help wanted` and `good first issue` to signal additional contributions the community can help.

### Long running issues or PRs

Try offering a 1:1 call in the attempt to get to a mutual understanding and clarify areas that maintainers could help.

In the rare cases where both parties don't have the bandwidth or expertise to continue, it's best to use the `revisit-in-3-months` label. By then, see if it's possible to break the PR or issue in smaller chunks, and eventually close if there is no progress.
