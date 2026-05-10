# Constitución de ContosoDashboard

## Principios Fundamentales

### I. Transparencia con Propósito de Entrenamiento
ContosoDashboard DEBE seguir siendo un artefacto de entrenamiento con un propósito educativo claro. El código DEBE explicar patrones mediante implementación legible, datos de ejemplo y comentarios explícitos. Ninguna característica puede presentarse como lista para producción; toda guía de seguridad, arquitectura y despliegue DEBE enmarcarse como ejemplos de entrenamiento.

### II. Seguridad como Ejemplo
La seguridad DEBE ser demostrable, utilizable y explícita en el repositorio. Los ejemplos de autorización y autenticación DEBEN aplicarse tanto en las páginas como en los servicios, y el código de entrenamiento DEBE mostrar las decisiones de defensa en profundidad en lugar de ocultarlas tras comportamientos opacos del framework. Los ejemplos de identidad simulada y roles DEBEN ilustrar compensaciones reales de seguridad mientras mantienen el sistema seguro para entrenamiento sin conexión.

### III. Aprendizaje Verificado con Pruebas y Especificaciones
Todo cambio significativo DEBE capturarse en artefactos verificables. El trabajo de funcionalidades DEBE originarse en especificaciones, planes y tareas, y la implementación DEBE ser verificable mediante ejemplos, criterios de aceptación o escenarios ejecutables. Esto asegura que el repositorio permanezca como un cuaderno de aprendizaje en lugar de una implementación sin documentación.

### IV. Prioridad Off-line con Claridad de Migración
El repositorio DEBE priorizar la ejecución local y sin conexión preservando rutas de migración claras hacia servicios en la nube. LocalDB, autenticación simulada y comportamiento de interfaz autocontenida son la configuración predeterminada. Cualquier abstracción preparada para la nube DEBE estar claramente separada, documentada y ser opcional para que la experiencia de entrenamiento no dependa de servicios externos.

### V. Disciplina de Documentación y Proceso
La documentación DEBE mantenerse sincronizada con el código y los objetivos de aprendizaje de este proyecto. La guía de inicio, las limitaciones de seguridad, las notas de arquitectura y las restricciones de entrenamiento DEBEN mantenerse en los documentos del proyecto. Las solicitudes de extracción y la retroalimentación de revisión DEBEN referenciar esta constitución y la versión actual para confirmar el cumplimiento.

## Restricciones Adicionales
Este proyecto es únicamente una implementación de entrenamiento. NO DEBE declarar estar listo para producción, requerir servicios en la nube externos ni almacenar credenciales reales de usuarios. El repositorio DEBE permanecer autocontenido con dependencias locales y datos de ejemplo. Todas las decisiones de diseño DEBEN priorizar claridad, reproducibilidad y ejecución segura sin conexión.

## Flujo de Trabajo de Desarrollo
El trabajo de funcionalidades DEBE seguir el flujo de trabajo Spec Kit: definir la funcionalidad en una especificación, crear un plan de implementación y generar tareas antes de codificar. Los cambios DEBEN revisarse frente a la constitución y documentarse en los resúmenes de PR. El riesgo, la complejidad y el valor de entrenamiento DEBEN hacerse explícitos en cada cambio no trivial.

## Gobernanza
Esta constitución es la fuente de verdad para la disciplina del proyecto y las prácticas aceptables. Cualquier enmienda DEBE actualizar este documento y referenciar el cambio en la solicitud de extracción asociada.

- Se requieren incrementos de versión mayor para cambios que alteren principios, gobernanza o el alcance de entrenamiento del proyecto.
- Se requieren incrementos de versión menor para secciones añadidas, nuevas restricciones obligatorias o expansiones materiales de la guía de gobernanza.
- Se requieren incrementos de parche para aclaraciones, mejoras de redacción y refinamientos no semánticos.
- Cada solicitud de extracción DEBE citar la versión actual de la constitución y anotar cualquier desviación o excepción.
- Las revisiones DEBEN verificar que la documentación siga siendo consistente con el código, que no queden tokens de marcador de posición sin explicar y que todas las fechas de gobernanza usen formato ISO.

**Versión**: 1.0.0 | **Ratificada**: 2026-05-09 | **Última Enmienda**: 2026-05-09
