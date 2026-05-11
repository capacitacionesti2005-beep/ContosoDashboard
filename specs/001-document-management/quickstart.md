# Document Upload and Management Quick Start

## Overview
This feature enables authenticated Contoso employees to upload, manage, and share documents securely from the Contoso Dashboard.

## Setup
1. Ensure the application is running with a valid `ASPNETCORE_ENVIRONMENT` and the database is migrated.
2. Confirm the uploads path is configured in `appsettings.json` and accessible by the web application.

## Using the feature
1. Log in as an authenticated user.
2. Open the `Documents` page from the navigation menu.
3. Upload a new document using the provided file upload form.
4. Enter metadata: filename, description, and share settings.
5. Save the document to store it in the secure `AppData/uploads/` location.

## Managing documents
- View existing documents in the document list.
- Edit metadata fields such as `Description` and `SharedWith`.
- Use built-in authorization rules to ensure only owners and allowed users can access documents.

## Security notes
- Uploaded files are stored outside the web root.
- File access uses secure authorization checks and audit logging.
- Shared documents are visible only to explicitly permitted users.
