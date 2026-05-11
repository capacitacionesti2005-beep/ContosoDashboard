# Tareas: Carga y Gestión de Documentos

**Entrada**: documentacion de diseño en `specs/001-document-management/` (plan.md, spec.md, research.md, data-model.md, contracts/)
**Formato**: `- [ ] [TaskID] [P?] [Story?] Descripción con ruta de archivo exacta`

## Fase 1: Configuración (Infraestructura compartida)

**Propósito**: Preparar la base del proyecto, archivos y servicios necesarios antes de comenzar las historias de usuario.

- [x] T001 [P] Crear `ContosoDashboard/Pages/Documents.razor` como página principal de lista de documentos.
- [x] T002 [P] Crear `ContosoDashboard/Pages/DocumentUpload.razor` como página de carga de documentos.
- [x] T003 [P] Crear `ContosoDashboard/Pages/DocumentDetails.razor` como página de detalles y descarga de documento.
- [x] T004 [P] Crear `ContosoDashboard/Pages/SharedWithMe.razor` como página para documentos compartidos conmigo.
- [x] T005 [P] Crear `ContosoDashboard/Components/DocumentList.razor` para mostrar listas de documentos.
- [x] T006 [P] Crear `ContosoDashboard/Components/DocumentSearchBar.razor` con filtros y búsqueda.
- [x] T007 [P] Crear `ContosoDashboard/Components/DocumentPreview.razor` para vista previa de PDF e imágenes.
- [x] T008 [P] Crear `ContosoDashboard/Services/DocumentService.cs` como servicio principal de documentos.
- [x] T009 [P] Crear `ContosoDashboard/Services/FileStorageService.cs` para operaciones de archivo en `AppData/uploads`.
- [x] T010 [P] Crear `ContosoDashboard/Services/DocumentAuthService.cs` para reglas de autorización IDOR y permisos.
- [x] T011 [P] Crear `ContosoDashboard/Services/DocumentAuditService.cs` para registrar acciones de documentos.
- [x] T012 Actualizar `ContosoDashboard/Data/ApplicationDbContext.cs` para agregar DbSet de Document, DocumentShare y DocumentAuditLog.
- [x] T013 Actualizar `appsettings.json` con la configuración de ruta de almacenamiento de archivos `AppData/uploads`.

---

## Fase 2: Fundacional (Prerequisitos bloqueantes)

**Propósito**: Crear el modelo de datos y la infraestructura central que deben existir antes de que cualquier historia de usuario pueda funcionar.

- [x] T014 Crear `ContosoDashboard/Models/Document.cs` con metadatos, ruta de archivo y bandera `IsArchived`.
- [x] T015 Crear `ContosoDashboard/Models/DocumentShare.cs` para relación de compartición de documentos.
- [x] T016 Crear `ContosoDashboard/Models/DocumentAuditLog.cs` para historial de auditoría de documentos.
- [x] T017 Actualizar `ContosoDashboard/Data/ApplicationDbContext.cs` con las entidades Document, DocumentShare y DocumentAuditLog y sus relaciones.
- [x] T018 Crear una migración EF Core en `ContosoDashboard/Data/Migrations/` que agregue las tablas de documentos, comparticiones y auditoría.
- [x] T019 Implementar `ContosoDashboard/Services/FileStorageService.cs` para generar nombres GUID y almacenar archivos fuera de la raíz web.
- [x] T020 Implementar `ContosoDashboard/Services/DocumentAuthService.cs` con métodos `CanAccessDocumentAsync`, `CanModifyDocumentAsync` y `CanShareDocumentAsync`.
- [x] T021 Implementar `ContosoDashboard/Services/DocumentAuditService.cs` para crear registros de auditoría en base de datos.
- [x] T022 Implementar validación de carga en `ContosoDashboard/Services/DocumentService.cs` para tamaño máximo, extensiones permitidas y permisos de proyecto.
- [x] T023 Actualizar `ContosoDashboard/Program.cs` para inyectar `DocumentService`, `FileStorageService`, `DocumentAuthService` y `DocumentAuditService`.
- [x] T024 Verificar que `ContosoDashboard/Program.cs` registre correctamente `appsettings.json` y la ruta de almacenamiento de archivos `AppData/uploads`.

---

## Fase 3: Historia de Usuario 1 - Cargar Documentos de Trabajo (Prioridad: P1) 🎯 MVP

**Objetivo**: Permitir a los empleados cargar archivos válidos con metadatos y asociarlos opcionalmente a un proyecto.

**Prueba independiente**: Cargar un PDF/Word/imagen con título, categoría y proyecto. Verificar que se almacena en `AppData/uploads` y que los metadatos aparecen en la lista de documentos.

### Implementación para US1

- [x] T025 [US1] Implementar el formulario de carga en `ContosoDashboard/Pages/DocumentUpload.razor`.
- [x] T026 [US1] Implementar la validación de formulario en `ContosoDashboard/Pages/DocumentUpload.razor` para título, categoría y tipo de archivo.
- [x] T027 [US1] Implementar `DocumentService.UploadDocumentAsync` en `ContosoDashboard/Services/DocumentService.cs`.
- [x] T028 [US1] Implementar la descarga segura de archivos en `ContosoDashboard/Services/FileStorageService.cs`.
- [x] T029 [US1] Crear registro de metadatos en `ContosoDashboard/Models/Document.cs` y persistir con `ApplicationDbContext`.
- [x] T030 [US1] Manejar errores de límite de tamaño y extensiones inválidas en `ContosoDashboard/Services/DocumentService.cs`.
- [x] T031 [US1] Mostrar mensajes de éxito y error en `ContosoDashboard/Pages/DocumentUpload.razor`.

---

## Fase 4: Historia de Usuario 2 - Examinar Mis Documentos (Prioridad: P1)

**Objetivo**: Mostrar todos los documentos cargados por el usuario con filtros, ordenamiento y metadatos claros.

**Prueba independiente**: Cargar varios documentos y verificar que aparecen en `Documents.razor` con título, categoría, fecha, tamaño y proyecto asociado.

### Implementación para US2

- [ ] T032 [US2] Implementar `DocumentService.GetMyDocumentsAsync` en `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T033 [US2] Implementar `ContosoDashboard/Pages/Documents.razor` para listar documentos del usuario.
- [ ] T034 [US2] Implementar `ContosoDashboard/Components/DocumentList.razor` con columnas ordenables y acciones de vista previa/descarga.
- [ ] T035 [US2] Implementar `ContosoDashboard/Components/DocumentSearchBar.razor` con filtros por categoría, proyecto y rango de fechas.
- [ ] T036 [US2] Implementar `ContosoDashboard/Pages/DocumentDetails.razor` para mostrar metadatos completos y botones de descarga.
- [ ] T037 [US2] Aplicar autorización con `DocumentAuthService.CanAccessDocumentAsync` antes de mostrar o descargar un documento.

---

## Fase 5: Historia de Usuario 3 - Buscar y Filtrar Documentos (Prioridad: P2)

**Objetivo**: Permitir búsquedas rápidas en metadatos de documentos que respeten los permisos de acceso.

**Prueba independiente**: Buscar por palabra clave y verificar que sólo aparecen documentos a los que el usuario tiene acceso, con resultados en menos de 2 segundos.

### Implementación para US3

- [ ] T038 [US3] Implementar `DocumentService.SearchDocumentsAsync` en `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T039 [US3] Integrar la búsqueda en `ContosoDashboard/Pages/Documents.razor` usando `DocumentSearchBar.razor`.
- [ ] T040 [US3] Mostrar resultados de búsqueda en `ContosoDashboard/Components/DocumentList.razor`.
- [ ] T041 [US3] Asegurar que la búsqueda filtra documentos sin permiso de acceso en `DocumentService.SearchDocumentsAsync`.

---

## Fase 6: Historia de Usuario 4 - Compartir Documentos con Miembros del Equipo (Prioridad: P2)

**Objetivo**: Permitir al propietario compartir un documento con un usuario específico y visualizar documentos compartidos.

**Prueba independiente**: Compartir un documento con un colega y verificar que aparece en `SharedWithMe.razor` y que el destinatario puede descargarlo.

### Implementación para US4

- [ ] T042 [US4] Implementar `DocumentService.ShareDocumentAsync` en `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T043 [US4] Implementar `DocumentService.RevokeShareAsync` en `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T044 [US4] Crear el modelo `ContosoDashboard/Models/DocumentShare.cs` y persistir comparticiones en la base de datos.
- [ ] T045 [US4] Implementar la página `ContosoDashboard/Pages/SharedWithMe.razor` para mostrar documentos compartidos conmigo.
- [ ] T046 [US4] Agregar botón de compartir y selección de usuario en `ContosoDashboard/Pages/DocumentDetails.razor`.
- [ ] T047 [US4] Registrar la acción de compartir en `DocumentAuditService` y enviar notificación si ya existe `NotificationService`.

---

## Fase 7: Historia de Usuario 6 - Eliminar Documentos (Prioridad: P2)

**Objetivo**: Permitir eliminar documentos con confirmación, tanto para el propietario como para el gerente de proyecto.

**Prueba independiente**: Eliminar un documento y verificar que desaparece de todas las vistas y que el archivo se elimina del almacenamiento.

### Implementación para US6

- [ ] T048 [US6] Implementar `DocumentService.DeleteDocumentAsync` en `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T049 [US6] Agregar botón de eliminar y diálogo de confirmación en `ContosoDashboard/Components/DocumentList.razor`.
- [ ] T050 [US6] Agregar acción de eliminar en `ContosoDashboard/Pages/DocumentDetails.razor`.
- [ ] T051 [US6] Permitir que un gerente de proyecto elimine documentos del proyecto según los permisos en `DocumentAuthService`.

---

## Fase 8: Historia de Usuario 5 - Gestionar Metadatos y Versiones (Prioridad: P3)

**Objetivo**: Permitir editar metadatos y reemplazar un archivo con una nueva versión de documento.

**Prueba independiente**: Editar título/categoría/etiquetas y reemplazar el archivo en la página de detalles.

### Implementación para US5

- [ ] T052 [US5] Implementar el formulario de edición de metadatos en `ContosoDashboard/Pages/DocumentDetails.razor`.
- [ ] T053 [US5] Implementar `DocumentService.UpdateDocumentMetadataAsync` en `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T054 [US5] Implementar la funcionalidad de reemplazo de archivo en `ContosoDashboard/Services/FileStorageService.cs` y `DocumentService`.
- [ ] T055 [US5] Actualizar la vista de detalles para mostrar la versión actual y la fecha de modificación.

---

## Fase 9: Pulido y preocupaciones transversales

**Propósito**: Ajustes finales, documentación, seguridad y limpieza de código después de que las historias principales estén implementadas.

- [ ] T056 [P] Actualizar `specs/001-document-management/quickstart.md` con pasos de uso reales y configuración final.
- [ ] T057 [P] Crear o actualizar `specs/001-document-management/checklists/requirements.md` con criterios de aceptación basados en las historias.
- [ ] T058 [P] Documentar la configuración de `AppData/uploads` en `README.md` y `appsettings.json`.
- [ ] T059 [P] Revisar y refactorizar `ContosoDashboard/Services/` y `ContosoDashboard/Models/` para eliminar duplicados y mejorar claridad.
- [ ] T060 [P] Probar manualmente las páginas `ContosoDashboard/Pages/Documents.razor`, `DocumentUpload.razor`, `DocumentDetails.razor` y `SharedWithMe.razor`.

---

## Dependencias y orden de ejecución

### Dependencias de fases

- **Fase 1: Configuración**: puede comenzar de inmediato.
- **Fase 2: Fundacional**: depende de completar la configuración y bloquea todas las historias.
- **Fase 3+**: cada historia de usuario depende de completar la Fase 2.
- **Fase 9: Pulido**: depende de completar las historias de usuario deseadas.

### Dependencias de historias

- **US1 (P1)**: puede comenzar después de Fase 2 y es la prioridad de MVP.
- **US2 (P1)**: puede comenzar después de Fase 2 y es necesaria para uso del MVP.
- **US3 (P2)**: puede comenzar después de Fase 2 y se integra con US1/US2.
- **US4 (P2)**: puede comenzar después de Fase 2 y se integra con US1/US2.
- **US6 (P2)**: puede comenzar después de Fase 2 y se integra con US1/US2.
- **US5 (P3)**: puede comenzar después de Fase 2 y se entrega como mejora posterior.

### Oportunidades de paralelismo

- Los tasks de creación de archivos en Fase 1 pueden ejecutarse en paralelo: `Pages/`, `Components/`, `Services/`.
- Muchos tasks de Fase 2 son paralelizables: modelos, migración, servicios y configuración.
- Las historias de usuario US3, US4 y US6 pueden trabajar en paralelo una vez que la infraestructura fundacional esté completa.
- Las tareas de pulido en Fase 9 son paralelizables entre documentación, pruebas manuales y refactorización.

---

##Resumen de tasks

- Total de tareas: 60
- Tareas por historia:
  - US1: 7
  - US2: 6
  - US3: 4
  - US4: 6
  - US6: 4
  - US5: 4
  - Setup/Fundacional/Pulido: 29
- Alcance MVP sugerido: completar US1 y US2 con la infraestructura fundacional antes de una primera entrega.
