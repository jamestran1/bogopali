# Single Diacritic Key Instead of Doubled Letters

We decided that diacritics are typed with one Diacritic Key (`f`) pressed right after the base letter (`af` gives `ā`, `tf` gives `ṭ`, `mf` gives `ṃ`, `nf` gives `ṇ`), with repeated presses stepping through a Diacritic Cycle for letters that have several forms (`n` → `ṇ` → `ñ` → `ṅ` → `n`, `m` → `ṃ` → `ṁ` → `m`). The doubled-vowel rules `aa`, `ii`, `uu` and the shortcuts `nj` (`ñ`) and `nq` (`ṅ`) stay as alternatives.

## Context

The original rules doubled the consonant (`tt` → `ṭ`, `mm` → `ṃ`, `nn` → `ṇ`, `ll` → `ḷ`, `dd` → `ḍ`). Pāli is full of genuine geminate consonants (*sutta*, *dhamma*, *anna*, *uddesa*, *puggala*), so those rules made common words untypeable: `dhamma` became `dhaṃa`. A third-key undo (`ttt` → `tt`) was rejected because it makes geminate retroflexes such as *aṭṭha* impossible. A per-letter marker (e.g. `tz`) was rejected in favour of one key the users (mostly monastics, not developers) can learn in a sentence: "type the letter, then `f` for the diacritic". `f` sits on the home row and does not occur in Pāli.

## Consequences

- Every geminate is typed naturally; `aṭṭha` is `atftfha`.
- After a Replacement the Input Buffer keeps only the produced character, so a following Diacritic Key can continue the Diacritic Cycle; any other key starts a new sequence (`aaa` gives `āa`).
- English words containing `f` after a vowel or `t/d/n/l/m` (`if`, `half`) are altered in Pāli Mode; users switch to Bypass Mode for English.
- The PRD's default rule table changes; the doubled-consonant rules are removed from the defaults.
