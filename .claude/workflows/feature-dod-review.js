export const meta = {
  name: 'feature-dod-review',
  description: 'Review the current working changes against the LostAndFound Definition of Done (state machine, authz/ownership, audit, notifications, validation, hidden-field leaks, tags, UI conventions, DB-First rules), then adversarially verify each finding.',
  phases: [
    { title: 'Review', detail: 'one reviewer per DoD dimension' },
    { title: 'Verify', detail: 'adversarially verify each finding' },
  ],
}

const FINDINGS_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['findings'],
  properties: {
    findings: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        required: ['title', 'file', 'severity', 'detail'],
        properties: {
          title: { type: 'string' },
          file: { type: 'string', description: 'path:line of the changed code' },
          severity: { type: 'string', enum: ['blocker', 'major', 'minor'] },
          detail: { type: 'string', description: 'what is wrong and how to fix it' },
        },
      },
    },
  },
}

const VERDICT_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['isReal', 'reason'],
  properties: {
    isReal: { type: 'boolean' },
    reason: { type: 'string' },
  },
}

const BASE = `You are reviewing the CURRENT working changes of the LostAndFound .NET 8 ASP.NET Core MVC app.
First inspect the changes: run \`git status\`, \`git diff\`, and \`git diff --staged\`; if the branch has commits not on dev, also \`git diff dev...HEAD\`. Read the changed files for context.
The project conventions live in CLAUDE.md and docs/INDEX.md (read them). This is a DB-First project: the DB schema in LostAndFound/db/schema.sql is the source of truth; entities in Models/Entities are generated; there are NO EF migrations.
Only report problems in the CHANGED code (do not audit the whole repo). If the dimension is fully satisfied, return an empty findings list.`

const DIMENSIONS = [
  { key: 'state-machine', title: 'State machine', prompt: 'Are status transitions legal? An item reaches Returned ONLY when BOTH HolderConfirmedHandover and ClaimantConfirmedHandover are true. Accepting one claim must auto-reject the other claims on that item, in ONE transaction. An item with a Pending claim is locked. Enum values must match Models/Enums (never renamed/reordered).' },
  { key: 'authz-ownership', title: 'Authorization & ownership', prompt: 'Do controller actions have correct [Authorize(Roles=...)]? Does the service check ownership / that the caller is the holder before mutating? Holder must be DERIVED from HoldingType (SelfHeld=reporter, Custodial=staff), never hardcoded by role. Test-with-missing-permission path considered?' },
  { key: 'audit', title: 'AuditLog', prompt: 'Is an AuditLog row written for every important action, inside the SAME transaction as the action? Is IsPublic set correctly (public rows feed the timeline)?' },
  { key: 'notification', title: 'Notification', prompt: 'When the action affects another user, is a Notification created (DB row) and pushed via SignalR (correct group: userId or "staff")?' },
  { key: 'validation', title: 'Two-tier validation', prompt: 'Tier 1: Data Annotations on the ViewModel + ModelState.IsValid in the controller + service-level business checks. Tier 2: a real DB constraint exists in schema.sql. Both required for important rules.' },
  { key: 'blind-listing', title: 'Hidden-field leaks', prompt: 'PrivateMarks and claim VerificationDetails / evidence images must NEVER appear on public pages or the public timeline. Check views and any AuditLog.Detail written with IsPublic=true.' },
  { key: 'tags', title: 'Tag normalization', prompt: 'All tag matching/subscribe must compare on NormalizedTag using the single TagService.Normalize function. No ad-hoc normalization. Display uses the raw DisplayTag.' },
  { key: 'ui-conventions', title: 'MVC conventions', prompt: 'Controllers are thin (logic in Services). Views receive ViewModels, never entities. Forms use TagHelpers (asp-for/asp-validation-for/asp-action). Reused markup is in PartialViews (_Name).' },
  { key: 'db-first', title: 'DB-First rules', prompt: 'No EF migrations added (this is DB-First). Schema changes go to db/schema.sql first. Generated entities in Models/Entities are not hand-edited as if they were the source. ApplicationDbContext (not the throwaway ScaffoldDbContext) is the runtime context and is updated correctly.' },
]

const results = await pipeline(
  DIMENSIONS,
  d => agent(
    `${BASE}\n\n## Review dimension: ${d.title}\n${d.prompt}\n\nReturn concrete findings with file:line and a fix. Empty list if clean.`,
    { label: `review:${d.key}`, phase: 'Review', schema: FINDINGS_SCHEMA },
  ),
  (review, d) => parallel((review?.findings ?? []).map(f => () =>
    agent(
      `${BASE}\n\n## Adversarially verify this claimed DoD issue (dimension: ${d.title})\nTitle: ${f.title}\nFile: ${f.file}\nSeverity: ${f.severity}\nDetail: ${f.detail}\n\nOpen the actual changed code and decide if this is a REAL problem. Default isReal=false unless you can cite the specific code that proves it. Beware false positives from code that lives outside the diff.`,
      { label: `verify:${d.key}`, phase: 'Verify', schema: VERDICT_SCHEMA },
    ).then(v => ({ ...f, dimension: d.key, verdict: v })),
  )),
)

const all = results.flat().filter(Boolean)
const confirmed = all.filter(f => f.verdict && f.verdict.isReal)
const order = { blocker: 0, major: 1, minor: 2 }
confirmed.sort((a, b) => (order[a.severity] ?? 9) - (order[b.severity] ?? 9))

log(`DoD review: ${confirmed.length} confirmed issue(s) out of ${all.length} raised.`)

return {
  confirmedCount: confirmed.length,
  raisedCount: all.length,
  confirmed: confirmed.map(f => ({
    dimension: f.dimension,
    severity: f.severity,
    title: f.title,
    file: f.file,
    detail: f.detail,
    why: f.verdict.reason,
  })),
}
