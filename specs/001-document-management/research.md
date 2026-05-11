# Research Summary: Document Management Feature

**Session**: 2026-05-10 | **Method**: Clarification Review with Stakeholders | **Status**: Complete

## Overview

All ambiguities in the feature specification have been systematically addressed through targeted clarification questions. This document consolidates decisions, rationales, and supporting context.

---

## Key Decisions & Rationales

### 1. Project Deletion Lifecycle

**Ambiguity**: "Todos los documentos asociados con el proyecto deben archivarse o moverse a documentos personales basado en política comercial - NECESITA ACLARACIÓN"

**Decision**: Documents are archived (marked as deleted/hidden but data retained) when their associated project is deleted. Project association is removed, but documents remain accessible to the uploader in "My Documents" view.

**Rationale**:
- Preserves audit trail for compliance (documents not permanently lost)
- Maintains accessibility for document creators (they can still reference/download)
- Prevents unintended data loss if project deletion is accidental
- Supports enterprise requirement to retain work artifacts
- Consistent with Google Drive and Microsoft OneDrive behavior for shared files

**Implementation Implication**:
- Add `IsArchived` boolean flag to Document entity
- Filter archived docs from project views (RF-020)
- Include archived docs in "My Documents" with "ARCHIVADO" badge (RF-012)
- When Project.Delete() is called → set all associated Documents.IsArchived = true

---

### 2. Document Sharing Model (Recursive vs. Direct)

**Ambiguity**: "¿deberían los documentos compartidos también compartirse recursivamente si el destinatario los comparte con otros, o solo compartir directo?"

**Decision**: Direct sharing only. Only the document owner can initiate shares; recipients cannot re-share with others.

**Rationale**:
- Simplifies authorization model (no need for transitive permission chains)
- Easier to audit and track (single source of truth: owner → recipient)
- Reduces complexity of access control logic (no need to check "who shared this with me then they shared with Bob")
- Matches enterprise policy pattern (document owner maintains control)
- Prevents unintended information leak if recipient account is compromised

**Implementation Implication**:
- `DocumentShare` entity stores only: `SharedBy` (document owner), `SharedWith` (recipient), `Document`
- UI hides "share" button for users who received a document but don't own it
- DocumentAuthService.CanShareDocument() checks if user == Document.UploadedBy

---

### 3. Archived Document Visibility & UI Treatment

**Ambiguity**: What happens to archived docs in search and views? Hide completely or show with visual indicator?

**Decision**: Archived documents remain visible in search and "My Documents" but with prominent "ARCHIVADO" badge. Users can filter to hide archived docs if desired.

**Rationale**:
- Users may need to reference historical documents from deleted projects
- Maintains transparency (archived docs aren't "hidden," just marked)
- Supports audit/compliance scenarios (can view reason document exists: project was deleted)
- Default filter shows all docs; users who prefer clean view can opt-in to hide archived
- No cognitive surprise: docs don't mysteriously disappear

**Implementation Implication**:
- Add filter toggle in DocumentList component: "Mostrar documentos archivados" (default: checked)
- Badge styling: red/orange "ARCHIVADO" label visible on list items
- Search results include archived docs by default; filterable
- When viewing Project.DocumentsTab, archived docs NOT shown (they're not "of the project" anymore)

---

### 4. Antivirus Scanning Failure Handling

**Ambiguity**: "¿Qué sucede si el escaneo antivirus falla o no está disponible?"

**Decision**: Block the upload completely. Return clear error message: "El sistema de verificación de seguridad no está disponible. Por favor, intente más tarde."

**Rationale**:
- Security-first approach; no compromise on integrity
- If antivirus is unavailable, we cannot guarantee file safety
- No fallback to "upload anyway" or "defer scanning" because:
  - Infected files could be accessed by other users before scanning completes (deferred)
  - User might not return for manual scan (lost audit trail)
  - Training/enterprise context demands explicit security posture
- User experience: clear message explains the issue and suggests retry (not a cryptic error)

**Implementation Implication**:
- DocumentService.UploadDocumentAsync checks antivirus health before accepting IFormFile
- If antivirus endpoint returns error/timeout, throw AntivirusUnavailableException
- Return HTTP 503 (Service Unavailable) with user-friendly message
- Log attempt with IP address for security incident review

---

### 5. User Deactivation & Shared Document Access

**Ambiguity**: "If User A shares a document with User B, and User A is later deactivated or deleted, does User B lose access?"

**Decision**: No. User B maintains permanent access to documents explicitly shared with them, regardless of User A's account status.

**Rationale**:
- Collaboration continuity (User B shouldn't lose work access due to User A departing)
- DocumentShare represents an explicit permission grant; it's independent of the sharer's account
- Enterprise pattern: shared files remain accessible after contributor leaves
- Prevents workflow disruption (team shouldn't lose visibility into shared materials)
- Audit trail still shows User A as the original sharer (metadata is immutable)

**Implementation Implication**:
- When checking access: query DocumentShare table directly; don't verify sharer.IsActive
- User deactivation deletes their own uploads but NOT the DocumentShare grants they created
- Access query: `var canAccess = document.UploadedBy == userId || documentShare.SharedWith == userId`
- No need to cascade-delete DocumentShare records when User is deactivated

---

## Supporting Context & Best Practices

### Antivirus Integration Strategy
ContosoDashboard uses local file storage and LocalDB. Antivirus scanning is assumed available (e.g., Windows Defender via ClamAV wrapper, or dedicated service). For training purposes:
- Real implementation should call antivirus API before storing file
- Fallback: if no antivirus available, document this as P1 blocker (don't launch without it)
- Future: mock antivirus service for offline testing

### File Storage Architecture (Offline-Ready)
- Files stored in `AppData/uploads/` with GUID-based names (prevents traversal attacks)
- Metadata in LocalDB; file content in local filesystem
- Clear migration path to Azure Blob Storage: just swap FileStorageService implementation
- Training value: demonstrates how to abstract file storage for multi-backend support

### RBAC & Authorization Patterns Demonstrated
1. **Resource-level authorization** (IDOR protection): Documents check user access before serving
2. **Role-based access**: Project members can see project docs; non-members cannot
3. **Owner-only actions**: Only document owner can share/delete
4. **Explicit grants**: Document shares are recorded in audit log

### Search Performance Rationale
- MVP target: 2-second search for ≤500 documents
- Implementation: LINQ-to-SQL query + in-memory filtering on category/tags
- Future optimization: Full-text search index in SQL Server (FTS) if volume exceeds thresholds
- Training value: Shows when "simple" queries work vs. when indexing becomes necessary

---

## Clarification Session Outcomes

| # | Question | Decision | Confidence | Risk if Wrong |
|---|----------|----------|------------|--------------|
| 1 | Project deletion → document lifecycle? | Archive docs | High | Data loss if wrong; user complaint |
| 2 | Allow recursive sharing? | No, direct only | High | Security/audit complexity if wrong |
| 3 | Show/hide archived docs? | Show with badge | High | UX confusion if hidden |
| 4 | Antivirus failure handling? | Block upload | High | Security incident if wrong |
| 5 | Shared access on user deactivation? | Persist access | High | Workflow disruption if revoked |

All decisions are consistent with enterprise SaaS patterns and training architecture principles.

---

## Next Artifact: data-model.md

Data model is defined in the implementation plan; see `plan.md` section "Phase 1: Design & Contracts" for:
- Document entity with all fields and relationships
- DocumentShare entity for explicit sharing
- DocumentAuditLog entity for compliance tracking
- Validation rules and cascade behaviors
