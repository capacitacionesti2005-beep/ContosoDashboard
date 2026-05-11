using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services
{
    public class DocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly FileStorageService _fileStorageService;
        private readonly DocumentAuthService _authService;
        private readonly DocumentAuditService _auditService;

        public DocumentService(
            ApplicationDbContext context,
            FileStorageService fileStorageService,
            DocumentAuthService authService,
            DocumentAuditService auditService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _authService = authService;
            _auditService = auditService;
        }

        public async Task<(bool Success, int? DocumentId, string ErrorMessage)> UploadDocumentAsync(
            IFormFile file,
            string title,
            string description,
            string category,
            int? projectId,
            List<string> tags,
            int userId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return (false, null, "El archivo no es valido");

                if (string.IsNullOrWhiteSpace(title))
                    return (false, null, "El titulo es obligatorio");

                if (title.Length > 500)
                    return (false, null, "El titulo no puede exceder 500 caracteres");

                if (string.IsNullOrWhiteSpace(category))
                    return (false, null, "La categoria es obligatoria");

                if (file.Length > 26_214_400)
                    return (false, null, "El archivo excede el limite maximo de 25 MB");

                if (!_fileStorageService.IsValidFileExtension(file.FileName))
                    return (false, null, "El tipo de archivo no esta permitido. Formatos soportados: PDF, Word, Excel, PowerPoint, imagenes, texto");

                if (projectId.HasValue)
                {
                    var hasProjectAccess = await _context.ProjectMembers
                        .AsNoTracking()
                        .AnyAsync(pm => pm.ProjectId == projectId.Value && pm.UserId == userId);

                    if (!hasProjectAccess)
                    {
                        hasProjectAccess = await _context.Projects
                            .AsNoTracking()
                            .AnyAsync(p => p.ProjectId == projectId.Value && p.ProjectManagerId == userId);
                    }

                    if (!hasProjectAccess)
                        return (false, null, "No tienes acceso al proyecto seleccionado");
                }

                var now = DateTime.UtcNow;
                var document = new Document
                {
                    Title = title.Trim(),
                    Description = description?.Trim() ?? string.Empty,
                    Category = category,
                    FilePath = $"pending/{Guid.NewGuid():N}",
                    FileName = file.FileName,
                    FileSizeBytes = file.Length,
                    MimeType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    UploadedAt = now,
                    UploadedBy = userId,
                    AssociatedProjectId = projectId,
                    Tags = tags != null && tags.Any() ? System.Text.Json.JsonSerializer.Serialize(tags) : "[]",
                    AntivirusStatus = "Pending",
                    CreatedAt = now,
                    ModifiedAt = now,
                    ModifiedBy = userId
                };

                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _context.Documents.Add(document);
                    await _context.SaveChangesAsync();

                    document.FilePath = await _fileStorageService.StoreFileAsync(file, document.DocumentId);
                    document.ModifiedAt = DateTime.UtcNow;

                    _context.Documents.Update(document);
                    await _context.SaveChangesAsync();

                    await _auditService.LogActionAsync(
                        document.DocumentId,
                        userId,
                        "Upload",
                        "192.168.1.1",
                        "Mozilla/5.0",
                        $"Uploaded: {file.FileName}, Size: {file.Length} bytes");

                    await transaction.CommitAsync();
                    return (true, document.DocumentId, "Documento cargado exitosamente");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, null, $"Error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Error inesperado: {ex.Message}");
            }
        }

        public async Task<List<Document>> GetMyDocumentsAsync(
            int userId,
            string sortBy = "uploadDate",
            string categoryFilter = null,
            string projectFilter = null,
            bool includeArchived = false)
        {
            try
            {
                var query = _context.Documents
                    .AsNoTracking()
                    .Include(d => d.Uploader)
                    .Include(d => d.AssociatedProject)
                    .Where(d => d.UploadedBy == userId);

                if (!includeArchived)
                    query = query.Where(d => !d.IsArchived);

                if (!string.IsNullOrWhiteSpace(categoryFilter))
                    query = query.Where(d => d.Category == categoryFilter);

                if (int.TryParse(projectFilter, out var projectId))
                    query = query.Where(d => d.AssociatedProjectId == projectId);

                query = sortBy switch
                {
                    "Title" or "title" => query.OrderBy(d => d.Title),
                    "Category" or "category" => query.OrderBy(d => d.Category),
                    "Size" or "size" => query.OrderByDescending(d => d.FileSizeBytes),
                    _ => query.OrderByDescending(d => d.UploadedAt)
                };

                return await query.ToListAsync();
            }
            catch
            {
                return new List<Document>();
            }
        }

        public async Task<List<Document>> GetMyDocumentsAsync(int userId, string sortBy = "UploadedAt", bool descending = true)
        {
            try
            {
                var query = _context.Documents
                    .AsNoTracking()
                    .Include(d => d.Uploader)
                    .Include(d => d.AssociatedProject)
                    .Where(d => d.UploadedBy == userId && !d.IsArchived);

                query = sortBy switch
                {
                    "Title" => descending ? query.OrderByDescending(d => d.Title) : query.OrderBy(d => d.Title),
                    "Category" => descending ? query.OrderByDescending(d => d.Category) : query.OrderBy(d => d.Category),
                    "Size" => descending ? query.OrderByDescending(d => d.FileSizeBytes) : query.OrderBy(d => d.FileSizeBytes),
                    _ => descending ? query.OrderByDescending(d => d.UploadedAt) : query.OrderBy(d => d.UploadedAt)
                };

                return await query.ToListAsync();
            }
            catch
            {
                return new List<Document>();
            }
        }

        public async Task<Document> GetDocumentAsync(int documentId, int requestingUserId)
        {
            // TODO: Implementar query de documento con autorizacion
            throw new NotImplementedException();
        }

        public async Task<byte[]> DownloadDocumentAsync(int documentId, int requestingUserId)
        {
            // TODO: Implementar descarga con autorizacion y auditoria
            throw new NotImplementedException();
        }

        public async Task<List<Document>> SearchDocumentsAsync(
            string searchTerm,
            int userId,
            int? projectFilter = null,
            string categoryFilter = null,
            TimeSpan? dateRange = null)
        {
            var fromDate = dateRange.HasValue ? DateTime.UtcNow.Subtract(dateRange.Value) : (DateTime?)null;
            return await SearchDocumentsAsync(userId, searchTerm, categoryFilter ?? string.Empty, fromDate, null, projectFilter);
        }

        public async Task<List<Document>> SearchDocumentsAsync(
            int userId,
            string searchTerm = "",
            string category = "",
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            return await SearchDocumentsAsync(userId, searchTerm, category, fromDate, toDate, null);
        }

        private async Task<List<Document>> SearchDocumentsAsync(
            int userId,
            string searchTerm,
            string category,
            DateTime? fromDate,
            DateTime? toDate,
            int? projectFilter)
        {
            try
            {
                var query = _context.Documents
                    .AsNoTracking()
                    .Include(d => d.Uploader)
                    .Include(d => d.AssociatedProject)
                    .Where(d => d.UploadedBy == userId && !d.IsArchived);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.ToLower();
                    query = query.Where(d =>
                        d.Title.ToLower().Contains(term) ||
                        d.Description.ToLower().Contains(term));
                }

                if (!string.IsNullOrWhiteSpace(category))
                    query = query.Where(d => d.Category == category);

                if (projectFilter.HasValue)
                    query = query.Where(d => d.AssociatedProjectId == projectFilter.Value);

                if (fromDate.HasValue)
                    query = query.Where(d => d.UploadedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(d => d.UploadedAt <= toDate.Value);

                return await query.OrderByDescending(d => d.UploadedAt).ToListAsync();
            }
            catch
            {
                return new List<Document>();
            }
        }

        public async Task<(bool Success, int? ShareId, string ErrorMessage)> ShareDocumentAsync(
            int documentId,
            int shareWithUserId,
            int requestingUserId)
        {
            // TODO: Implementar comparticion de documentos
            throw new NotImplementedException();
        }

        public async Task<bool> RevokeShareAsync(int documentShareId, int requestingUserId)
        {
            // TODO: Implementar revocacion de comparticion
            throw new NotImplementedException();
        }

        public async Task<List<Document>> GetSharedWithMeAsync(int userId)
        {
            // TODO: Implementar query de documentos compartidos conmigo
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId, string ipAddress)
        {
            // TODO: Implementar eliminacion con autorizacion y auditoria
            throw new NotImplementedException();
        }

        public async Task<int> ArchiveProjectDocumentsAsync(int projectId, int requestingUserId, string ipAddress)
        {
            // TODO: Implementar archivacion de documentos de proyecto
            throw new NotImplementedException();
        }
    }
}
