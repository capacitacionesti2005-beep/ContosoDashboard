# Service Contracts: Document Management API

**Feature**: Carga y Gestión de Documentos  
**Layer**: Service / Business Logic  
**Language**: C#  
**Version**: 1.0.0  
**Date**: 2026-05-10

---

## Overview

Document management exposes a service-oriented API for Razor components and backend logic. All services implement explicit authorization checks (IDOR protection) and log audit trails.

Services are injected via dependency injection into Razor pages and components. Example:

```csharp
@inject DocumentService DocumentService
@inject DocumentAuthService AuthService
```

---

## IDocumentService

Main orchestration service for document lifecycle: upload, retrieval, search, sharing, deletion.

### Upload Document

**Signature**:
```csharp
public async Task<DocumentUploadResult> UploadDocumentAsync(
    DocumentUploadRequest request,
    int userId,
    string ipAddress,
    string userAgent)
```

**Request**:
```csharp
public class DocumentUploadRequest
{
    public IFormFile File { get; set; }                  // Required
    public string Title { get; set; }                    // Required; 1-500 chars
    public string Description { get; set; }             // Optional; 0-2000 chars
    public string Category { get; set; }                 // Required; enum: ProjectDocs, TeamResources, PersonalFiles, Reports, Presentations, Other
    public int? AssociatedProjectId { get; set; }        // Optional
    public List<string> Tags { get; set; }               // Optional; max 10, 50 chars each
}
```

**Response**:
```csharp
public class DocumentUploadResult
{
    public bool Success { get; set; }
    public int? DocumentId { get; set; }                 // If success = true
    public string ErrorMessage { get; set; }             // If success = false
    public string ErrorCode { get; set; }                // E.g., "FILE_TOO_LARGE", "INVALID_EXTENSION", "ANTIVIRUS_UNAVAILABLE"
}
```

**Behavior**:
1. Validate file size (≤25 MB)
2. Validate file extension (whitelist)
3. Check user authorization for project (if AssociatedProjectId provided)
4. **Call antivirus scan** (block if unavailable)
5. Generate GUID-based file path
6. Store file via FileStorageService
7. Create Document record in DB
8. Log "Upload" action to DocumentAuditLog
9. Send notification if assigned to project

**Throws**:
- `FileToolargeException` — If FileSizeBytes > 25 MB
- `InvalidFileTypeException` — If extension not in whitelist
- `UnauthorizedAccessException` — If user not authorized for project
- `AntivirusUnavailableException` — If antivirus scan fails/times out
- `FileStorageException` — If file system error occurs

**HTTP Endpoint** (Blazor component calls service method; UI binding generates form):
```csharp
// POST /api/documents/upload
// Content-Type: multipart/form-data
// Returns: 200 OK { documentId, title, ... } or 400/403/503
```

---

### Get My Documents

**Signature**:
```csharp
public async Task<GetMyDocumentsResult> GetMyDocumentsAsync(
    int userId,
    DocumentsFilterRequest filter)
```

**Request**:
```csharp
public class DocumentsFilterRequest
{
    public string SortBy { get; set; }                   // "uploadDate" (default), "title", "size"
    public bool SortDescending { get; set; }             // Default: true
    public string CategoryFilter { get; set; }           // Optional category
    public int? ProjectIdFilter { get; set; }            // Optional project
    public DateTime? DateFromFilter { get; set; }        // Optional date range
    public DateTime? DateToFilter { get; set; }
    public bool IncludeArchived { get; set; }            // Default: false
    public int PageNumber { get; set; }                  // Default: 1
    public int PageSize { get; set; }                    // Default: 50; max: 500
}
```

**Response**:
```csharp
public class GetMyDocumentsResult
{
    public List<DocumentDTO> Documents { get; set; }
    public int Total { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (Total + PageSize - 1) / PageSize;
}

public class DocumentDTO
{
    public int DocumentId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploaderName { get; set; }
    public int UploaderUserId { get; set; }
    public int? ProjectId { get; set; }
    public string ProjectName { get; set; }
    public List<string> Tags { get; set; }
    public bool IsArchived { get; set; }
    public bool CanDelete { get; set; }                  // User == Uploader
    public bool CanShare { get; set; }                   // User == Uploader
    public bool IsSharedWithMe { get; set; }             // From DocumentShare
}
```

**Behavior**:
1. Query Document table: UploadedBy = userId
2. Apply filters (category, project, date range, archived)
3. Sort and paginate
4. Hydrate DTOs with UserNames (join to User table)
5. Mark CanDelete, CanShare, IsSharedWithMe flags

**Query Details**:
- Default: exclude archived documents
- If IncludeArchived = true: include archived with "ARCHIVADO" badge in UI
- Pagination: offset-based (Page 1 = rows 0-49, Page 2 = rows 50-99, etc.)

**HTTP Endpoint**:
```csharp
// GET /api/documents/my-documents
// Query params: sortBy, sortDescending, categoryFilter, projectIdFilter, dateFromFilter, dateToFilter, includeArchived, pageNumber, pageSize
// Returns: 200 OK { documents: [...], total, pageNumber, pageSize }
```

---

### Get Document (Retrieve for View/Download)

**Signature**:
```csharp
public async Task<DocumentDTO> GetDocumentAsync(
    int documentId,
    int requestingUserId)
```

**Behavior**:
1. Query Document by DocumentId
2. **Check authorization** via DocumentAuthService.CanAccessDocumentAsync()
3. If authorized: return DocumentDTO with full metadata
4. If unauthorized: throw UnauthorizedAccessException

**Throws**:
- `DocumentNotFoundException` — If document doesn't exist
- `UnauthorizedAccessException` — If user lacks access (IDOR protection)

---

### Download Document

**Signature**:
```csharp
public async Task<DocumentDownloadResult> DownloadDocumentAsync(
    int documentId,
    int requestingUserId,
    string ipAddress)
```

**Response**:
```csharp
public class DocumentDownloadResult
{
    public byte[] FileContent { get; set; }
    public string FileName { get; set; }
    public string MimeType { get; set; }
}
```

**Behavior**:
1. Get document via GetDocumentAsync (includes auth check)
2. Retrieve file from disk via FileStorageService
3. Log "Download" action to DocumentAuditLog
4. Return file content with original FileName for browser download

**HTTP Endpoint**:
```csharp
// GET /api/documents/{documentId}/download
// Returns: 200 OK (file as attachment) or 403 Forbidden or 404 Not Found
// Headers: Content-Disposition: attachment; filename="OriginalName.pdf"
```

---

### Search Documents

**Signature**:
```csharp
public async Task<SearchDocumentsResult> SearchDocumentsAsync(
    string searchTerm,
    int userId,
    DocumentsFilterRequest filter)
```

**Behavior**:
1. Parse searchTerm into keywords
2. Query Document table: Title OR Description OR Tags OR UploaderName contains keywords
3. Filter by category, project, date range
4. **Filter by access**: User must be uploader OR document is shared with user
5. Exclude documents user doesn't have access to (authorization filter)
6. Sort and paginate
7. Return DocumentDTOs

**Search Fields**:
- Document.Title (highest priority)
- Document.Description (medium priority)
- Document.Tags (medium priority)
- User.FullName (uploader name)
- Project.ProjectName (if associated)

**Performance Target**: ≤2 seconds for search across 500 documents.

**HTTP Endpoint**:
```csharp
// GET /api/documents/search?q=keyword&categoryFilter=...&projectIdFilter=...
// Returns: 200 OK { documents: [...], total }
```

---

### Share Document

**Signature**:
```csharp
public async Task<ShareDocumentResult> ShareDocumentAsync(
    int documentId,
    int shareWithUserId,
    int requestingUserId,
    string ipAddress)
```

**Response**:
```csharp
public class ShareDocumentResult
{
    public bool Success { get; set; }
    public int? DocumentShareId { get; set; }
    public string ErrorMessage { get; set; }
}
```

**Behavior**:
1. Get document (includes existence check)
2. **Verify requestingUserId == Document.UploadedBy** (only owner can share)
3. Verify shareWithUserId != requestingUserId (cannot share with self)
4. Check for existing active share (DocumentShare where RevokedAt IS NULL)
5. If exists: return error "Document already shared with this user"
6. Create DocumentShare record: SharedBy = requestingUserId, SharedWith = shareWithUserId
7. Create in-app Notification: "User A shared document 'Title' with you"
8. Log "Share" action to DocumentAuditLog
9. Return success with DocumentShareId

**Throws**:
- `UnauthorizedAccessException` — If requestingUserId != Document.UploadedBy
- `DocumentNotFoundException` — If document doesn't exist
- `UserNotFoundException` — If shareWithUserId doesn't exist
- `DocumentAlreadySharedException` — If already shared with that user

**HTTP Endpoint**:
```csharp
// POST /api/documents/{documentId}/share
// Body: { shareWithUserId }
// Returns: 201 Created or 400/403/404
```

---

### Revoke Document Share

**Signature**:
```csharp
public async Task<RevokeShareResult> RevokeShareAsync(
    int documentShareId,
    int requestingUserId,
    string ipAddress)
```

**Behavior**:
1. Get DocumentShare record
2. **Verify requestingUserId == DocumentShare.SharedBy** (only sharer can revoke)
3. Set RevokedAt = DateTime.UtcNow (soft delete)
4. Delete associated Notification (if exists)
5. Log "Revoke" action to DocumentAuditLog
6. Return success

**Throws**:
- `UnauthorizedAccessException` — If requestingUserId != SharedBy
- `DocumentShareNotFoundException` — If share doesn't exist

---

### Get Shared With Me

**Signature**:
```csharp
public async Task<GetSharedWithMeResult> GetSharedWithMeAsync(
    int userId,
    DocumentsFilterRequest filter)
```

**Behavior**:
1. Query DocumentShare table: SharedWith = userId AND RevokedAt IS NULL
2. Join to Document table
3. Apply category/project/date filters
4. Sort and paginate
5. Return DocumentDTOs marked IsSharedWithMe = true

---

### Delete Document

**Signature**:
```csharp
public async Task<DeleteDocumentResult> DeleteDocumentAsync(
    int documentId,
    int requestingUserId,
    string ipAddress)
```

**Behavior**:
1. Get document
2. **Check authorization**: requestingUserId == Document.UploadedBy OR user is ProjectManager for AssociatedProject
3. Delete file from disk via FileStorageService
4. Delete Document record (cascade deletes DocumentShare, DocumentTag)
5. Log "Delete" action to DocumentAuditLog (with filename for recovery tracking)
6. Return success

**Throws**:
- `UnauthorizedAccessException` — If user not authorized
- `DocumentNotFoundException` — If document doesn't exist
- `FileStorageException` — If file deletion fails

**UI Consideration**: Show confirmation dialog: "Eliminar documento 'Title'? Esta acción es permanente."

---

### Archive Project Documents

**Signature** (Internal/Admin):
```csharp
public async Task<ArchiveProjectDocumentsResult> ArchiveProjectDocumentsAsync(
    int projectId,
    int requestingUserId,
    string ipAddress)
```

**Behavior** (called from ProjectService.DeleteProjectAsync):
1. Query Document table: AssociatedProjectId = projectId
2. For each document: set IsArchived = true, ArchivedAt = DateTime.UtcNow
3. Log "Archive" action to DocumentAuditLog for each document
4. Return count of archived documents

---

## IDocumentAuthService

Authorization checks for IDOR protection.

### Can Access Document

**Signature**:
```csharp
public async Task<bool> CanAccessDocumentAsync(
    int documentId,
    int userId)
```

**Returns** true if:
- User uploaded the document (Document.UploadedBy == userId), OR
- Document is shared with user (EXISTS DocumentShare WHERE DocumentId = documentId AND SharedWith = userId AND RevokedAt IS NULL), OR
- User is member of associated project AND project member permissions allow viewing project documents

---

### Can Modify Document

**Signature**:
```csharp
public async Task<bool> CanModifyDocumentAsync(
    int documentId,
    int userId)
```

**Returns** true if:
- User uploaded the document (Document.UploadedBy == userId), OR
- User is ProjectManager for associated project

---

### Can Share Document

**Signature**:
```csharp
public async Task<bool> CanShareDocumentAsync(
    int documentId,
    int userId)
```

**Returns** true if:
- User uploaded the document (Document.UploadedBy == userId)

---

## IFileStorageService

Low-level file system operations.

### Store File

**Signature**:
```csharp
public async Task<string> StoreFileAsync(
    IFormFile file,
    int documentId)
```

**Returns**: Logical file path (e.g., "AppData/uploads/12345/abc-def-123-ghi")

**Behavior**:
1. Create directory: AppData/uploads/{DocumentId}/
2. Generate GUID: {GUID}
3. Write file to AppData/uploads/{DocumentId}/{GUID}
4. Return path

**Throws**:
- `DirectoryNotFoundException` — If AppData directory doesn't exist
- `IOException` — If write fails (disk full, permissions, etc.)

---

### Retrieve File

**Signature**:
```csharp
public async Task<byte[]> RetrieveFileAsync(string filePath)
```

**Returns**: File contents as byte array

**Throws**:
- `FileNotFoundException` — If file doesn't exist
- `IOException` — If read fails

---

### Delete File

**Signature**:
```csharp
public async Task DeleteFileAsync(string filePath)
```

**Behavior**:
1. Delete file from disk
2. Remove directory if empty

**Throws**:
- `FileNotFoundException` — If file doesn't exist (may be tolerated)
- `IOException` — If deletion fails

---

## IDocumentAuditService

Immutable audit logging.

### Log Action

**Signature**:
```csharp
public async Task LogActionAsync(
    int documentId,
    int userId,
    string action,
    string ipAddress,
    string userAgent,
    string details = null)
```

**Behavior**:
1. Create DocumentAuditLog record
2. Insert into DB (write-once)
3. Return success

**Actions**: Upload, Download, Share, Revoke, Edit, Delete, Archive

---

## Error Handling & HTTP Status Codes

| Error | HTTP Status | Message |
|-------|------------|---------|
| Document not found | 404 | "Documento no encontrado" |
| Unauthorized access | 403 | "No tienes permiso para acceder a este documento" |
| File too large | 400 | "El archivo excede el límite de 25 MB" |
| Invalid file type | 400 | "Tipo de archivo no permitido" |
| Antivirus unavailable | 503 | "El sistema de verificación de seguridad no está disponible. Por favor, intente más tarde" |
| Duplicate share | 400 | "Este documento ya está compartido con este usuario" |
| Antivirus infected | 400 | "El archivo fue detectado como potencialmente malicioso. No se puede cargar" |

---

## Dependency Injection Setup

```csharp
// Program.cs
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<DocumentAuthService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<DocumentSearchService>();
builder.Services.AddScoped<DocumentAuditService>();
builder.Services.AddScoped<IAntivirusService, AntivirusService>();  // TBD: implementation
```

---

## Security Considerations

1. **IDOR Protection**: Every service method checks authorization before returning data
2. **No Directory Traversal**: Files stored with GUIDs; no user-controllable paths
3. **File Storage Outside Web Root**: `AppData/uploads/` is not served directly; access only via API
4. **Audit Logging**: All document actions logged with user, timestamp, IP, user agent
5. **No Antivirus Fallback**: If antivirus unavailable, block upload (fail-secure)
6. **Soft-Delete for Sharing**: RevokedAt flag instead of hard delete preserves audit trail
