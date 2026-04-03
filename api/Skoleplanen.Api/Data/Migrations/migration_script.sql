CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE TABLE "Classes" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(8000),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Classes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE TABLE "Courses" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(8000),
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_Courses" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE TABLE "Rooms" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(8000),
        "Capacity" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Rooms" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE TABLE "Schools" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Slug" character varying(128) NOT NULL,
        "ContactEmail" character varying(500),
        "ContactPhone" character varying(50),
        "LogoUrl" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_Schools" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE TABLE "Staff" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Email" character varying(500),
        "Phone" character varying(50),
        "Role" integer NOT NULL,
        "KeycloakSubject" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Staff" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE TABLE "TimeSlotTemplates" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "LessonDurationMinutes" integer NOT NULL,
        "DayStartTime" time without time zone NOT NULL,
        "DayEndTime" time without time zone NOT NULL,
        "ActiveDays" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TimeSlotTemplates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE TABLE "Schemas" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "ClassId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Status" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Schemas" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Schemas_Classes_ClassId" FOREIGN KEY ("ClassId") REFERENCES "Classes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE TABLE "TimeSlots" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "ClassId" uuid,
        "SortOrder" integer NOT NULL,
        "StartTime" time without time zone NOT NULL,
        "EndTime" time without time zone NOT NULL,
        "Label" character varying(500),
        CONSTRAINT "PK_TimeSlots" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TimeSlots_Classes_ClassId" FOREIGN KEY ("ClassId") REFERENCES "Classes" ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE TABLE "StaffInvitations" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "StaffId" uuid NOT NULL,
        "Email" character varying(500) NOT NULL,
        "Token" character varying(128) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "AcceptedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "RowVersion" bytea,
        CONSTRAINT "PK_StaffInvitations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_StaffInvitations_Staff_StaffId" FOREIGN KEY ("StaffId") REFERENCES "Staff" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE TABLE "TimeSlotTemplateBreaks" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "TimeSlotTemplateId" uuid NOT NULL,
        "StartTime" time without time zone NOT NULL,
        "DurationMinutes" integer NOT NULL,
        CONSTRAINT "PK_TimeSlotTemplateBreaks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TimeSlotTemplateBreaks_TimeSlotTemplates_TimeSlotTemplateId" FOREIGN KEY ("TimeSlotTemplateId") REFERENCES "TimeSlotTemplates" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE TABLE "SchemaSlots" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "SchemaId" uuid NOT NULL,
        "TimeSlotId" uuid NOT NULL,
        "Weekday" integer NOT NULL,
        "CourseId" uuid NOT NULL,
        "TeacherId" uuid NOT NULL,
        "RoomId" uuid,
        "AideId" uuid,
        CONSTRAINT "PK_SchemaSlots" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SchemaSlots_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_SchemaSlots_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id"),
        CONSTRAINT "FK_SchemaSlots_Schemas_SchemaId" FOREIGN KEY ("SchemaId") REFERENCES "Schemas" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_SchemaSlots_Staff_AideId" FOREIGN KEY ("AideId") REFERENCES "Staff" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_SchemaSlots_Staff_TeacherId" FOREIGN KEY ("TeacherId") REFERENCES "Staff" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_SchemaSlots_TimeSlots_TimeSlotId" FOREIGN KEY ("TimeSlotId") REFERENCES "TimeSlots" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE INDEX "IX_Schemas_ClassId" ON "Schemas" ("ClassId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE INDEX "IX_SchemaSlots_AideId" ON "SchemaSlots" ("AideId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE INDEX "IX_SchemaSlots_CourseId" ON "SchemaSlots" ("CourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE INDEX "IX_SchemaSlots_RoomId" ON "SchemaSlots" ("RoomId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE INDEX "IX_SchemaSlots_SchemaId" ON "SchemaSlots" ("SchemaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE INDEX "IX_SchemaSlots_TeacherId" ON "SchemaSlots" ("TeacherId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE INDEX "IX_SchemaSlots_TimeSlotId" ON "SchemaSlots" ("TimeSlotId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE UNIQUE INDEX "IX_Schools_Slug" ON "Schools" ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE INDEX "IX_StaffInvitations_StaffId" ON "StaffInvitations" ("StaffId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE UNIQUE INDEX "IX_StaffInvitations_Token" ON "StaffInvitations" ("Token");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE INDEX "IX_TimeSlots_ClassId" ON "TimeSlots" ("ClassId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    CREATE INDEX "IX_TimeSlotTemplateBreaks_TimeSlotTemplateId" ON "TimeSlotTemplateBreaks" ("TimeSlotTemplateId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403183219_Initial') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260403183219_Initial', '10.0.5');
    END IF;
END $EF$;
COMMIT;

