# Services

Business logic lives here (controllers stay thin). Each service implements an interface from
`Services/Interfaces`. A service is responsible for: the business rule + writing `AuditLog` +
pushing `Notification` — all in the SAME transaction.

The shared contracts are already defined in `Services/Interfaces` (`ITagService`,
`INotificationService`, `IAuditService`). Add implementations here as features land, and register
them in `Program.cs` (see the commented placeholder block there).
