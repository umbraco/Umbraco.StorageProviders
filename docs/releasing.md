# Releasing

Versions are calculated by [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) from `version.json` and the commit height, so no version number is ever typed by hand. A build only publishes when it is a public release, and `publicReleaseRefSpec` limits that to `release-*` tags, so the tag is what makes a build publishable. Publishing to nuget.org then waits on a manual approval, described under [Publishing](#publishing).

## Branches

- `develop` is the development branch for the newest major and the default branch of the repository. `support/X.x` is the development branch for an older, still supported major.
- `main` holds the released code of the newest major, and `vX/main` holds it for an older one. They exist so a patch can be branched from what was actually released rather than from whatever has since landed on a development branch. `main` is moved by **Merge release** each time a stable release of the newest major is published, a `vX/main` is created the first time an older major is released through this process, and the outgoing line is preserved as `vX/main` when a new major takes over `main`.
- `release/X.Y.Z` is cut when preparing a release and is deleted once it has been merged back. The `release-X.Y.Z` tag still points at its final commit, so it can be restored with `git branch release/X.Y.Z release-X.Y.Z && git push origin release/X.Y.Z`.

A development branch holds the next minor version with an `alpha` label, for example `17.2.0-alpha`, so its builds always sort below the release they lead to.

A `main` branch only ever points at a commit that was built, tagged and published. It is never a merge commit, and nothing is committed to it directly.

Fixes that apply to several majors are made on the oldest affected branch and merged forward, ending in `develop`.

## Cutting a release

Both of these are GitHub Actions, run from the **Actions** tab. Each one refuses to run on a branch it does not belong on.

**Minor release:** run **Prepare release** on `develop` or `support/X.x` and choose `rc`, `beta` or `stable`. It creates `release/X.Y.0`, moves the development branch on to the next minor version (keeping its `alpha` label), and merges the release branch back so that the `version.json` conflict is resolved once rather than at every later merge.

**Patch release:** run **Prepare patch release** on `main` or `vX/main`. It takes no options: it creates `release/X.Y.Z+1` at the stable version, because patches ship without stabilisation. Nothing on a development branch is touched. If a patch does need stabilising, run **Prepare release** with `rc` on the new release branch afterwards.

Both also create a draft GitHub Release for the new release branch, ready to be published (see [Publishing](#publishing)).

## Stabilising

Run **Prepare release** again on the `release/X.Y.Z` branch to move its version along:

- the same label continues its numbering, so `rc1` becomes `rc2`
- a different label starts again at 1, so `beta2` becomes `rc1`
- `stable` removes the label, which is the last step before shipping

Each run also updates the draft GitHub Release of the branch to the new version, keeping any release notes already added to it.

Only move labels forward. `beta` to `rc` is fine; nbgv rejects going back from a stable version to a prerelease one, and nothing stops `rc2` becoming `beta1`, which would publish a version sorting below what already shipped.

## Publishing

The prepare workflows leave a draft GitHub Release on the **Releases** page, targeting the release branch. Its tag is `release-` followed by exactly the version in `version.json` on that branch (`release-17.2.0` for a stable release, `release-17.2.0-rc1` for a prerelease), its name is the version itself, and **Set as a pre-release** is ticked when the version has a prerelease label. To publish it:

1. Open the draft and click **Generate release notes**. Do this last, so the notes include everything merged into the release branch while stabilising.
2. Leave **Set as the latest release** unticked. GitHub then assigns the latest release by semantic version, so a stable release on the newest major becomes the latest, while a prerelease or a patch to an older major never does.
3. Publish the release. That creates the tag on the tip of the release branch, which triggers the build, which packs and publishes.

Don't change the tag: `publicReleaseRefSpec` only matches the `release-` prefix, so a typo still builds and publishes.

Building and publishing is Azure Pipelines (`azure-pipelines.yml`), not GitHub Actions. For every public release, prerelease or stable, its `Prerelease` stage pushes the packages to the MyGet pre-release feed without a gate, and its `Release` stage pauses on a manual approval before pushing them to nuget.org. That approval is given from the Azure DevOps run and rejects itself after three days.

## After publishing

Publishing a stable release runs **Merge release**, which sets the major's `main` branch to the released commit, merges the release branch into its development branch, and deletes the release branch. A new major additionally takes over `main`, with the outgoing line preserved as `v{previous}/main`.

It stops without changing anything if the tag is not the tip of its release branch, if `main` is not contained in the tag, or if anything other than `version.json` conflicts. The push is atomic and happens last, so a failed run leaves nothing half-done and can simply be re-run once the cause is fixed. Re-run it from the Actions tab, or start it manually with **Run workflow** and the tag name.

A prerelease is a no-op that says so in the log, whether or not the pre-release box was ticked.

## What is still done by hand

- Adding the release notes to the draft GitHub Release and publishing it.
- Deleting the draft GitHub Release of a release branch that is abandoned.
- Merging between majors, both `release/X.Y.Z` into the next major's release branch and `support/X.x` into the next `support/Y.x` or `develop`. These are judgement calls about timing and they conflict often, so they are deliberately not automated.
- Branching `support/X.x` from `develop` when development moves to a new major while the previous one is still supported, and setting `develop` to the new major's first `alpha` version. This happens before that major is released, so there is no release event to hang it on.
- The first patch release of a major. **Prepare patch release** runs on `main` or `vX/main`, and that branch only holds the major once **Merge release** has run for one of its releases, so until then create `release/X.Y.Z+1` by hand from the `release-X.Y.Z` tag.

## Notes

These workflows run from the commit a tag points at, not from the default branch, so **Merge release** only runs for releases cut from branches that carry these files. Releases up to and including 17.1.0 and 18.0.0 were made before they existed, and `support/13.x` does not have them, so its releases are still prepared and merged back by hand.

They push to `develop`, `support/X.x`, `main` and `vX/main` and create or update the draft releases as `github-actions[bot]` using `GITHUB_TOKEN`, so any branch protection on those refs has to permit it.

Creating the draft with `GITHUB_TOKEN` is fine, because a person publishes it. If publishing is ever automated, it must not use `GITHUB_TOKEN`: releases published with it trigger no workflow at all, so the merge-back would silently never run. Use a personal access token or a GitHub App.
