using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using ContosoDashboard.Models;
using ContosoDashboard.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services
{
    public class DocumentAuditService
    {
        private readonly ApplicationDbContext _context;

        public DocumentAuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registra una acción de documento en la auditoría
        /// </summary>
        public async Task LogActionAsync(
            int documentId,
            int userId,
            string action, // Upload, Download, Share, Revoke, Delete, Archive
            string ipAddress,
            string userAgent,
            string details = null)
        {
            try
            {
                var auditLog = new DocumentAuditLog
                {
                    DocumentId = documentId,
                    UserId = userId,
                    Action = action,
                    IpAddress = ipAddress ?? "Unknown",
                    UserAgent = userAgent ?? "Unknown",
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };

                _context.DocumentAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Logging silencioso - la auditoría no debe fallar la operación principal
                System.Diagnostics.Debug.WriteLine($"Error registrando auditoría: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene el historial de auditoría de un documento
        /// </summary>
        public async Task<List<DocumentAuditLog>> GetDocumentAuditTrailAsync(int documentId)
        {
            try
            {
                return await _context.DocumentAuditLogs
                    .AsNoTracking()
                    .Where(dal => dal.DocumentId == documentId)
                    .OrderByDescending(dal => dal.Timestamp)
                    .ToListAsync();
            }
            catch
            {
                return new List<DocumentAuditLog>();
            }
        }
    }
}
