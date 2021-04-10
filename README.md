# GitGrep

Runs grep against all commits for a given file

## Usage

Run grep against all commits for a specific file (git rev-list --all)

```bash
$ GitGrep TODO Integration
```

Run grep against all files in all commits
```bash
$ GitGrep --all Integration -i
```

Note that arguments after the search pattern are passed directly to grep.

A regex can be passed like so:

```bash
$ GitGrep --all 'Needle.*Pattern'
```

These commands are used:

  - git rev-list --all [filename]
  - git rev-list --all
  - git ls-tree -r --name-only --full-tree commit
  - git show commit
  - grep --line-buffered --with-filename --label={...}
