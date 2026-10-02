# Study Materials

**Audience:** lecturers, students, administrators
**Last updated:** 2026-10-02

Study Materials lets a lecturer publish the documents for a unit they teach, and
lets a student read and download the documents for a unit they are enrolled in.

---

## 1. Navigation

**Academics → Study Materials** appears in the left sidebar for the `Lecturer`
and `Student` roles. It opens a unit selector listing **only** the units the
signed-in user is entitled to:

| Role | Units listed | May upload? |
| --- | --- | --- |
| Lecturer | Units they are appointed to teach | Yes |
| Student | Units they are enrolled to study | No |
| Administrator / System Administrator / Coordinator | *(not listed here)* | No |
| Receptionist | *(not listed here)* | No |

Administrator and Coordinator staff already reach any unit's materials through
the course-offering **Units** tab (`CourseOfferingDetail` → the MenuBook action
on a unit row). Receptionist has no study-material workflow, so the entry is not
shown to any of them and no permission is broadened to obtain it.

The selector supports free-text filtering by unit code, name or course.

## 2. Lecturer workflow

1. Sign in → **Academics → Study Materials**.
2. Pick a unit from the list (only units you are appointed to teach appear).
3. On the unit page use **Upload material**: set a **Title** (required), an
   optional **Description**, choose a **File**, then press **Upload**.
4. A progress bar shows upload progress; on success the material appears at the
   top of the unit's list.
5. Use the download icon on any row to verify the file downloads and opens.
6. Use the delete icon to soft-delete a material (uploaders, lecturers teaching
   the unit, and admin/coordinator may delete).

## 3. Student workflow

1. Sign in → **Academics → Study Materials**.
2. Pick a unit (only units you are enrolled to study appear).
3. Review the published materials.
4. Press the download icon to save the file; return to the list at any time.

Students never see upload or delete controls, and the API rejects those calls
regardless of what the UI shows.

## 4. API endpoints

Base route: `/api/v1/study-materials`. All require authentication
(`[Authorize]`, cookie session or bearer token) and a resolved tenant.

| Method | Route | Policy | Purpose |
| --- | --- | --- | --- |
| `GET` | `/my-units` | `[Authorize]` + handler | Units the caller may open |
| `GET` | `/unit/{unitId}` | `[Authorize]` + handler | List published materials for a unit |
| `POST` | `/unit/{unitId}` | `LecturerAccess` + handler | Upload a material (multipart/form-data) |
| `GET` | `/{id}/download` | `[Authorize]` + handler | Download the stored file |
| `DELETE` | `/{id}?unitId={unitId}` | `LecturerAccess` + handler | Soft-delete a material |

`GET /my-units` returns a plain array (not a paged envelope):

```json
{
  "unitId": "…", "code": "CSC201", "name": "Data Structures",
  "credits": 3, "courseId": "…", "courseName": "Computer Science",
  "accessRole": "Lecturer"
}
```

`accessRole` is `Lecturer` when the unit came from a teaching assignment and
`Student` when it came from an enrollment.

## 5. Authorization rules

Authorization is enforced **server-side in the handlers**. The React UI only
mirrors it; hiding a control is never the control.

| Rule | Enforced by |
| --- | --- |
| A lecturer may upload only to a unit they are appointed to teach | `CreateStudyMaterialCommandHandler` → `IAcademicAccessService.LecturerTeachesUnitAsync(lecturerId, unitId)` |
| The lecturer is resolved from the authenticated user, never from client input | `CreateStudyMaterialCommandHandler`; `SpecifiedLecturerId` is honoured only for Administrator/Coordinator |
| A student may list/download only units they are enrolled in | `GetUnitStudyMaterialsQueryHandler`, `DownloadStudyMaterialQueryHandler` → `StudentEnrolledInUnitAsync(unitId)` |
| A client-supplied `unitId` is never trusted | The unit is re-resolved through the repository and the relationship re-checked on every call |
| Material `id` manipulation is not an IDOR vector | `DownloadStudyMaterialQueryHandler` loads the material, then authorizes against **its own** `UnitId` |
| Delete requires ownership or teaching relationship | `DeleteStudyMaterialCommandHandler` |
| Roles with no academic relationship get nothing | All handlers; `my-units` returns `403` |
### Tenant isolation

* `Unit`, `LectureNote` and `UploadFile` implement `ITenantAwareEntity`, so EF
  Core applies a global query filter per request. A unit, material or upload row
  belonging to another tenant is invisible to every query — it resolves to
  "not found", never to another tenant's data.
* `UnitRepository.GetUnitsByIdsAsync` inherits that filter, so the unit selector
  cannot emit a foreign unit even if a foreign id reached it.
* PostgreSQL row-level security (`app.current_tenant_id()`) is the second layer.
* Stored files are addressed by relative path only, through `IUploadService`;
  the physical server path is never returned to a client.

## 6. File storage

Uploads go through the **existing** `IUploadService` / `UploadFile` pipeline —
no new or external storage provider was introduced.

| Control | Value |
| --- | --- |
| Category | `UploadCategory.LecturerNotes` |
| Allowed extensions | `.pdf .doc .docx .ppt .pptx .xls .xlsx .odt .odp .ods .rtf .txt .csv` |
| Blocked extensions | executables/scripts (`.exe .dll .bat .cmd .ps1 .vbs .scr .msi .jar .js .php .asp .py .sh` …) |
| Maximum size | 50 MB (also enforced by `[RequestSizeLimit(52_428_800)]`) |
| Validation | extension, double-extension rejection, MIME-from-magic-bytes, magic-byte signature, SHA-256 hash, duplicate detection |
| Filenames | server-generated and unique; the client name is never used as a path |
| Path traversal | `FileStorageService.ResolveSafePath` rejects `../`, absolute paths and anything resolving outside the storage root |
| Duplicate names | each upload gets a fresh GUID-prefixed name |
| Storage failure | surfaced as a validation error; no metadata row is created |
| Consistency | if the database insert fails after the bytes are stored, the upload metadata is retired through `IUploadService.DeleteAsync` and the orphaned file id is logged, so nothing leaks silently |

Downloads are streamed through the authenticated endpoint with the original
filename and the recorded MIME type. Physical paths are never exposed. A
material whose stored file has gone missing returns **404**, not 500.

## 7. Data model

`LectureNote` (table `lecture_notes`, tenant-aware, soft-deletable):

| Field | Notes |
| --- | --- |
| `Title` | required, max 200 |
| `Description` | optional, max 1000 |
| `UnitId` | FK → `units` (indexed) |
| `LecturerId` | FK → `lecturers` |
| `FilePath` / `FileName` / `ContentType` / `FileSize` / `Version` | stored metadata |
| `UploadFileId` | nullable FK → `upload_files` (the real storage record) |
| `UploadDate`, `IsPublished` | listing filters unpublished records |

The table already exists in `20260728195330_InitialMigration`; this work added
**no schema change**, so no new migration is required.

## 8. Error handling

| Situation | Response |
| --- | --- |
| Not signed in | `401` |
| Role without academic relationship | `403` |
| Lecturer uploading to an unassigned unit | `403` |
| Student accessing an unenrolled unit | `403` |
| Another tenant's unit / material | treated as not found or forbidden — never revealed |
| Unknown material id | `404` |
| Stored file missing | `404` |
| Disallowed type / oversize | `400` with a field-level message |

The UI renders loading, empty, error and permission states for each of these.

## 9. Tests

| Suite | Location |
| --- | --- |
| Handler authorization (unit selector, upload, orphan compensation) | `tests/SMS.UnitTests/StudyMaterials/StudyMaterialAuthorizationTests.cs` |
| End-to-end API (upload → list → download → delete, negative cases, `my-units`) | `tests/SMS.ApiTests/Controllers/DocumentUploadApiTests.cs` |
| Page behaviour, role visibility, extension allow-list | `frontend/sms-web/src/pages/StudyMaterialsPage.test.tsx` |

See `STUDY_MATERIALS_REPAIR_AND_DEPLOYMENT_REPORT.md` for the executed results.

`AcademicAccessService` derives relationships from persisted data only:

* **Taught** — active `UnitAllocation` rows **plus** units of active
  `CourseOfferingLecturer` assignments.
* **Enrolled** — legacy `Enrollment` rows, `StudentEnrollment` rows with status
  `Enrolled`, **plus** units of active `CourseOfferingEnrollment` rows.