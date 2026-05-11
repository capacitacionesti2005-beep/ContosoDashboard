using System;
using System.Threading.Tasks;
using ContosoDashboard.Models;
using ContosoDashboard.Data;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services
{
    public class DocumentAuthService
    {
        private readonly ApplicationDbContext _context;

        public DocumentAuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Verifica si un usuario puede acceder a un documento
        /// </summary>
        public async Task<bool> CanAccessDocumentAsync(int documentId, int userId)
        {
            try
            {
                var document = await _context.Documents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                if (document == null)
                    return false;

                // Usuario puede acceder si:
                // 1. Es el propietario del documento
                if (document.UploadedBy == userId)
                    return true;

                // 2. El documento está compartido con él
                var isShared = await _context.DocumentShares
                    .AsNoTracking()
                    .AnyAsync(ds => 
                        ds.DocumentId == documentId && 
                        ds.SharedWith == userId && 
                        ds.RevokedAt == null);

                if (isShared)
                    return true;

                // 3. Es miembro del proyecto asociado (si existe)
                if (document.AssociatedProjectId.HasValue)
                {
                    var isMember = await _context.ProjectMembers
                        .AsNoTracking()
                        .AnyAsync(pm => 
                            pm.ProjectId == document.AssociatedProjectId && 
                            pm.UserId == userId);

                    if (isMember)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si un usuario puede modificar un documento
        /// </summary>
        public async Task<bool> CanModifyDocumentAsync(int documentId, int userId)
        {
            try
            {
                var document = await _context.Documents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                if (document == null)
                    return false;

                // El propietario siempre puede modificar
                if (document.UploadedBy == userId)
                    return true;

                // Un ProjectManager del proyecto asociado puede modificar
                if (document.AssociatedProjectId.HasValue)
                {
                    var user = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == userId);

                    if (user?.Role == UserRole.ProjectManager || user?.Role == UserRole.Administrator)
                    {
                        // Verificar que sea PM del proyecto asociado
                        var project = await _context.Projects
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.ProjectId == document.AssociatedProjectId);

                        if (project?.ProjectManagerId == userId)
                            return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si un usuario puede compartir un documento
        /// </summary>
        public async Task<bool> CanShareDocumentAsync(int documentId, int userId)
        {
            try
            {
                var document = await _context.Documents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                if (document == null)
                    return false;

                // Solo el propietario del documento puede compartirlo
                return document.UploadedBy == userId;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si un usuario puede eliminar un documento
        /// </summary>
        public async Task<bool> CanDeleteDocumentAsync(int documentId, int userId)
        {
            try
            {
                var document = await _context.Documents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DocumentId == documentId);

                if (document == null)
                    return false;

                // El propietario siempre puede eliminar
                if (document.UploadedBy == userId)
                    return true;

                // Un ProjectManager del proyecto asociado puede eliminar
                if (document.AssociatedProjectId.HasValue)
                {
                    var user = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == userId);

                    if (user?.Role == UserRole.ProjectManager || user?.Role == UserRole.Administrator)
                    {
                        var project = await _context.Projects
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.ProjectId == document.AssociatedProjectId);

                        if (project?.ProjectManagerId == userId)
                            return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
