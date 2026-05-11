using System;
using System.IO;
using System.Threading.Tasks;

namespace ContosoDashboard.Services
{
    public class FileStorageService
    {
        private readonly IConfiguration _configuration;
        private const long MAX_FILE_SIZE = 25 * 1024 * 1024; // 25 MB
        private readonly string[] _allowedExtensions = { ".pdf", ".docx", ".xlsx", ".pptx", ".jpg", ".jpeg", ".png", ".txt" };

        public FileStorageService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Almacena un archivo en AppData/uploads con un nombre basado en GUID
        /// </summary>
        public async Task<string> StoreFileAsync(IFormFile file, int documentId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("El archivo no es válido", nameof(file));

            if (file.Length > MAX_FILE_SIZE)
                throw new InvalidOperationException($"El archivo excede el límite de 25 MB");

            if (!IsValidFileExtension(file.FileName))
                throw new InvalidOperationException("El tipo de archivo no está permitido");

            try
            {
                var storagePath = GetStoragePath();
                var documentPath = Path.Combine(storagePath, documentId.ToString());

                // Crear directorio si no existe
                if (!Directory.Exists(documentPath))
                    Directory.CreateDirectory(documentPath);

                // Generar nombre GUID para el archivo
                var fileName = $"{Guid.NewGuid()}";
                var filePath = Path.Combine(documentPath, fileName);

                // Guardar archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Retornar ruta lógica relativa
                return Path.Combine(documentId.ToString(), fileName).Replace("\\", "/");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al almacenar el archivo: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Recupera un archivo del almacenamiento
        /// </summary>
        public async Task<byte[]> RetrieveFileAsync(string filePath)
        {
            try
            {
                var fullPath = GetSafeFullPath(filePath);

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"El archivo no existe: {filePath}");

                return await File.ReadAllBytesAsync(fullPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al recuperar el archivo: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Elimina un archivo del almacenamiento
        /// </summary>
        public async Task DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = GetSafeFullPath(filePath);

                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                // Intentar eliminar el directorio si está vacío
                var directoryPath = Path.GetDirectoryName(fullPath);
                if (Directory.Exists(directoryPath) && !Directory.EnumerateFiles(directoryPath).Any())
                    Directory.Delete(directoryPath);
            }
            catch (Exception ex)
            {
                // Logging silencioso - no fallar si la eliminación falla
                System.Diagnostics.Debug.WriteLine($"Error al eliminar archivo: {ex.Message}");
            }
        }

        /// <summary>
        /// Valida si la extensión está en la lista blanca
        /// </summary>
        public bool IsValidFileExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return Array.Exists(_allowedExtensions, ext => ext == extension);
        }

        /// <summary>
        /// Obtiene la ruta de almacenamiento configurada
        /// </summary>
        public string GetStoragePath()
        {
            var storagePath = _configuration["FileStorage:UploadsPath"] ?? "AppData/uploads";
            var basePath = AppContext.BaseDirectory;
            var fullPath = Path.Combine(basePath, storagePath);
            return fullPath;
        }

        private string GetSafeFullPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("La ruta del archivo no es valida", nameof(filePath));

            var storagePath = Path.GetFullPath(GetStoragePath());
            var fullPath = Path.GetFullPath(Path.Combine(storagePath, filePath.Replace("/", "\\")));

            if (!fullPath.StartsWith(storagePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La ruta del archivo no es valida");

            return fullPath;
        }
    }
}
