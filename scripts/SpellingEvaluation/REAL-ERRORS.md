# Real-error development audit

This adapter evaluates the frozen 0.1.91-compatible baseline and disabled
diacritic prototype on VSEC source labels. It does not train a model or package
the source. The pinned upstream repository does not specify a distribution
license; source sentences and annotation files are kept out of the repository,
published artifacts and application bundle. Only aggregate measurements and
provenance are exported.

Run from the repository root:

    python scripts/SpellingEvaluation/test_prepare_vsec.py
    dotnet run --project tests/ChinhTa.SpellingEvaluation -c Release -- --self-check
    python scripts/SpellingEvaluation/prepare.py --sources /tmp/ud-sources --output /tmp/legacy --fetch
    python scripts/SpellingEvaluation/prepare_vsec.py --source /tmp/VSEC.jsonl --training-sources /tmp/ud-sources --output /tmp/vsec-development --fetch
    dotnet run --project tests/ChinhTa.SpellingEvaluation -c Release -- /tmp/vsec-development /tmp/real-error-quality.json
    python scripts/SpellingEvaluation/summarize_external.py /tmp/real-error-quality.json --output /tmp/real-error-quality.md

The source commit and SHA-256 are fixed in prepare_vsec.py. Training sources
are checked and every runtime n-gram count is reconstructed by the same loader
as the historical audit. Exclude a row if its normalized original or any
accepted corrected sentence equals training text or the historical audit.

Alignment must match the complete ordered whitespace-token annotation sequence.
Empty correct annotations may represent repeated spaces; empty error annotations
are rejected. A blank replacement is retained as deletion. A source token that
is itself an accepted alternative is not treated as a certain error. Shared
punctuation is trimmed consistently, and offsets remain into original UTF-16.
No nearest-word search or guessed offset is used.

Union all related original/corrected sentences before hashing sentence groups
into development (80% expected) and reserve (20% expected). Reserved rows are
not exported, printed, scored or used to choose implementation thresholds.
Document-level separation cannot be established because source document IDs
are unavailable. Development numbers are not a blind final score.

Report error categories, dictionary membership of the original as observed by
the actual Core model, target detection, top-one/top-three accepted answers,
and warning overlap with source labels. Report source-correct-token warning
load separately: VSEC has no error-free sentences, so these spans are not a
replacement for an independently reviewed correct-text corpus. Do not call
agreement with these labels a production quality certificate.

The normal application still disables the prototype. The quality workflow
uploads explicit report paths only; never expand its upload pattern to the
private-evaluation directory. JSON reports omit external row text, annotation
content and per-row findings. No personal user documents are used.
