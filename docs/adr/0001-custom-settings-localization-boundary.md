---
status: accepted
---

# Temporary localization boundary for custom settings rows

Vanilla settings rows cloned by CustomSettings carry the game's localization components, which can overwrite a mod-provided custom item label after a language change. We temporarily disable those components on cloned custom rows so custom labels remain stable across menu refreshes and language switches.

This is a compatibility workaround, not the final localization design: custom item labels are not localized by CustomSettings today. Future localization work must adapt these rows to the localization functionality provided by ModdingAPI, then remove this component-disabling workaround.
