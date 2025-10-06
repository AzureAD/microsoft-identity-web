Microsoft Identity Web welcomes new contributors. This document will guide you
through the process.

### Contributor License agreement

Please visit [https://cla.microsoft.com/](https://cla.microsoft.com/) and sign the Contributor License
Agreement.  You only need to do that once. We can not look at your code until you've submitted this request.

### Setup, Building and Testing

Please see the [Build & Run](build-and-test) wiki page.

### Decide on which branch to create

**Bug fixes for the current stable version need to go to 'master' branch.**

If you need to contribute to a different branch, please contact us first (open an issue).

All details after this point are standard - make sure your commits have nice messages, and prefer rebase to merge.

In case of doubt, open an issue in the [issue tracker](https://github.com/AzureAD/microsoft-identity-web/issues/new/choose).

Especially do so if you plan to work on a major change in functionality.  Nothing is more
frustrating than seeing your hard work go to waste because your vision
does not align with our goals for the SDK.

### Branch

Okay, so you have decided on the proper branch.  Create a feature branch
and start hacking:

```
$ git checkout -b my-feature-branch 
```

### Commit

Make sure git knows your name and email address:

```
$ git config --global user.name "J. Random User"
$ git config --global user.email "j.random.user@example.com"
```

Writing good commit logs is important.  A commit log should describe what
changed and why.  Follow these guidelines when writing one:

1. The first line should be 50 characters or less and contain a short
   description of the change prefixed with the name of the changed
   subsystem (e.g. "net: add localAddress and localPort to Socket").
2. Keep the second line blank.
3. Wrap all other lines at 72 columns.

A good commit log looks like this:

```
fix: explaining the commit in one line

Body of commit message is a few lines of text, explaining things
in more detail, possibly giving some background about the issue
being fixed, etc etc.

The body of the commit message can be several paragraphs, and
please do proper word-wrap and keep columns shorter than about
72 characters or so. That way `git log` will show things
nicely even when it is indented.
```

The header line should be meaningful; it is what other people see when they
run `git shortlog` or `git log --one line`.

Check the output of `git log --one line files_that_you_changed` to find out
what directories your changes touch.

### Rebase

Use `git rebase` (not `git merge`) to sync your work from time to time.

```
$ git fetch upstream
$ git rebase upstream/v0.1  # or upstream/master
```

### Tests

It's all standard stuff, but please note that you won't be able to run integration tests locally because they connect to a KeyVault to fetch some test users and passwords. The CI will run them for you.

### Push

```
$ git push origin my-feature-branch
```

Go to `https://github.com/username/microsoft-identity-web` and select your feature branch.  Click
the 'Pull Request' button and fill out the form.

Pull requests are usually reviewed within a few days.  If there are comments
to address, apply your changes in a separate commit and push that to your
feature branch.  Post a comment in the pull request afterwards; GitHub does
not send out notifications when you add commits.

[on GitHub]: https://github.com/AzureAD/microsoft-identity-web
[issue tracker]: https://github.com/AzureAD/microsoft-identity-web/issues