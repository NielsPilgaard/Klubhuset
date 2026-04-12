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

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403205521_Remove_Slugs') THEN
    DROP INDEX "IX_Schools_Slug";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403205521_Remove_Slugs') THEN
    ALTER TABLE "Schools" DROP COLUMN "Slug";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260403205521_Remove_Slugs') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260403205521_Remove_Slugs', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404051806_Add_SchoolFile') THEN
    CREATE TABLE "SchoolFiles" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "FileName" character varying(500) NOT NULL,
        "ContentType" character varying(200) NOT NULL,
        "SizeBytes" bigint NOT NULL,
        "StorageKey" character varying(1000) NOT NULL,
        "Url" character varying(2000) NOT NULL,
        "CourseId" uuid,
        "UploadedBy" character varying(200) NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_SchoolFiles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_SchoolFiles_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404051806_Add_SchoolFile') THEN
    CREATE INDEX "IX_SchoolFiles_CourseId" ON "SchoolFiles" ("CourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404051806_Add_SchoolFile') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260404051806_Add_SchoolFile', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404060432_Add_Subscription') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260404060432_Add_Subscription', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404065008_Transactions') THEN
    CREATE TABLE "Subscriptions" (
        "Id" uuid NOT NULL,
        "SchoolId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "StripeCustomerId" text,
        "StripeSubscriptionId" text,
        "CurrentPeriodEnd" timestamp with time zone,
        "TrialEnd" timestamp with time zone NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_Subscriptions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404065008_Transactions') THEN
    CREATE UNIQUE INDEX "IX_Subscriptions_SchoolId" ON "Subscriptions" ("SchoolId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404065008_Transactions') THEN
    CREATE INDEX "IX_Subscriptions_StripeCustomerId" ON "Subscriptions" ("StripeCustomerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404065008_Transactions') THEN
    CREATE INDEX "IX_Subscriptions_StripeSubscriptionId" ON "Subscriptions" ("StripeSubscriptionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404065008_Transactions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260404065008_Transactions', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404223250_TimeSlot_IsBreak') THEN
    ALTER TABLE "TimeSlots" ADD "IsBreak" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260404223250_TimeSlot_IsBreak') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260404223250_TimeSlot_IsBreak', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405115552_AddCalendarEntry') THEN
    CREATE TABLE "CalendarEntries" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Type" integer NOT NULL,
        "Title" character varying(200) NOT NULL,
        "StartDate" date NOT NULL,
        "EndDate" date NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_CalendarEntries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405115552_AddCalendarEntry') THEN
    CREATE INDEX "IX_CalendarEntries_TenantId_StartDate_EndDate" ON "CalendarEntries" ("TenantId", "StartDate", "EndDate");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405115552_AddCalendarEntry') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260405115552_AddCalendarEntry', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405124740_Add_WeekPlan') THEN
    CREATE TABLE "WeekPlans" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "ClassId" uuid NOT NULL,
        "IsoYear" integer NOT NULL,
        "IsoWeek" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_WeekPlans" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WeekPlans_Classes_ClassId" FOREIGN KEY ("ClassId") REFERENCES "Classes" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405124740_Add_WeekPlan') THEN
    CREATE TABLE "WeekPlanSlots" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "WeekPlanId" uuid NOT NULL,
        "SchemaSlotId" uuid NOT NULL,
        "Beskrivelse" character varying(8000),
        "Lektier" character varying(8000),
        "FagSwapCourseId" uuid,
        "UpdatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        CONSTRAINT "PK_WeekPlanSlots" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WeekPlanSlots_Courses_FagSwapCourseId" FOREIGN KEY ("FagSwapCourseId") REFERENCES "Courses" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_WeekPlanSlots_SchemaSlots_SchemaSlotId" FOREIGN KEY ("SchemaSlotId") REFERENCES "SchemaSlots" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_WeekPlanSlots_WeekPlans_WeekPlanId" FOREIGN KEY ("WeekPlanId") REFERENCES "WeekPlans" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405124740_Add_WeekPlan') THEN
    CREATE TABLE "WeekPlanSlotFiles" (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "WeekPlanSlotId" uuid NOT NULL,
        "SchoolFileId" uuid NOT NULL,
        CONSTRAINT "PK_WeekPlanSlotFiles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WeekPlanSlotFiles_SchoolFiles_SchoolFileId" FOREIGN KEY ("SchoolFileId") REFERENCES "SchoolFiles" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_WeekPlanSlotFiles_WeekPlanSlots_WeekPlanSlotId" FOREIGN KEY ("WeekPlanSlotId") REFERENCES "WeekPlanSlots" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405124740_Add_WeekPlan') THEN
    CREATE INDEX "IX_WeekPlans_ClassId" ON "WeekPlans" ("ClassId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405124740_Add_WeekPlan') THEN
    CREATE UNIQUE INDEX "IX_WeekPlans_TenantId_ClassId_IsoYear_IsoWeek" ON "WeekPlans" ("TenantId", "ClassId", "IsoYear", "IsoWeek");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405124740_Add_WeekPlan') THEN
    CREATE INDEX "IX_WeekPlanSlotFiles_SchoolFileId" ON "WeekPlanSlotFiles" ("SchoolFileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405124740_Add_WeekPlan') THEN
    CREATE UNIQUE INDEX "IX_WeekPlanSlotFiles_WeekPlanSlotId_SchoolFileId" ON "WeekPlanSlotFiles" ("WeekPlanSlotId", "SchoolFileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405124740_Add_WeekPlan') THEN
    CREATE INDEX "IX_WeekPlanSlots_FagSwapCourseId" ON "WeekPlanSlots" ("FagSwapCourseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405124740_Add_WeekPlan') THEN
    CREATE INDEX "IX_WeekPlanSlots_SchemaSlotId" ON "WeekPlanSlots" ("SchemaSlotId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405124740_Add_WeekPlan') THEN
    CREATE UNIQUE INDEX "IX_WeekPlanSlots_WeekPlanId_SchemaSlotId" ON "WeekPlanSlots" ("WeekPlanId", "SchemaSlotId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405124740_Add_WeekPlan') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260405124740_Add_WeekPlan', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405204220_Add_TimeSlot_SchemaId') THEN
    ALTER TABLE "TimeSlots" ADD "SchemaId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405204220_Add_TimeSlot_SchemaId') THEN
    CREATE INDEX "IX_TimeSlots_SchemaId" ON "TimeSlots" ("SchemaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405204220_Add_TimeSlot_SchemaId') THEN
    ALTER TABLE "TimeSlots" ADD CONSTRAINT "FK_TimeSlots_Schemas_SchemaId" FOREIGN KEY ("SchemaId") REFERENCES "Schemas" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260405204220_Add_TimeSlot_SchemaId') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260405204220_Add_TimeSlot_SchemaId', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260412083208_Add_Course_Color') THEN
    ALTER TABLE "Courses" ADD "Color" character varying(7);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260412083208_Add_Course_Color') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260412083208_Add_Course_Color', '10.0.5');
    END IF;
END $EF$;
COMMIT;

