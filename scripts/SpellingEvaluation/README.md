# Evaluate proofing quality separately from software checks

The normal regression suite protects code behavior. It does not measure whether
Vietnamese spelling warnings are correct in real text.

For the pinned real-error development audit, alternative answers, source-label
agreement and reserved partition, see [REAL-ERRORS.md](REAL-ERRORS.md). Its reports
contain aggregates only. The normal application still disables the prototype.

Run from the repository root:

    python scripts/SpellingEvaluation/prepare.py --sources /path/to/UD_Vietnamese-VTB --output /tmp/spelling-audit
    dotnet run --project tests/ChinhTa.SpellingEvaluation -c Release -- /tmp/spelling-audit /tmp/spelling-audit/report.json
    python scripts/SpellingEvaluation/summarize.py /tmp/spelling-audit/report.json --output /tmp/spelling-audit/report.md

Use --fetch to retrieve missing corpus files from the exact pinned commit.
Every source SHA-256 is checked against the bundled manifest. The script
reconstructs the 2,639/659 training/audit split and every runtime n-gram count,
rejecting any mismatch. The historical excluded set was used in earlier
development and is explicitly not a new blind test. No model data or manifest is
rewritten.

The evaluator compares the previous 0.1.91-compatible behavior and an experimental
generic diacritic stage through WorkerService, including strict syllable lookup,
scope filtering and personal terms. The stage is disabled in normal application
calls. No word-specific Hom/Hôm exception is implemented.

Report separately:

- Developer fixtures: per error group, target detection, correct suggestion in
  the first/top three choices, and warnings outside labeled targets.
- Synthetic missing-mark stress rows: recovery of the intentionally changed
  token, not real-world spelling recall.
- Original unannotated news: warning load per 1,000 tokens, not false-positive
  rate. Review is required before declaring any warning right or wrong.
- Warm worker latency and cold model/index setup; not Word UI latency.

Every returned span must match the original UTF-16 string. The evaluator never
edits documents, uses Word, sends text to a service, or calls a red warning an
automatic replacement authorization.

ReleaseReadiness remains NOT_QUALIFIED until a separate, independently reviewed
real-error/correct-text benchmark exists and the product gates in
docs/SPELLING-QUALITY-ROADMAP.md are measured. CI deliberately emits this status
even when the measurement process and code regressions pass. The quality
workflow uploads reports and provenance only, with no installer.

Corpus-derived audit data retains CC BY-SA 4.0. Keep attribution in
assets/spelling/NOTICE.txt and CC-BY-SA-4.0.txt when sharing derived audit rows.
Do not add private user documents or personal dictionaries to these fixtures.
