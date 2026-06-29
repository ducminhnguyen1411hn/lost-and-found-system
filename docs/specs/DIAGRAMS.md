# Diagrams — State Machine & ERD

> Mermaid source kept in the repo (renders on GitHub / VS Code / mermaid.live). Edit it here, then re-sync the images in the report.

---

## 1. FoundItem Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Open: SelfHeld (finder keeps it self-held)
    [*] --> PendingDropoff: Custodial (handed to staff)
    PendingDropoff --> Open: staff receives the item
    Open --> ClaimAccepted: holder accepts a claim
    ClaimAccepted --> Returned: both parties confirm the handover
    ClaimAccepted --> Open: holder cancels (reopen)
    Open --> Unclaimed: after 30/60 days
    Unclaimed --> Disposed: dispose / donate
    Returned --> [*]
    Disposed --> [*]
```

## 2. Claim Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Pending
    Pending --> Accepted: holder approves (other claims on the same item → Rejected)
    Pending --> Rejected: holder rejects + RejectReason
    Accepted --> [*]
    Rejected --> [*]
```

> `holder` = the finder if `SelfHeld`, = staff if `Custodial`. Disputes (multiple claims) → staff takes over to adjudicate.

---

## 3. ERD

```mermaid
erDiagram
    APPLICATION_USER ||--o{ LOST_ALERT : "creates"
    APPLICATION_USER ||--o{ FOUND_ITEM : "reports found"
    APPLICATION_USER ||--o{ CLAIM : "claim"
    APPLICATION_USER ||--o{ CAMERA_CHECK_REQUEST : "requests"
    APPLICATION_USER ||--o{ NOTIFICATION : "receives"
    APPLICATION_USER ||--o{ AUDIT_LOG : "performs"
    CATEGORY ||--o{ FOUND_ITEM : ""
    CATEGORY ||--o{ LOST_ALERT : ""
    LOCATION ||--o{ FOUND_ITEM : ""
    LOCATION ||--o{ LOST_ALERT : ""
    LOCATION ||--o{ CAMERA_CHECK_REQUEST : ""
    FOUND_ITEM ||--o{ CLAIM : "has"
    FOUND_ITEM ||--o{ FOUND_ITEM_TAG : ""
    TAG ||--o{ FOUND_ITEM_TAG : ""
    TAG ||--o{ LOST_ALERT_TAG : ""
    LOST_ALERT ||--o{ LOST_ALERT_TAG : ""
    FOUND_ITEM ||--o| THANK_YOU : "generates"

    APPLICATION_USER {
        string Id PK
        string FullName
        string StudentOrStaffCode UK
        string Department
    }
    CATEGORY {
        int Id PK
        int ParentId FK
        string Name
    }
    LOCATION {
        int Id PK
        string Building
        string Name
    }
    LOST_ALERT {
        int Id PK
        string OwnerUserId FK
        int CategoryId FK
        int LocationId FK
        datetime FromDate
        datetime ToDate
        bool IsActive
    }
    FOUND_ITEM {
        int Id PK
        string Title
        int CategoryId FK
        int LocationId FK
        int Status
        int HoldingType
        string PrivateMarks
        string ReporterUserId FK
        string CustodianStaffId FK
        bool HolderConfirmed
        bool ClaimantConfirmed
    }
    TAG {
        int Id PK
        string DisplayTag
        string NormalizedTag UK
    }
    FOUND_ITEM_TAG {
        int FoundItemId FK
        int TagId FK
    }
    LOST_ALERT_TAG {
        int LostAlertId FK
        int TagId FK
    }
    CLAIM {
        int Id PK
        int FoundItemId FK
        string ClaimantUserId FK
        string VerificationDetails
        int Status
        string HandledByUserId FK
        string RejectReason
    }
    CAMERA_CHECK_REQUEST {
        int Id PK
        string RequesterUserId FK
        int LocationId FK
        datetime FromTime
        datetime ToTime
        int Status
    }
    THANK_YOU {
        int Id PK
        int FoundItemId FK
        string FromUserId FK
        string ToUserId FK
        int Rating
    }
    NOTIFICATION {
        int Id PK
        string RecipientUserId FK
        string Type
        bool IsRead
    }
    AUDIT_LOG {
        int Id PK
        string ActorUserId FK
        string Action
        string EntityType
        bool IsPublic
    }
```
