# Rebuild the offline Vietnamese data

The Windows package includes the data. End users do not run this script, download a corpus or import a ZIP.

Maintainers use Python 3.12 and `build.py --sources <folder>`. Place unchanged source files in:

- `vietnamese-wordlist/{Viet74K.txt,LICENSE,README.md}` from commit `688b0953c8cdb21ee090ce80b83cefdb052fe2cd` of https://github.com/duyet/vietnamese-wordlist.
- `UD_Vietnamese-VTB/{vi_vtb-ud-train.conllu,vi_vtb-ud-dev.conllu,vi_vtb-ud-test.conllu,LICENSE.txt,README.md}` from commit `3c971fd1c5d03561e00b267d3d0057b81a9bea84` of https://github.com/UniversalDependencies/UD_Vietnamese-VTB.

Each source can be fetched from `https://raw.githubusercontent.com/<owner>/<repo>/<commit>/<filename>`. No credentials or user files are needed. Source hashes and normalized count totals are recorded in `assets/spelling/manifest.json`.

The builder deduplicates sentence text, excludes a deterministic 20% holdout from all frequency counts, and writes a ZIP containing the unchanged preferred word-list source and readable derived LM JSON. It retains both full license texts, source READMEs and modification attribution alongside the ZIP. Data licenses: GPL-2.0 for the word list and CC BY-SA 4.0 for the adapted LM. They remain separate data files; no upstream program code is incorporated.

Outputs are committed, so normal `dotnet build/publish` requires no Python or network. A changed output requires review of source licenses, NOTICE, totals and hashes before updating the pinned `VietnameseSpellingDefaults.ManifestHash` constant. Recompression with a different zlib version may change ZIP bytes; do not loosen the hash check. For an independent rebuild compare uncompressed entry bytes and count totals too. Current container SHA256 is recorded in the manifest.

Optional `--holdout /tmp/holdout.json` writes the excluded text for local evaluation only. The held-out set measures conservative proposals on news text, not general Vietnamese spelling accuracy. Do not train on the holdout to improve its reported result.
